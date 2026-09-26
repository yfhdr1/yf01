using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float stopDistance = 1.5f;
    public float turnSpeed = 10f;
    [Header("Detection")]
    public float detectionRange = 15f;
    public float closeDetectionRange = 5f;
    public float loseTargetRange = 25f;
    public float leashRange = 40f;
    public float attackRange = 2f;
    public float patrolRadius = 10f;
    public float eyeHeight = 1.6f;
    public LayerMask sightMask = ~0;
    [Header("Combat")]
    public float maxHealth = 100f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    public float attackHitDelay = 0.4f;
    [Header("Timing")]
    public float destinationUpdateInterval = 0.1f;
    public float targetScanInterval = 0.5f;
    public float unreachableGiveUpTime = 2f;
    public float navMeshSampleDistance = 1.2f;
    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform playerTarget;
    private enum State { Idle, Chase, Return }
    private State state = State.Idle;
    private Vector3 spawnPoint;
    private Quaternion spawnRotation;
    private PlayerHealth[] cachedPlayers = new PlayerHealth[0];
    private float nextScanTime;
    private float nextDestinationTime;
    private float lastAttackTime = -100f;
    private float pendingHitTime = -1f;
    private Transform pendingHitTarget;
    private float unreachableTimer;
    private float currentHealth;
    private bool isDead;
    private float animSpeed;
    private float networkAnimSpeed;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private bool hasSpeedParam;
    private bool hasAttackParam;
    private bool hasDieParam;
    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        spawnPoint = transform.position;
        spawnRotation = transform.rotation;
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        currentHealth = maxHealth;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (animator.runtimeAnimatorController != null)
            {
                foreach (AnimatorControllerParameter p in animator.parameters)
                {
                    if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
                    if (p.name == "Attack" && p.type == AnimatorControllerParameterType.Trigger) hasAttackParam = true;
                    if (p.name == "Die" && p.type == AnimatorControllerParameterType.Trigger) hasDieParam = true;
                }
            }
        }
    }
    private void Start()
    {
        if (agent == null) return;
        agent.speed = chaseSpeed;
        agent.acceleration = 20f;
        agent.angularSpeed = 540f;
        agent.stoppingDistance = ChaseStopDistance();
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        if (IsAuthority()) EnableAuthority();
        else agent.enabled = false;
    }
    private float ChaseStopDistance()
    {
        return Mathf.Min(stopDistance, attackRange * 0.7f);
    }
    private bool IsAuthority()
    {
        return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
    }
    private void EnableAuthority()
    {
        if (isDead || agent == null) return;
        agent.enabled = true;
        TryPlaceOnNavMesh();
        state = FlatDistance(transform.position, spawnPoint) > 0.6f ? State.Return : State.Idle;
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient) EnableAuthority();
    }
    private void TryPlaceOnNavMesh()
    {
        if (agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas)) agent.Warp(hit.position);
    }
    private void Update()
    {
        if (isDead) return;
        if (!IsAuthority())
        {
            ClientUpdate();
            return;
        }
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh)
        {
            TryPlaceOnNavMesh();
            return;
        }
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + targetScanInterval;
            cachedPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include);
        }
        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.Return: UpdateReturn(); break;
        }
        HandlePendingHit();
        UpdateAnimatorAuthority();
    }
    private void UpdateIdle()
    {
        Transform found = FindVisibleTarget();
        if (found != null)
        {
            StartChase(found);
            return;
        }
        if (FlatDistance(transform.position, spawnPoint) > 0.6f)
        {
            state = State.Return;
            return;
        }
        if (agent.hasPath) agent.ResetPath();
        agent.updateRotation = false;
        transform.rotation = Quaternion.Slerp(transform.rotation, spawnRotation, Time.deltaTime * turnSpeed * 0.3f);
    }
    private void StartChase(Transform target)
    {
        playerTarget = target;
        state = State.Chase;
        unreachableTimer = 0f;
        nextDestinationTime = 0f;
    }
    private void UpdateChase()
    {
        if (!IsValidTarget(playerTarget))
        {
            Transform other = FindVisibleTarget();
            if (other != null) StartChase(other);
            else BeginReturn();
            return;
        }
        float distance = FlatDistance(transform.position, playerTarget.position);
        float distanceFromSpawn = FlatDistance(transform.position, spawnPoint);
        if (distance > loseTargetRange || distanceFromSpawn > leashRange)
        {
            BeginReturn();
            return;
        }
        Vector3 point;
        if (!TryGetReachablePoint(playerTarget.position, out point))
        {
            unreachableTimer += Time.deltaTime;
            if (unreachableTimer >= unreachableGiveUpTime)
            {
                BeginReturn();
                return;
            }
        }
        else
        {
            unreachableTimer = 0f;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.autoBraking = false;
            agent.stoppingDistance = ChaseStopDistance();
            if (Time.time >= nextDestinationTime)
            {
                nextDestinationTime = Time.time + destinationUpdateInterval;
                agent.SetDestination(point);
            }
        }
        if (distance <= attackRange + 1f)
        {
            agent.updateRotation = false;
            FaceTarget(playerTarget.position);
        }
        else
        {
            agent.updateRotation = true;
        }
        float heightDiff = Mathf.Abs(playerTarget.position.y - transform.position.y);
        if (distance <= attackRange && heightDiff < 2f && Time.time >= lastAttackTime + attackCooldown) StartAttack();
    }
    private void BeginReturn()
    {
        playerTarget = null;
        pendingHitTime = -1f;
        pendingHitTarget = null;
        state = State.Return;
        nextDestinationTime = 0f;
    }
    private void UpdateReturn()
    {
        if (FlatDistance(transform.position, spawnPoint) < leashRange * 0.6f)
        {
            Transform found = FindVisibleTarget();
            if (found != null)
            {
                StartChase(found);
                return;
            }
        }
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.1f;
        agent.updateRotation = true;
        if (Time.time >= nextDestinationTime)
        {
            nextDestinationTime = Time.time + 0.5f;
            Vector3 point;
            if (TryGetReachablePoint(spawnPoint, out point)) agent.SetDestination(point);
            else agent.SetDestination(spawnPoint);
        }
        if (FlatDistance(transform.position, spawnPoint) <= 0.6f)
        {
            agent.ResetPath();
            state = State.Idle;
        }
    }
    private Transform FindVisibleTarget()
    {
        Transform best = null;
        float bestDistance = Mathf.Infinity;
        foreach (PlayerHealth p in cachedPlayers)
        {
            if (p == null || !IsValidTarget(p.transform)) continue;
            float d = FlatDistance(transform.position, p.transform.position);
            if (d > detectionRange) continue;
            if (d > closeDetectionRange && !HasLineOfSight(p.transform)) continue;
            Vector3 point;
            if (!TryGetReachablePoint(p.transform.position, out point)) continue;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = p.transform;
            }
        }
        return best;
    }
    private bool IsValidTarget(Transform t)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return false;
        CharController_Motor motor = t.GetComponent<CharController_Motor>();
        if (motor != null && !motor.enabled) return false;
        CharacterController cc = t.GetComponent<CharacterController>();
        if (cc != null && !cc.enabled) return false;
        return true;
    }
    private bool HasLineOfSight(Transform target)
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 end = target.position + Vector3.up * 1.2f;
        Vector3 dir = end - origin;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir / dist, dist, sightMask, QueryTriggerInteraction.Ignore);
        float closest = Mathf.Infinity;
        Transform closestHit = null;
        foreach (RaycastHit h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;
            if (h.distance < closest)
            {
                closest = h.distance;
                closestHit = h.transform;
            }
        }
        if (closestHit == null) return true;
        return closestHit.IsChildOf(target);
    }
    private bool TryGetReachablePoint(Vector3 position, out Vector3 point)
    {
        point = position;
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas)) return false;
        if (FlatDistance(hit.position, position) > 1f) return false;
        point = hit.position;
        return true;
    }
    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
    }
    private void StartAttack()
    {
        lastAttackTime = Time.time;
        pendingHitTime = Time.time + attackHitDelay;
        pendingHitTarget = playerTarget;
        if (PhotonNetwork.InRoom && photonView != null) photonView.RPC(nameof(RPC_PlayAttack), RpcTarget.All);
        else PlayAttackLocal();
    }
    private void HandlePendingHit()
    {
        if (pendingHitTime < 0f || Time.time < pendingHitTime) return;
        pendingHitTime = -1f;
        Transform t = pendingHitTarget;
        pendingHitTarget = null;
        if (!IsValidTarget(t)) return;
        if (FlatDistance(transform.position, t.position) > attackRange + 0.6f) return;
        DealDamage(t);
    }
    private void DealDamage(Transform target)
    {
        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health == null) health = target.GetComponentInChildren<PlayerHealth>();
        if (health == null) return;
        int damage = Mathf.RoundToInt(attackDamage);
        if (PhotonNetwork.InRoom && photonView != null)
        {
            PhotonView pv = health.GetComponent<PhotonView>();
            if (pv != null)
            {
                photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, pv.ViewID, damage);
                return;
            }
        }
        health.TakeDamage(damage);
    }
    [PunRPC]
    private void RPC_PlayAttack()
    {
        PlayAttackLocal();
    }
    [PunRPC]
    private void RPC_ApplyDamage(int viewId, int damage)
    {
        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null || !pv.IsMine) return;
        PlayerHealth health = pv.GetComponent<PlayerHealth>();
        if (health == null) health = pv.GetComponentInChildren<PlayerHealth>();
        if (health != null) health.TakeDamage(damage);
    }
    private void PlayAttackLocal()
    {
        if (animator != null && hasAttackParam) animator.SetTrigger("Attack");
    }
    private void UpdateAnimatorAuthority()
    {
        Vector3 v = agent.velocity;
        v.y = 0f;
        float target = 0f;
        if (v.magnitude > 0.15f) target = state == State.Chase ? 1f : 0.5f;
        animSpeed = Mathf.MoveTowards(animSpeed, target, Time.deltaTime * 4f);
        if (animator != null && hasSpeedParam) animator.SetFloat("Speed", animSpeed);
    }
    private void ClientUpdate()
    {
        if (Vector3.Distance(transform.position, networkPosition) > 5f) transform.position = networkPosition;
        else transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * 10f);
        animSpeed = Mathf.MoveTowards(animSpeed, networkAnimSpeed, Time.deltaTime * 4f);
        if (animator != null && hasSpeedParam) animator.SetFloat("Speed", animSpeed);
    }
    private float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
    public void SetPlayer(Transform target)
    {
        if (isDead || !IsAuthority() || !IsValidTarget(target)) return;
        StartChase(target);
    }
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
        {
            if (photonView != null) photonView.RPC(nameof(RPC_EnemyTakeDamage), RpcTarget.MasterClient, damage);
            return;
        }
        currentHealth -= Mathf.Max(0f, damage);
        if (currentHealth <= 0f) Die();
    }
    [PunRPC]
    private void RPC_EnemyTakeDamage(float damage)
    {
        if (PhotonNetwork.IsMasterClient) TakeDamage(damage);
    }
    public void Die()
    {
        if (isDead) return;
        if (PhotonNetwork.InRoom)
        {
            if (photonView == null) return;
            if (PhotonNetwork.IsMasterClient) photonView.RPC(nameof(RPC_EnemyDie), RpcTarget.All);
            else photonView.RPC(nameof(RPC_EnemyRequestDie), RpcTarget.MasterClient);
            return;
        }
        RPC_EnemyDie();
    }
    [PunRPC]
    private void RPC_EnemyRequestDie()
    {
        if (PhotonNetwork.IsMasterClient) Die();
    }
    [PunRPC]
    private void RPC_EnemyDie()
    {
        if (isDead) return;
        isDead = true;
        playerTarget = null;
        pendingHitTarget = null;
        pendingHitTime = -1f;
        if (animator != null && hasDieParam) animator.SetTrigger("Die");
        if (agent != null) agent.enabled = false;
        if (!PhotonNetwork.InRoom) Destroy(gameObject, 5f);
        else if (PhotonNetwork.IsMasterClient) Invoke(nameof(NetworkDestroy), 5f);
    }
    private void NetworkDestroy()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(gameObject);
    }
    private void OnDestroy()
    {
        CancelInvoke();
        playerTarget = null;
        pendingHitTarget = null;
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(animSpeed);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkAnimSpeed = (float)stream.ReceiveNext();
        }
    }
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? spawnPoint : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, leashRange);
    }
}
