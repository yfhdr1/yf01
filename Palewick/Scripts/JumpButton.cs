using UnityEngine;
using UnityEngine.EventSystems;
public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public CharController_Motor playerMotor;
    private float nextSearchTime;
    public void Setup(CharController_Motor motor)
    {
        playerMotor = motor;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        CharController_Motor motor = GetMotor();
        if (motor != null)
        {
            motor.Jump();
        }
        SetIconScale(0.9f);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        SetIconScale(1f);
    }
    private void OnDisable()
    {
        SetIconScale(1f);
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
    private void SetIconScale(float s)
    {
        transform.localScale = new Vector3(s, s, 1f);
    }
}
