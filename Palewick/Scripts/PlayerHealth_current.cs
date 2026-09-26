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
    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        currentHealth = maxHealth;
    }
    private void Start()
    {
        FindUIElements();
        UpdateUI();
        if (bloodEffect == null)
        {
            bloodEffect = FindAnyObjectByType<BloodEffectUI>(FindObjectsInactive.Include);
        }
    }
    private void Update()
    {
        if (invincibilityTimer > 0f)
        {
            invincibilityTimer -= Time.deltaTime;
        }
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
            Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("DeathPanel");
                if (panel != null)
                {
                    deathPanel = panel.gameObject;
                }
            }
        }
    }
    [PunRPC]
    public void TakeDamage(int amount, PhotonMessageInfo info = default)
    {
        if (isDead) return;
        if (invincibilityTimer > 0f) return;
        if (PhotonNetwork.InRoom && pv != null && !pv.IsMine) return;
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
        isDead = true;
        if (deathPanel != null) deathPanel.SetActive(true);
        CharController_Motor motor = GetComponent<CharController_Motor>();
        if (motor != null) motor.enabled = false;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        if (PhotonNetwork.InRoom && pv != null) pv.RPC(nameof(RPC_Die), RpcTarget.Others);
    }
    [PunRPC]
    private void RPC_Die()
    {
        isDead = true;
        if (deathPanel != null) deathPanel.SetActive(true);
        CharController_Motor motor = GetComponent<CharController_Motor>();
        if (motor != null) motor.enabled = false;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }
    public void Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
        invincibilityTimer = 2f;
        if (deathPanel != null) deathPanel.SetActive(false);
        CharController_Motor motor = GetComponent<CharController_Motor>();
        if (motor != null) motor.enabled = true;
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = true;
        UpdateUI();
    }
    private void UpdateUI()
    {
        if (healthSlider == null) return;
        healthSlider.maxValue = Mathf.Max(1, maxHealth);
        healthSlider.value = currentHealth;
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
            bool receivedDead = (bool)stream.ReceiveNext();
            if (!pv.IsMine)
            {
                isDead = receivedDead;
                if (deathPanel != null) deathPanel.SetActive(isDead);
                CharController_Motor motor = GetComponent<CharController_Motor>();
                if (motor != null) motor.enabled = !isDead;
                CharacterController controller = GetComponent<CharacterController>();
                if (controller != null) controller.enabled = !isDead;
                UpdateUI();
            }
        }
    }
}
