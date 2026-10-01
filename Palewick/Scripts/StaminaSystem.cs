using UnityEngine;
using UnityEngine.UI;
public class StaminaSystem : MonoBehaviour
{
    public float maxStamina = 100f;
    public float drainRate = 25f;
    public float regenRate = 15f;
    public float regenDelay = 1.5f;
    public Slider staminaBar;
    private float currentStamina;
    private float regenTimer;
    private bool isDraining;
    private float lastFrameStamina;
    public float CurrentStamina
    {
        get
        {
            return currentStamina;
        }
    }
    public float MaxStamina
    {
        get
        {
            return Mathf.Max(0f, maxStamina);
        }
    }
    public bool HasStamina
    {
        get
        {
            return currentStamina > 0f;
        }
    }
    public float StaminaPercent
    {
        get
        {
            return MaxStamina > 0f ? Mathf.Clamp01(currentStamina / MaxStamina) : 0f;
        }
    }
    private void Start()
    {
        currentStamina = MaxStamina;
        lastFrameStamina = currentStamina;
        UpdateUI();
    }
    private void Update()
    {
        float staminaLimit = MaxStamina;
        currentStamina = Mathf.Min(currentStamina, staminaLimit);
        if (isDraining)
        {
            currentStamina = Mathf.Max(0f, currentStamina - Mathf.Max(0f, drainRate) * Time.deltaTime);
            regenTimer = 0f;
        }
        else
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= Mathf.Max(0f, regenDelay) && currentStamina < staminaLimit)
                currentStamina = Mathf.Min(staminaLimit, currentStamina + Mathf.Max(0f, regenRate) * Time.deltaTime);
        }
        if (!Mathf.Approximately(currentStamina, lastFrameStamina))
        {
            UpdateUI();
            lastFrameStamina = currentStamina;
        }
    }
    public void StartDraining()
    {
        isDraining = true;
    }
    public void StopDraining()
    {
        isDraining = false;
    }
    private void UpdateUI()
    {
        if (staminaBar != null)
            staminaBar.value = StaminaPercent;
    }
}
