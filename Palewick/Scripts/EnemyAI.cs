using System.Collections.Generic;
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
    public float returnDistance = 15f;
    public float attackRange = 2f;
    public float patrolRadius = 10f;
    public float eyeHeight = 1.6f;
    public LayerMask sightMask = ~0;
    [Header("Senses")]
    public float sprintHearingRange = 16f;
    public float jumpHearingRange = 10f;
    public float walkHearingRange = 4.5f;
    public float doorHearingRange = 12f;
    public float loseSightTime = 3f;
    public float searchTime = 5f;
    public float investigateSpeedMultiplier = 1.6f;
    public float flashlightSlowRange = 9f;
    public float flashlightSlowAngle = 22f;
    public float flashlightSlowFactor = 0.7f;
    public float doorOpenDistance = 1.7f;
    public float patrolPauseMin = 2f;
    public float patrolPauseMax = 5f;
    [Header("Combat")]
    public float maxHealth = 100f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    public float attackHitDelay = 0.4f;
    [Header("Timing")]
    public float destinationUpdateInterval = 0.1f;
    public float targetScanInterval = 0.5f;
    public float chaseSampleDistance = 20f;
    public float stuckTime = 0.4f;
    [Header("Debug")]
    public string currentStateDebug;
    public float debugDistance;
    public float debugVelocity;
    public string debugPath;
    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform playerTarget;
    private enum State { Idle, Chase, Return, Patrol, Investigate, Search }
    private static readonly List<EnemyAI> All = new List<EnemyAI>();
    private readonly Dictionary<PlayerHealth, Vector3> lastPlayerPositions = new Dictionary<PlayerHealth, Vector3>();
    private float lastHearingTime;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private Vector3 investigatePoint;
    private float searchEndTime;
    private float searchBaseYaw;
    private float patrolWaitUntil;
    private bool patrolMoving;
    private float nextDoorCheck;
    private float speedFactor = 1f;
    private State state = State.Idle;
    private Vector3 spawnPoint;
    private Quaternion spawnRotation;
    private PlayerHealth[] cachedPlayers = new PlayerHealth[0];
    private float nextScanTime;
    private float nextDestinationTime;
    private float lastAttackTime = -100f;
    private float pendingHitTime = -1f;
    private Transform pendingHitTarget;
    private float stuckTimer;
    private float moveHoldTimer;
    private Vector3 networkVelocity;
    private float networkReceiveTime;
    private Vector3 lastDestination = Vector3.positiveInfinity;
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
    public override void OnEnable()
    {
        base.OnEnable();
        if (!All.Contains(this)) All.Add(this);
    }
    public override void OnDisable()
    {
        base.OnDisable();
        All.Remove(this);
    }
    public static void HearNoise(Vector3 position, float radius)
    {
        for (int i = 0; i < All.Count; i++)
        {
            EnemyAI e = All[i];
            if (e != null) e.OnNoise(position, radius);
        }
    }
    private void OnNoise(Vector3 position, float radius)
    {
        if (isDead || !IsAuthority() || agent == null || !agent.enabled) return;
        if (radius < 0f) radius = doorHearingRange;
        float d = FlatDistance(transform.position, position);
        if (d < 2.5f || d > radius) return;
        if (state == State.Chase) return;
        BeginInvestigate(position);
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
            ListenToPlayers();
        }
        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(); break;
            case State.Return: UpdateReturn(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Search: UpdateSearch(); break;
        }
        if (Time.time >= nextDoorCheck)
        {
            nextDoorCheck = Time.time + 0.25f;
            OpenDoorAhead();
        }
        HandlePendingHit();
        UpdateAnimatorAuthority();
        currentStateDebug = state.ToString();
        debugDistance = playerTarget != null ? FlatDistance(transform.position, playerTarget.position) : 0f;
        Vector3 dv = agent.velocity;
        dv.y = 0f;
        debugVelocity = dv.magnitude;
        debugPath = agent.pathPending ? "Pending" : (agent.hasPath ? agent.pathStatus.ToString() : "NoPath");
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
        if (patrolRadius > 0.5f && Time.time >= patrolWaitUntil)
        {
            state = State.Patrol;
            patrolMoving = false;
        }
    }
    private void UpdatePatrol()
    {
        Transform found = FindVisibleTarget();
        if (found != null)
        {
            StartChase(found);
            return;
        }
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.2f;
        agent.updateRotation = true;
        if (!patrolMoving)
        {
            if (Time.time < patrolWaitUntil) return;
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 p = spawnPoint + new Vector3(r.x, 0f, r.y);
            Vector3 point;
            if (TryGetReachablePoint(p, out point) && agent.SetDestination(point))
            {
                patrolMoving = true;
            }
            else
            {
                patrolWaitUntil = Time.time + 1f;
            }
            return;
        }
        if (!agent.pathPending && (agent.remainingDistance <= 0.4f || !agent.hasPath))
        {
            patrolMoving = false;
            patrolWaitUntil = Time.time + Random.Range(patrolPauseMin, patrolPauseMax);
        }
    }
    private void BeginInvestigate(Vector3 position)
    {
        Vector3 point;
        if (!TryGetChasePoint(position, out point)) return;
        investigatePoint = point;
        playerTarget = null;
        state = State.Investigate;
        nextDestinationTime = 0f;
        patrolMoving = false;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(investigatePoint);
        }
    }
    private void UpdateInvestigate()
    {
        Transform found = FindVisibleTarget();
        if (found != null)
        {
            StartChase(found);
            return;
        }
        agent.isStopped = false;
        agent.speed = patrolSpeed * investigateSpeedMultiplier;
        agent.autoBraking = true;
        agent.stoppingDistance = 0.5f;
        agent.updateRotation = true;
        if (Time.time >= nextDestinationTime)
        {
            nextDestinationTime = Time.time + 0.5f;
            agent.SetDestination(investigatePoint);
        }
        if (!agent.pathPending && (FlatDistance(transform.position, investigatePoint) <= 1f || (agent.hasPath && agent.remainingDistance <= 0.6f) || agent.pathStatus == NavMeshPathStatus.PathInvalid))
        {
            BeginSearch();
        }
    }
    private void BeginSearch()
    {
        state = State.Search;
        searchEndTime = Time.time + searchTime;
        searchBaseYaw = transform.eulerAngles.y;
        if (agent.hasPath) agent.ResetPath();
    }
    private void UpdateSearch()
    {
        Transform found = FindVisibleTarget();
        if (found != null)
        {
            StartChase(found);
            return;
        }
        agent.updateRotation = false;
        float k = 1f - (searchEndTime - Time.time) / Mathf.Max(0.1f, searchTime);
        float yaw = searchBaseYaw + Mathf.Sin(k * Mathf.PI * 2f) * 80f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), Time.deltaTime * 3f);
        if (Time.time >= searchEndTime)
        {
            BeginReturn();
        }
    }
    private void ListenToPlayers()
    {
        float now = Time.time;
        float dt = Mathf.Max(0.05f, now - lastHearingTime);
        lastHearingTime = now;
        for (int i = 0; i < cachedPlayers.Length; i++)
        {
            PlayerHealth p = cachedPlayers[i];
            if (p == null || !IsValidTarget(p.transform)) continue;
            Vector3 pos = p.transform.position;
            Vector3 old;
            bool had = lastPlayerPositions.TryGetValue(p, out old);
            lastPlayerPositions[p] = pos;
            if (!had || state == State.Chase) continue;
            float flat = FlatDistance(pos, old) / dt;
            float rise = (pos.y - old.y) / dt;
            float radius = 0f;
            if (flat > 4.8f) radius = sprintHearingRange;
            else if (flat > 1.5f) radius = walkHearingRange;
            if (rise > 2.5f) radius = Mathf.Max(radius, jumpHearingRange);
            if (radius <= 0f) continue;
            if (FlatDistance(transform.position, pos) <= radius) BeginInvestigate(pos);
        }
        if (lastPlayerPositions.Count > 16) lastPlayerPositions.Clear();
    }
    private bool LitByFlashlight(Transform target)
    {
        if (target == null) return false;
        Light[] lights = target.GetComponentsInChildren<Light>(false);
        Vector3 me = transform.position + Vector3.up * eyeHeight * 0.8f;
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null || !l.enabled || l.type != LightType.Spot) continue;
            Vector3 to = me - l.transform.position;
            float d = to.magnitude;
            if (d > flashlightSlowRange || d < 0.01f) continue;
            if (Vector3.Angle(l.transform.forward, to) <= flashlightSlowAngle) return true;
        }
        return false;
    }
    private void OpenDoorAhead()
    {
        if (state != State.Chase && state != State.Investigate && state != State.Patrol && state != State.Return) return;
        Vector3 dir = agent.desiredVelocity;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.Normalize();
        Vector3 origin = transform.position + Vector3.up * 1.1f;
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.3f, dir, doorOpenDistance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.IsChildOf(transform)) continue;
            DoorController door = hits[i].transform.GetComponentInParent<DoorController>();
            if (door != null && !door.IsOpen)
            {
                door.OpenFrom(transform.position);
                return;
            }
        }
    }
    private float nextSightCheck;
    private bool cachedSight;
    private bool HasLineOfSightCached(Transform target)
    {
        if (Time.time >= nextSightCheck)
        {
            nextSightCheck = Time.time + 0.25f;
            cachedSight = HasLineOfSight(target);
            speedFactor = LitByFlashlight(target) ? flashlightSlowFactor : 1f;
        }
        return cachedSight;
    }
    private void StartChase(Transform target)
    {
        playerTarget = target;
        lastSeenPosition = target.position;
        lastSeenTime = Time.time;
        speedFactor = 1f;
        state = State.Chase;
        stuckTimer = 0f;
        nextDestinationTime = 0f;
        lastDestination = Vector3.positiveInfinity;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
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
        if (distance <= closeDetectionRange || HasLineOfSightCached(playerTarget))
        {
            lastSeenPosition = playerTarget.position;
            lastSeenTime = Time.time;
        }
        if (distance > returnDistance || Time.time - lastSeenTime > loseSightTime)
        {
            Transform keep = playerTarget;
            Vector3 guess = lastSeenPosition;
            playerTarget = null;
            BeginInvestigate(guess);
            if (state != State.Investigate)
            {
                playerTarget = keep;
                BeginReturn();
            }
            return;
        }
        Vector3 point;
        bool found = TryGetChasePoint(playerTarget.position, out point);
        agent.isStopped = false;
        agent.speed = chaseSpeed * speedFactor;
        agent.autoBraking = false;
        agent.stoppingDistance = ChaseStopDistance();
        if (found && Time.time >= nextDestinationTime)
        {
            nextDestinationTime = Time.time + destinationUpdateInterval;
            if (!agent.hasPath || (point - lastDestination).sqrMagnitude > 0.04f)
            {
                if (agent.SetDestination(point)) lastDestination = point;
            }
        }
        CheckStuck(distance);
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
    private void CheckStuck(float distance)
    {
        Vector3 v = agent.velocity;
        v.y = 0f;
        if (distance > ChaseStopDistance() + 0.3f && !agent.pathPending && v.magnitude < 0.3f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTime)
            {
                stuckTimer = 0f;
                Vector3 point;
                if (playerTarget != null && TryGetChasePoint(playerTarget.position, out point) && agent.SetDestination(point)) lastDestination = point;
                Vector3 dir = agent.steeringTarget - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) agent.velocity = dir.normalized * chaseSpeed * speedFactor;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
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
        Transform seen = FindVisibleTarget();
        if (seen != null)
        {
            StartChase(seen);
            return;
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
            patrolWaitUntil = Time.time + Random.Range(patrolPauseMin, patrolPauseMax);
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
            if (d > Mathf.Min(detectionRange, returnDistance - 1f)) continue;
            if (d > closeDetectionRange && !HasLineOfSight(p.transform)) continue;
            Vector3 point;
            if (!TryGetChasePoint(p.transform.position, out point)) continue;
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
        PlayerHealth health = t.GetComponent<PlayerHealth>();
        if (health != null) return !health.IsDead;
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
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, chaseSampleDistance, NavMesh.AllAreas)) return false;
        if (FlatDistance(hit.position, position) > 1f) return false;
        point = hit.position;
        return true;
    }
    private bool TryGetChasePoint(Vector3 position, out Vector3 point)
    {
        point = position;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        if (NavMesh.SamplePosition(position, out hit, chaseSampleDistance, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        return false;
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
        if (v.magnitude > 0.15f) moveHoldTimer = 0.3f;
        else moveHoldTimer -= Time.deltaTime;
        float target = 0f;
        if (moveHoldTimer > 0f) target = state == State.Chase ? 1f : 0.5f;
        animSpeed = Mathf.MoveTowards(animSpeed, target, Time.deltaTime * 3f);
        if (animator != null && hasSpeedParam) animator.SetFloat("Speed", animSpeed);
    }
    private void ClientUpdate()
    {
        float ahead = Mathf.Min(Time.time - networkReceiveTime, 0.3f);
        Vector3 predicted = networkPosition + networkVelocity * ahead;
        if (Vector3.Distance(transform.position, predicted) > 5f) transform.position = predicted;
        else transform.position = Vector3.Lerp(transform.position, predicted, Time.deltaTime * 12f);
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
        All.Remove(this);
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
            stream.SendNext(agent != null && agent.enabled ? agent.velocity : Vector3.zero);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkAnimSpeed = (float)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += networkVelocity * Mathf.Min(lag, 0.3f);
            networkReceiveTime = Time.time;
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
        Gizmos.DrawWireSphere(center, returnDistance);
    }
}
