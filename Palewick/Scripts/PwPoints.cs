using System;
using UnityEngine;
using Object = UnityEngine.Object;
public class PwPoints : MonoBehaviour
{
    public const int StartPoints = 3;
    public const int RespawnCost = 1;
    private const string CloudKey = "points";
    private const string CacheKey = "pw_points_";
    private const string PendingKey = "pw_pending_";
    private const float RetryDelay = 6f;
    public static event Action<int> Changed;
    private static PwPoints instance;
    private static string account = string.Empty;
    private static int points;
    private static int pending;
    private static bool loaded;
    private static bool synced;
    private static bool working;
    private static float nextTry;
    public static int Points
    {
        get { return points; }
    }
    public static bool Synced
    {
        get { return synced; }
    }
    public static bool CanRespawn
    {
        get { return points >= RespawnCost; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PwPoints");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PwPoints>();
    }
    private void Awake()
    {
        instance = this;
        PwCloud.Changed += OnCloudChanged;
        LoadLocal();
    }
    private void OnDestroy()
    {
        PwCloud.Changed -= OnCloudChanged;
        if (instance == this) instance = null;
    }
    private void Update()
    {
        if (working || Time.unscaledTime < nextTry) return;
        if (!PwCloud.SignedIn)
        {
            nextTry = Time.unscaledTime + 1f;
            return;
        }
        if (!synced) Pull();
        else if (pending != 0) Push();
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveLocal();
    }
    private void OnApplicationQuit()
    {
        SaveLocal();
    }
    private void OnCloudChanged()
    {
        if (account != PwCloud.PlayerId)
        {
            synced = false;
            nextTry = 0f;
            LoadLocal();
        }
    }
    private static void LoadLocal()
    {
        account = PwCloud.PlayerId;
        points = Mathf.Max(0, PlayerPrefs.GetInt(CacheKey + account, StartPoints));
        pending = PlayerPrefs.GetInt(PendingKey + account, 0);
        loaded = true;
        Raise();
    }
    private static void SaveLocal()
    {
        if (!loaded) return;
        PlayerPrefs.SetInt(CacheKey + account, points);
        PlayerPrefs.SetInt(PendingKey + account, pending);
        PlayerPrefs.Save();
    }
    private static void Raise()
    {
        Action<int> a = Changed;
        if (a != null) a(points);
    }
    private static void Kick()
    {
        nextTry = 0f;
    }
    public static void Add(int amount)
    {
        if (amount <= 0) return;
        points += amount;
        pending += amount;
        SaveLocal();
        Raise();
        Kick();
    }
    public static bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (points < amount) return false;
        points -= amount;
        pending -= amount;
        SaveLocal();
        Raise();
        Kick();
        return true;
    }
    private async void Pull()
    {
        working = true;
        nextTry = Time.unscaledTime + RetryDelay;
        PwCloudValue value = await PwCloud.LoadIntAsync(CloudKey);
        if (value == null || !value.Ok)
        {
            working = false;
            return;
        }
        int baseValue = value.Found ? value.Value : StartPoints;
        int total = Mathf.Max(0, baseValue + pending);
        bool needSave = !value.Found || pending != 0;
        points = total;
        SaveLocal();
        Raise();
        if (needSave)
        {
            int sent = pending;
            bool ok = await PwCloud.SaveIntAsync(CloudKey, total);
            if (!ok)
            {
                working = false;
                return;
            }
            pending -= sent;
        }
        else
        {
            pending = 0;
        }
        synced = true;
        SaveLocal();
        working = false;
    }
    private async void Push()
    {
        working = true;
        nextTry = Time.unscaledTime + RetryDelay;
        int sent = pending;
        int value = points;
        bool ok = await PwCloud.SaveIntAsync(CloudKey, value);
        if (ok)
        {
            pending -= sent;
            SaveLocal();
        }
        working = false;
    }
}
