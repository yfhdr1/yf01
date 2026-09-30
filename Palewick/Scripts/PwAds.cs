using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if PW_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif
public class PwAds : MonoBehaviour
{
    public static bool UseTestAd = true;
    public const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";
    public const string AndroidRewardedId = "ca-app-pub-9194615148813735/8226703242";
    public static string UnitId
    {
        get { return UseTestAd ? TestRewardedId : AndroidRewardedId; }
    }
    private static PwAds instance;
#if PW_ADMOB
    private static bool loading;
    private static bool sdkReady;
    private static float nextLoad;
    private static bool started;
    private static RewardedAd ad;
    private static Action<bool> pending;
    private static bool rewarded;
    private static bool closed;
#endif
    public static bool Available
    {
        get
        {
#if PW_ADMOB
            return true;
#else
            return false;
#endif
        }
    }
    public static bool Ready
    {
        get
        {
#if PW_ADMOB
            return ad != null && ad.CanShowAd();
#else
            return false;
#endif
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PwAds");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PwAds>();
    }
    private void Awake()
    {
        instance = this;
        Init();
    }
    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
    private void Update()
    {
#if PW_ADMOB
        if (!sdkReady || Ready || loading) return;
        if (Time.unscaledTime < nextLoad) return;
        Preload();
#endif
    }
    public static void Init()
    {
#if PW_ADMOB
        if (started) return;
        started = true;
        try
        {
            MobileAds.Initialize(status => MobileAdsEventExecutor.ExecuteInUpdate(() => sdkReady = true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_ADS init " + e.Message);
        }
#endif
    }
    public static void Preload()
    {
#if PW_ADMOB
        if (!sdkReady || loading || Ready) return;
        loading = true;
        nextLoad = Time.unscaledTime + 20f;
        try
        {
            Drop();
            AdRequest request = new AdRequest();
            RewardedAd.Load(UnitId, request, (RewardedAd loaded, LoadAdError error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    loading = false;
                    if (error != null || loaded == null)
                    {
                        nextLoad = Time.unscaledTime + 30f;
                        return;
                    }
                    ad = loaded;
                    Hook(ad);
                });
            });
        }
        catch (Exception e)
        {
            loading = false;
            nextLoad = Time.unscaledTime + 30f;
            Debug.LogWarning("PW_ADS load " + e.Message);
        }
#endif
    }
    public static void Show(Action<bool> done)
    {
#if PW_ADMOB
        if (!Ready)
        {
            Preload();
            if (done != null) done(false);
            return;
        }
        pending = done;
        rewarded = false;
        closed = false;
        try
        {
            ad.Show(reward => rewarded = true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_ADS show " + e.Message);
            closed = true;
            Finish();
        }
#else
        if (done != null) done(false);
#endif
    }
#if PW_ADMOB
    private static void Hook(RewardedAd target)
    {
        target.OnAdFullScreenContentClosed += () => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            closed = true;
            Finish();
        });
        target.OnAdFullScreenContentFailed += error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            closed = true;
            Finish();
        });
    }
    private static void Finish()
    {
        if (!closed) return;
        Action<bool> call = pending;
        bool result = rewarded;
        pending = null;
        rewarded = false;
        Drop();
        nextLoad = 0f;
        if (call != null) call(result);
    }
    private static void Drop()
    {
        if (ad == null) return;
        try { ad.Destroy(); }
        catch (Exception) { }
        ad = null;
    }
#endif
}
