using UnityEngine;
using UnityEngine.UI;
public class BloodEffectUI : MonoBehaviour
{
    private Image bloodImage;
    private float alpha;
    private void Awake()
    {
        FindOrCreateOverlay();
    }
    private void FindOrCreateOverlay()
    {
        if (bloodImage != null) return;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) return;
        Transform existing = canvas.transform.Find("BloodOverlay");
        if (existing != null)
        {
            bloodImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject g = new GameObject("BloodOverlay");
            g.transform.SetParent(canvas.transform, false);
            RectTransform rt = g.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            bloodImage = g.AddComponent<Image>();
        }
        if (bloodImage != null)
        {
            bloodImage.raycastTarget = false;
            bloodImage.color = new Color(0.75f, 0f, 0f, alpha);
        }
    }
    private void Update()
    {
        if (bloodImage != null && alpha > 0f)
        {
            alpha = Mathf.MoveTowards(alpha, 0f, Time.unscaledDeltaTime * 1.8f);
            bloodImage.color = new Color(0.75f, 0f, 0f, alpha);
        }
    }
    public void FlashBlood()
    {
        FindOrCreateOverlay();
        alpha = 0.65f;
        if (bloodImage != null)
        {
            bloodImage.color = new Color(0.75f, 0f, 0f, alpha);
        }
    }
}
