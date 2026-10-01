using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PwLanguageScreen : MonoBehaviour
{
    private const string LangKey = "pw_lang";
    public string nextScene = "Scene_Lobby";
    public Button englishButton;
    public Button arabicButton;
    public Button kurdishButton;
    public CanvasGroup fade;
    private bool going;
    private float t;
    private void Start()
    {
        if (fade != null) fade.alpha = 1f;
        if (englishButton != null) englishButton.onClick.AddListener(PickEnglish);
        if (arabicButton != null) arabicButton.onClick.AddListener(PickArabic);
        if (kurdishButton != null) kurdishButton.onClick.AddListener(PickKurdish);
    }
    private void OnDestroy()
    {
        if (englishButton != null) englishButton.onClick.RemoveListener(PickEnglish);
        if (arabicButton != null) arabicButton.onClick.RemoveListener(PickArabic);
        if (kurdishButton != null) kurdishButton.onClick.RemoveListener(PickKurdish);
    }
    private void Update()
    {
        if (fade == null) return;
        if (!going)
        {
            t = Mathf.Min(t + Time.unscaledDeltaTime * 1.6f, 1f);
            fade.alpha = 1f - t;
            return;
        }
        t = Mathf.Min(t + Time.unscaledDeltaTime * 2.2f, 1f);
        fade.alpha = t;
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
        if (going) return;
        going = true;
        t = 0f;
        PlayerPrefs.SetInt(LangKey, value);
        PlayerPrefs.Save();
        Invoke(nameof(Go), 0.45f);
    }
    private void Go()
    {
        SceneManager.LoadScene(nextScene);
    }
}
