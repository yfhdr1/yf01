using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class AutoRunButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public CharController_Motor playerMotor;
    public Image icon;
    public Color offColor = new Color(1f, 1f, 1f, 0.75f);
    public Color onColor = new Color(1f, 0.55f, 0.55f, 1f);
    private float nextSearchTime;
    private void Awake()
    {
        if (icon == null)
        {
            icon = GetComponent<Image>();
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        CharController_Motor motor = GetMotor();
        if (motor != null)
        {
            motor.ToggleAutoRun();
        }
        transform.localScale = new Vector3(0.9f, 0.9f, 1f);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
    }
    private void OnDisable()
    {
        transform.localScale = Vector3.one;
    }
    private void Update()
    {
        if (icon == null)
        {
            return;
        }
        bool on = playerMotor != null && playerMotor.AutoRun;
        if (on)
        {
            float p = 0.85f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f;
            icon.color = new Color(onColor.r, onColor.g, onColor.b, onColor.a * p);
        }
        else
        {
            icon.color = offColor;
        }
    }
    private CharController_Motor GetMotor()
    {
        if (playerMotor != null && playerMotor.IsLocal)
        {
            return playerMotor;
        }
        if (Time.unscaledTime < nextSearchTime)
        {
            return null;
        }
        nextSearchTime = Time.unscaledTime + 0.5f;
        CharController_Motor[] motors = FindObjectsByType<CharController_Motor>(FindObjectsInactive.Include);
        for (int i = 0; i < motors.Length; i++)
        {
            if (motors[i] != null && motors[i].isActiveAndEnabled && motors[i].IsLocal)
            {
                playerMotor = motors[i];
                return playerMotor;
            }
        }
        return null;
    }
}
