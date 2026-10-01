using UnityEngine;
using UnityEngine.UI;
public class PwPointsHud : MonoBehaviour
{
    private const float FlashTime = 1.4f;
    public Text valueText;
    public Text gainText;
    public RectTransform icon;
    public Image syncDot;
    private static float flashUntil;
    private static int flashValue;
    private int shown = int.MinValue;
    private float pulseUntil;
    public static void Flash(int amount)
    {
        flashValue = amount;
        flashUntil = Time.unscaledTime + FlashTime;
    }
    private void OnEnable()
    {
        PwPoints.Changed += OnChanged;
        Apply(PwPoints.Points);
    }
    private void OnDisable()
    {
        PwPoints.Changed -= OnChanged;
    }
    private void OnChanged(int value)
    {
        Apply(value);
    }
    private void Apply(int value)
    {
        if (value != shown)
        {
            shown = value;
            pulseUntil = Time.unscaledTime + 0.35f;
        }
        if (valueText != null) valueText.text = value.ToString();
    }
    private void Update()
    {
        float now = Time.unscaledTime;
        if (icon != null)
        {
            float k = pulseUntil > now ? (pulseUntil - now) / 0.35f : 0f;
            icon.localScale = Vector3.one * (1f + 0.35f * k);
        }
        if (gainText != null)
        {
            bool on = flashUntil > now && flashValue != 0;
            if (gainText.gameObject.activeSelf != on) gainText.gameObject.SetActive(on);
            if (on)
            {
                float t = 1f - (flashUntil - now) / FlashTime;
                gainText.text = (flashValue > 0 ? "+" : string.Empty) + flashValue;
                Color c = gainText.color;
                c.a = 1f - t * t;
                gainText.color = c;
                gainText.rectTransform.anchoredPosition = new Vector2(gainText.rectTransform.anchoredPosition.x, 40f * t);
            }
        }
        if (syncDot != null)
        {
            bool online = PwCloud.SignedIn;
            Color c = online ? new Color(0.45f, 0.9f, 0.4f, 1f) : new Color(0.95f, 0.22f, 0.18f, 1f);
            if (online && !PwPoints.Synced) c = new Color(0.95f, 0.75f, 0.2f, 1f);
            syncDot.color = c;
        }
    }
}
