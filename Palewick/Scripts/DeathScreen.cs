using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
public class DeathScreen : MonoBehaviour
{
    private const float RespawnDelay = 5f;
    private const float MessageTime = 2.5f;
    public CanvasGroup group;
    public Image redPulse;
    public RectTransform title;
    public Text countdownText;
    public Button respawnButton;
    public Button leaveButton;
    public Button adButton;
    public Text pointsText;
    public Text costText;
    public Text messageText;
    private float shownAt;
    private bool ready;
    private bool waitingAd;
    private float messageUntil;
    private void Awake()
    {
        if (respawnButton != null) respawnButton.onClick.AddListener(Respawn);
        if (leaveButton != null) leaveButton.onClick.AddListener(Leave);
        if (adButton != null) adButton.onClick.AddListener(WatchAd);
    }
    private void OnDestroy()
    {
        if (respawnButton != null) respawnButton.onClick.RemoveListener(Respawn);
        if (leaveButton != null) leaveButton.onClick.RemoveListener(Leave);
        if (adButton != null) adButton.onClick.RemoveListener(WatchAd);
        PwPoints.Changed -= OnPointsChanged;
    }
    private void OnEnable()
    {
        transform.SetAsLastSibling();
        shownAt = Time.unscaledTime;
        ready = false;
        waitingAd = false;
        messageUntil = 0f;
        if (group != null) group.alpha = 0f;
        if (respawnButton != null) respawnButton.interactable = false;
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = Mathf.CeilToInt(RespawnDelay).ToString();
        }
        if (costText != null) costText.text = "Respawn costs 1 point";
        if (messageText != null) messageText.gameObject.SetActive(false);
        PwPoints.Changed -= OnPointsChanged;
        PwPoints.Changed += OnPointsChanged;
        OnPointsChanged(PwPoints.Points);
        PwAds.Preload();
    }
    private void OnDisable()
    {
        PwPoints.Changed -= OnPointsChanged;
    }
    private void OnPointsChanged(int value)
    {
        if (pointsText != null) pointsText.text = value.ToString();
    }
    private void Update()
    {
        float e = Time.unscaledTime - shownAt;
        if (group != null) group.alpha = Mathf.Clamp01(e / 1.2f);
        if (title != null)
        {
            float k = Mathf.Clamp01(e / 1.5f);
            float s = Mathf.Lerp(1.35f, 1f, 1f - (1f - k) * (1f - k));
            title.localScale = new Vector3(s, s, 1f);
        }
        if (redPulse != null)
        {
            Color c = redPulse.color;
            c.a = 0.55f + 0.3f * Mathf.Sin(Time.unscaledTime * 2.2f);
            redPulse.color = c;
        }
        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime > messageUntil) messageText.gameObject.SetActive(false);
        if (adButton != null) adButton.interactable = !waitingAd && PwAds.Available;
        if (!ready)
        {
            float left = RespawnDelay - e;
            if (left > 0f)
            {
                if (countdownText != null) countdownText.text = Mathf.CeilToInt(left).ToString();
                return;
            }
            ready = true;
            if (countdownText != null) countdownText.gameObject.SetActive(false);
        }
        if (respawnButton != null) respawnButton.interactable = !waitingAd && PwPoints.CanRespawn;
    }
    private void LateUpdate()
    {
        FitLabel(respawnButton);
        FitLabel(leaveButton);
        FitLabel(adButton);
    }
    private static void FitLabel(Button b)
    {
        if (b == null) return;
        Text t = b.GetComponentInChildren<Text>(true);
        if (t == null) return;
        RectTransform r = t.rectTransform;
        float w = r.rect.width;
        float h = r.rect.height;
        float pw = t.preferredWidth;
        float ph = t.preferredHeight;
        float s = 1f;
        if (pw > w && pw > 0f) s = w / pw;
        if (ph > 0f && ph * s > h) s = h / ph;
        s = Mathf.Clamp(s, 0.3f, 1f);
        if (Mathf.Abs(r.localScale.x - s) > 0.001f) r.localScale = new Vector3(s, s, 1f);
    }
    private void Message(string text)
    {
        if (messageText == null) return;
        messageText.text = text;
        messageText.gameObject.SetActive(true);
        messageUntil = Time.unscaledTime + MessageTime;
    }
    private void Respawn()
    {
        if (!ready || waitingAd) return;
        if (!PwPoints.TrySpend(PwPoints.RespawnCost))
        {
            Message("Not enough points");
            return;
        }
        if (!Revive()) PwPoints.Add(PwPoints.RespawnCost);
    }
    private void WatchAd()
    {
        if (waitingAd) return;
        if (!PwAds.Available)
        {
            Message("Ad not ready");
            return;
        }
        if (!PwAds.Ready)
        {
            PwAds.Preload();
            Message("Ad not ready");
            return;
        }
        waitingAd = true;
        Message("Loading ad...");
        PwAds.Show(OnAdDone);
    }
    private void OnAdDone(bool rewarded)
    {
        waitingAd = false;
        if (!rewarded)
        {
            Message("Ad not ready");
            return;
        }
        PwPoints.Add(1);
        PwPointsHud.Flash(1);
        if (!PwPoints.TrySpend(PwPoints.RespawnCost)) return;
        if (!Revive()) PwPoints.Add(PwPoints.RespawnCost);
    }
    private bool Revive()
    {
        PlayerHealth[] all = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        for (int i = 0; i < all.Length; i++)
        {
            PlayerHealth h = all[i];
            if (h == null || !h.IsDead) continue;
            PhotonView v = h.GetComponent<PhotonView>();
            if (PhotonNetwork.InRoom && v != null && !v.IsMine) continue;
            h.Revive();
            return true;
        }
        gameObject.SetActive(false);
        return false;
    }
    private void Leave()
    {
        PauseMenuPUBG menu = FindAnyObjectByType<PauseMenuPUBG>(FindObjectsInactive.Include);
        if (menu != null)
        {
            menu.SendMessage("ExitToLobby", SendMessageOptions.DontRequireReceiver);
            return;
        }
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Lobby");
    }
}
