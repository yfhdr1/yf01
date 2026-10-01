using UnityEngine;
using UnityEngine.UI;
public class PwLanguageUI : MonoBehaviour
{
    private const string LangKey = "pw_lang";
    private const string DoneKey = "pw_lang_set";
    public GameObject panel;
    public Button englishButton;
    public Button arabicButton;
    public Button kurdishButton;
    public static bool Picked
    {
        get { return PlayerPrefs.GetInt(DoneKey, 0) == 1; }
    }
    public static bool Showing { get; private set; }
    private void Awake()
    {
        Font rtl = Resources.Load<Font>("Fonts/UniMahanBilal");
        SetLabel(englishButton, "English", null);
        SetLabel(arabicButton, "العربية", rtl);
        SetLabel(kurdishButton, "کوردی", rtl);
        bool show = !Picked;
        Showing = show;
        if (panel != null) panel.SetActive(show);
    }
    private void Start()
    {
        if (panel != null && panel.activeSelf) panel.transform.SetAsLastSibling();
    }
    private void OnDestroy()
    {
        Showing = false;
    }
    public void PickEnglish()
    {
        Pick(0);
    }
    public void PickArabic()
    {
        Pick(1);
    }
    public void PickKurdish()
    {
        Pick(2);
    }
    private void Pick(int value)
    {
        PlayerPrefs.SetInt(LangKey, value);
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        Showing = false;
        if (panel != null) panel.SetActive(false);
    }
    private static void SetLabel(Button button, string value, Font font)
    {
        if (button == null) return;
        Text label = button.GetComponentInChildren<Text>(true);
        if (label == null) return;
        if (font != null)
        {
            label.font = font;
            label.text = PwRtl.Visual(value);
        }
        else
        {
            label.text = value;
        }
    }
}
