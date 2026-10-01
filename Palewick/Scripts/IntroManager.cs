using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
public class IntroManager : MonoBehaviour
{
    private const float WarningFadeIn = 0.6f;
    private const float WarningHold = 1.8f;
    private const float WarningFadeOut = 0.6f;
    private const float MinLoadingTime = 1.6f;
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
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;
    public Button skipButton;
    public CanvasGroup fadeGroup;
    public CanvasGroup warningGroup;
    public GameObject loadingRoot;
    public Image barFill;
    public Text percentText;
    public Text tipText;
    public RectTransform spinner;
    public string nextScene = "Scene_Lobby";
    private float t;
    private float frameTime;
    private float frameStall;
    private bool firstFrameSeen;
    private bool videoStarted;
    private bool warningDone;
    private bool done;
    private bool activating;
    private float loadStart;
    private float shownProgress;
    private float fadeOutTimer;
    private float nextTip;
    private int tipIndex;
    private AsyncOperation loadOp;
    private void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = true;
        }
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.AddListener(SkipNow);
        }
        if (loadingRoot != null)
        {
            loadingRoot.SetActive(false);
        }
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
        if (warningGroup != null)
        {
            warningGroup.alpha = 0f;
            warningGroup.gameObject.SetActive(true);
        }
        else
        {
            warningDone = true;
        }
    }
    private void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.nearClipPlane = 0.1f;
        }
        if (videoPlayer == null)
        {
            return;
        }
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = Application.streamingAssetsPath + "/sarata.mp4";
        videoPlayer.targetCameraAlpha = 1f;
        videoPlayer.skipOnDrop = true;
        videoPlayer.sendFrameReadyEvents = true;
        if (audioSource != null)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += OnError;
        videoPlayer.loopPointReached += OnEnd;
        videoPlayer.frameReady += OnFrame;
        videoPlayer.Prepare();
        if (warningDone)
        {
            StartVideo();
        }
    }
    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(SkipNow);
        }
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.errorReceived -= OnError;
            videoPlayer.loopPointReached -= OnEnd;
            videoPlayer.frameReady -= OnFrame;
        }
    }
    private void StartVideo()
    {
        if (videoStarted || done)
        {
            return;
        }
        videoStarted = true;
        t = 0f;
        if (videoPlayer == null)
        {
            return;
        }
        videoPlayer.Play();
    }
    private void OnPrepared(VideoPlayer vp)
    {
        if (videoStarted && !vp.isPlaying && !done)
        {
            vp.Play();
        }
    }
    private void OnError(VideoPlayer vp, string msg)
    {
        Debug.LogWarning("Intro video error: " + msg);
        SkipNow();
    }
    private void OnFrame(VideoPlayer vp, long frameIdx)
    {
        if (!videoStarted)
        {
            return;
        }
        firstFrameSeen = true;
        frameTime = Time.time;
        frameStall = 0f;
    }
    private void OnEnd(VideoPlayer vp)
    {
        SkipNow();
    }
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        t += dt;
        if (!warningDone)
        {
            UpdateWarning();
            return;
        }
        if (done)
        {
            UpdateLoading(dt);
            return;
        }
        if (t > 2f && skipButton != null && !skipButton.gameObject.activeSelf)
        {
            skipButton.gameObject.SetActive(true);
        }
        if (videoPlayer == null)
        {
            if (t > 1f)
            {
                SkipNow();
            }
            return;
        }
        if (firstFrameSeen)
        {
            frameStall += dt;
            if (frameStall > 0.5f)
            {
                frameStall = 0f;
                if (Time.time - frameTime > 6f)
                {
                    SkipNow();
                }
            }
        }
        else if (t > 10f)
        {
            SkipNow();
        }
    }
    private void UpdateWarning()
    {
        float a;
        if (t < WarningFadeIn)
        {
            a = t / WarningFadeIn;
        }
        else if (t < WarningFadeIn + WarningHold)
        {
            a = 1f;
            if (Input.GetMouseButtonDown(0))
            {
                t = WarningFadeIn + WarningHold;
            }
        }
        else
        {
            a = 1f - (t - WarningFadeIn - WarningHold) / WarningFadeOut;
        }
        warningGroup.alpha = Mathf.Clamp01(a);
        if (t >= WarningFadeIn + WarningHold + WarningFadeOut)
        {
            warningGroup.alpha = 0f;
            warningGroup.gameObject.SetActive(false);
            warningDone = true;
            StartVideo();
        }
    }
    private void UpdateLoading(float dt)
    {
        if (spinner != null)
        {
            spinner.Rotate(0f, 0f, -300f * dt);
        }
        if (fadeGroup != null && !activating && fadeOutTimer <= 0f)
        {
            fadeGroup.alpha = Mathf.MoveTowards(fadeGroup.alpha, 0f, dt * 3f);
        }
        if (tipText != null && Time.unscaledTime >= nextTip)
        {
            nextTip = Time.unscaledTime + TipInterval;
            tipIndex = (tipIndex + 1) % Tips.Length;
            tipText.text = Tips[tipIndex];
        }
        if (loadOp == null)
        {
            return;
        }
        float target = Mathf.Clamp01(loadOp.progress / 0.9f);
        float elapsed = Time.unscaledTime - loadStart;
        float timeCap = Mathf.Clamp01(elapsed / MinLoadingTime);
        shownProgress = Mathf.MoveTowards(shownProgress, Mathf.Min(target, timeCap), dt * 1.2f);
        if (barFill != null)
        {
            barFill.fillAmount = shownProgress;
        }
        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(shownProgress * 100f) + "%";
        }
        if (activating || shownProgress < 0.999f)
        {
            return;
        }
        fadeOutTimer += dt;
        if (fadeGroup != null)
        {
            fadeGroup.alpha = Mathf.Clamp01(fadeOutTimer / 0.35f);
        }
        if (fadeOutTimer >= 0.35f)
        {
            activating = true;
            loadOp.allowSceneActivation = true;
        }
    }
    private void SkipNow()
    {
        if (done)
        {
            return;
        }
        done = true;
        warningDone = true;
        if (warningGroup != null)
        {
            warningGroup.gameObject.SetActive(false);
        }
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (loadingRoot != null)
        {
            loadingRoot.SetActive(true);
        }
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f;
        }
        tipIndex = Random.Range(0, Tips.Length);
        if (tipText != null)
        {
            tipText.text = Tips[tipIndex];
        }
        nextTip = Time.unscaledTime + TipInterval;
        if (barFill != null)
        {
            barFill.fillAmount = 0f;
        }
        loadStart = Time.unscaledTime;
        if (loadOp == null)
        {
            loadOp = SceneManager.LoadSceneAsync(nextScene);
            if (loadOp != null)
            {
                loadOp.allowSceneActivation = false;
            }
        }
    }
}
