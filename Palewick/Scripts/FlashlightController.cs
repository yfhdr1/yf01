using UnityEngine;
using UnityEngine.UI;
public class FlashlightController : MonoBehaviour
{
    public GameObject flashlightObject;
    public Image flashlightIcon;
    private bool isOn = true;
    public void SetFlashlight(GameObject lightObj)
    {
        flashlightObject = lightObj;
        if (flashlightObject == null) return;
        LineRenderer lr = flashlightObject.GetComponentInChildren<LineRenderer>(true);
        if (lr != null) lr.enabled = false;
        isOn = flashlightObject.activeSelf;
        UpdateIconAlpha();
    }
    public void ToggleFlashlight()
    {
        if (flashlightObject == null) return;
        isOn = !isOn;
        flashlightObject.SetActive(isOn);
        UpdateIconAlpha();
    }
    private void UpdateIconAlpha()
    {
        if (flashlightIcon == null) return;
        Color c = flashlightIcon.color;
        c.a = isOn ? 1f : 0.4f;
        flashlightIcon.color = c;
    }
}
