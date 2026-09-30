using UnityEngine;
using UnityEngine.UI;
public class LoadingScreenFx : MonoBehaviour
{
    private const float TipInterval = 3.5f;
    private static readonly string[] Tips =
    {
        "Keep your flashlight off to stay hidden",
        "Running makes noise. Walk when it is near",
        "Stay close to your friends",
        "Doors will not stop it forever",
        "Listen for its footsteps",
        "Use headphones for the best experience"
    };
    public RectTransform spinner;
    public Image barFill;
    public Text percentText;
    public Text tipText;
    private float progress;
    private float nextTip;
    private int tipIndex;
    private void OnEnable()
    {
        progress = 0f;
        tipIndex = Random.Range(0, Tips.Length);
        if (tipText != null) tipText.text = Tips[tipIndex];
        nextTip = Time.unscaledTime + TipInterval;
        Apply();
    }
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (spinner != null) spinner.Rotate(0f, 0f, -300f * dt);
        progress += (0.96f - progress) * dt * 0.55f;
        Apply();
        if (tipText != null && Time.unscaledTime >= nextTip)
        {
            nextTip = Time.unscaledTime + TipInterval;
            tipIndex = (tipIndex + 1) % Tips.Length;
            tipText.text = Tips[tipIndex];
        }
    }
    private void Apply()
    {
        if (barFill != null) barFill.fillAmount = progress;
        if (percentText != null) percentText.text = Mathf.RoundToInt(progress * 100f) + "%";
    }
}
