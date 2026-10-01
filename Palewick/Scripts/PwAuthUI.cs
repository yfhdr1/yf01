using UnityEngine;
using UnityEngine.UI;
public class PwAuthUI : MonoBehaviour
{
    private const string OfflineKey = "pw_offline_ok";
    public GameObject panel;
    public InputField emailInput;
    public InputField passwordInput;
    public Button signInButton;
    public Button signUpButton;
    public Button googleButton;
    public Button offlineButton;
    public Button signOutButton;
    public Text statusText;
    public Text accountText;
    public RectTransform spinner;
    private bool working;
    private bool offlineMode;
    private bool noNet;
    public static bool Ready
    {
        get { return PwCloud.SignedIn || PlayerPrefs.GetInt(OfflineKey, 0) == 1; }
    }
    private void Awake()
    {
        if (passwordInput != null)
        {
            passwordInput.contentType = InputField.ContentType.Password;
            passwordInput.asteriskChar = '*';
        }
        if (emailInput != null) emailInput.contentType = InputField.ContentType.EmailAddress;
    }
    private void OnEnable()
    {
        PwCloud.Changed += Refresh;
    }
    private void OnDisable()
    {
        PwCloud.Changed -= Refresh;
    }
    private void Start()
    {
        if (emailInput != null) emailInput.text = PwCloud.SavedAccountType == "email" ? PwCloud.SavedAccount : string.Empty;
        bool known = PwCloud.HasAccount || PlayerPrefs.GetInt(OfflineKey, 0) == 1;
        if (panel != null) panel.SetActive(!known);
        if (known) offlineMode = true;
        Resume(known);
    }
    private void Update()
    {
        if (spinner != null)
        {
            bool spin = working;
            if (spinner.gameObject.activeSelf != spin) spinner.gameObject.SetActive(spin);
            if (spin) spinner.Rotate(0f, 0f, -260f * Time.unscaledDeltaTime);
        }
    }
    private async void Resume(bool silent)
    {
        if (!silent) SetStatus("Connecting...");
        SetWorking(!silent);
        string error = await PwCloud.ResumeAsync();
        SetWorking(false);
        noNet = error == PwCloud.NoInternet;
        if (error == null)
        {
            offlineMode = false;
            Close();
            return;
        }
        if (silent)
        {
            Refresh();
            return;
        }
        Open();
        SetStatus(error == PwCloud.NeedLogin ? "Sign in to play" : error);
    }
    public void SignIn()
    {
        Enter(false);
    }
    public void SignUp()
    {
        Enter(true);
    }
    private async void Enter(bool create)
    {
        if (working) return;
        string mail = emailInput != null ? emailInput.text : string.Empty;
        string pass = passwordInput != null ? passwordInput.text : string.Empty;
        SetStatus("Signing in...");
        SetWorking(true);
        string error = await PwCloud.SignInEmailAsync(mail, pass, create);
        SetWorking(false);
        noNet = error == PwCloud.NoInternet;
        if (error == null)
        {
            offlineMode = false;
            Close();
            return;
        }
        SetStatus(error);
        Refresh();
    }
    public async void SignInGoogle()
    {
        if (working) return;
        if (!PwCloud.GoogleAvailable)
        {
            SetStatus("Google sign-in not available");
            return;
        }
        SetStatus("Signing in...");
        SetWorking(true);
        string error = await PwCloud.SignInGoogleAsync();
        SetWorking(false);
        noNet = error == PwCloud.NoInternet;
        if (error == null)
        {
            offlineMode = false;
            Close();
            return;
        }
        SetStatus(error);
        Refresh();
    }
    public void PlayOffline()
    {
        offlineMode = true;
        PlayerPrefs.SetInt(OfflineKey, 1);
        PlayerPrefs.Save();
        if (panel != null) panel.SetActive(false);
        SetStatus(string.Empty);
        Refresh();
    }
    public void SignOut()
    {
        PlayerPrefs.SetInt(OfflineKey, 0);
        PlayerPrefs.Save();
        offlineMode = false;
        PwCloud.SignOut();
        if (passwordInput != null) passwordInput.text = string.Empty;
        Open();
        SetStatus("Sign in to play");
    }
    public void OpenPanel()
    {
        Open();
        SetStatus(string.Empty);
    }
    private void Open()
    {
        if (panel != null && !panel.activeSelf) panel.SetActive(true);
        Refresh();
    }
    private void Close()
    {
        if (PwCloud.SignedIn)
        {
            PlayerPrefs.SetInt(OfflineKey, 1);
            PlayerPrefs.Save();
        }
        if (panel != null && panel.activeSelf) panel.SetActive(false);
        SetStatus(string.Empty);
        Refresh();
    }
    private void SetWorking(bool value)
    {
        working = value;
        Refresh();
    }
    private void SetStatus(string value)
    {
        string text = value == null ? string.Empty : value;
        if (statusText == null) return;
        statusText.text = text;
        statusText.gameObject.SetActive(text.Length > 0);
    }
    private void Refresh()
    {
        bool signedIn = PwCloud.SignedIn;
        if (signInButton != null) signInButton.interactable = !working;
        if (signUpButton != null) signUpButton.interactable = !working;
        if (googleButton != null) googleButton.interactable = !working && PwCloud.GoogleAvailable;
        if (offlineButton != null)
        {
            bool show = !signedIn && (noNet || PwCloud.HasAccount);
            if (offlineButton.gameObject.activeSelf != show) offlineButton.gameObject.SetActive(show);
            offlineButton.interactable = !working;
        }
        if (signOutButton != null)
        {
            bool show = signedIn || offlineMode;
            if (signOutButton.gameObject.activeSelf != show) signOutButton.gameObject.SetActive(show);
        }
        if (accountText != null)
        {
            string label = PwCloud.HasAccount ? PwCloud.SavedAccount : string.Empty;
            if (!signedIn && label.Length > 0) label = label + " (Offline)";
            accountText.text = label;
        }
    }
}
