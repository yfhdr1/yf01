using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
public class PlayerHealth : MonoBehaviourPun, IPunObservable
{
    public int maxHealth = 100;
    public int currentHealth;
    public float invincibilityTime = 1f;
    private Slider healthSlider;
    private GameObject deathPanel;
    private bool isDead;
    private float invincibilityTimer;
    private BloodEffectUI bloodEffect;
    private PhotonView pv;
    public bool IsDead
    {
        get { return isDead; }
    }
    private bool IsLocal
    {
        get { return !PhotonNetwork.InRoom || pv == null || pv.IsMine; }
    }
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        currentHealth = maxHealth;
    }
    private void Start()
    {
        if (!IsLocal) return;
        FindUIElements();
        if (bloodEffect == null) bloodEffect = FindAnyObjectByType<BloodEffectUI>(FindObjectsInactive.Include);
        if (deathPanel != null) deathPanel.SetActive(false);
        UpdateUI();
    }
    private void Update()
    {
        if (invincibilityTimer > 0f) invincibilityTimer -= Time.deltaTime;
    }
    private void FindUIElements()
    {
        if (healthSlider == null)
        {
            Slider[] sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include);
            foreach (Slider slider in sliders)
            {
                if (slider != null && slider.gameObject.name.ToLower().Contains("health"))
                {
                    healthSlider = slider;
                    break;
                }
            }
        }
        if (deathPanel == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null) continue;
                Transform panel = FindChildRecursive(canvas.transform, "DeathPanel");
                if (panel != null)
                {
                    deathPanel = panel.gameObject;
                    break;
                }
            }
        }
    }
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }
    [PunRPC]
    public void TakeDamage(int amount, PhotonMessageInfo info = default)
    {
        if (isDead) return;
        if (invincibilityTimer > 0f) return;
        if (!IsLocal) return;
        ApplyDamage(amount);
    }
    private void ApplyDamage(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(0, amount));
        invincibilityTimer = invincibilityTime;
        UpdateUI();
        if (bloodEffect != null) bloodEffect.FlashBlood();
        if (currentHealth <= 0) Die();
    }
    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (deathPanel != null) deathPanel.SetActive(true);
        SetLocalControl(false);
        if (PhotonNetwork.InRoom && pv != null) pv.RPC(nameof(RPC_Die), RpcTarget.Others);
    }
    [PunRPC]
    private void RPC_Die()
    {
        isDead = true;
        currentHealth = 0;
    }
    public void Revive()
    {
        if (!IsLocal) return;
        isDead = false;
        currentHealth = maxHealth;
        invincibilityTimer = 2f;
        if (deathPanel != null) deathPanel.SetActive(false);
        SetLocalControl(true);
        UpdateUI();
        if (PhotonNetwork.InRoom && pv != null) pv.RPC(nameof(RPC_Revive), RpcTarget.Others);
    }
    [PunRPC]
    private void RPC_Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
    }
    private void SetLocalControl(bool value)
    {
        if (!IsLocal) return;
        CharController_Motor motor = GetComponent<CharController_Motor>();
        if (motor != null) motor.enabled = value;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = value;
    }
    private void UpdateUI()
    {
        if (!IsLocal || healthSlider == null) return;
        healthSlider.maxValue = Mathf.Max(1, maxHealth);
        healthSlider.value = currentHealth;
    }
    private void OnDestroy()
    {
        healthSlider = null;
        deathPanel = null;
        bloodEffect = null;
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHealth);
            stream.SendNext(isDead);
        }
        else
        {
            currentHealth = (int)stream.ReceiveNext();
            isDead = (bool)stream.ReceiveNext();
        }
    }
}
