using System;
using UnityEngine;
using Object = UnityEngine.Object;
public class PwShop : MonoBehaviour
{
    public const int SkinRed = 1;
    public const int SkinBlack = 2;
    public const int SkinGold = 4;
    public const int FlashUp = 8;
    public const int LampRed = 16;
    public const int LampBlue = 32;
    public const int PriceRed = 5;
    public const int PriceBlack = 5;
    public const int PriceGold = 10;
    public const int PriceFlash = 10;
    public const int PriceLamp = 5;
    private const string OwnedCloudKey = "shop";
    private const string SkinCloudKey = "skin";
    private const string OwnedCache = "pw_shop_";
    private const string SkinCache = "pw_skin_";
    private const float RetryDelay = 6f;
    public static event Action Changed;
    private static PwShop instance;
    private static string account = string.Empty;
    private static int owned;
    private static int skin;
    private static int lamp;
    private static bool loaded;
    private static bool synced;
    private static bool dirty;
    private static bool working;
    private static float nextTry;
    public static int Owned
    {
        get { return owned; }
    }
    public static int Skin
    {
        get { return skin; }
    }
    public static int Lamp
    {
        get { return lamp; }
    }
    public static int Look
    {
        get { return skin + lamp * 10 + (FlashlightUpgraded ? 100 : 0); }
    }
    public static bool FlashlightUpgraded
    {
        get { return (owned & FlashUp) != 0; }
    }
    public static bool Owns(int flag)
    {
        return flag == 0 || (owned & flag) != 0;
    }
    public static int FlagForSkin(int index)
    {
        if (index == 1) return SkinRed;
        if (index == 2) return SkinBlack;
        if (index == 3) return SkinGold;
        return 0;
    }
    public static int FlagForLamp(int index)
    {
        if (index == 1) return LampRed;
        if (index == 2) return LampBlue;
        return 0;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        GameObject go = new GameObject("PwShop");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PwShop>();
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
        else if (dirty) Push();
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveLocal();
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
        owned = PlayerPrefs.GetInt(OwnedCache + account, 0);
        int packed = PlayerPrefs.GetInt(SkinCache + account, 0);
        Unpack(packed);
        loaded = true;
        Raise();
    }
    private static void SaveLocal()
    {
        if (!loaded) return;
        PlayerPrefs.SetInt(OwnedCache + account, owned);
        PlayerPrefs.SetInt(SkinCache + account, Packed());
        PlayerPrefs.Save();
    }
    private static void Raise()
    {
        Action a = Changed;
        if (a != null) a();
    }
    public static bool Buy(int flag, int price)
    {
        if (flag == 0 || Owns(flag)) return false;
        if (!PwPoints.TrySpend(price)) return false;
        owned |= flag;
        dirty = true;
        nextTry = 0f;
        SaveLocal();
        Raise();
        return true;
    }
    public static void Equip(int index)
    {
        int value = Mathf.Clamp(index, 0, 3);
        if (!Owns(FlagForSkin(value))) return;
        if (skin == value) return;
        skin = value;
        dirty = true;
        nextTry = 0f;
        SaveLocal();
        Raise();
    }
    public static void EquipLamp(int index)
    {
        int value = Mathf.Clamp(index, 0, 2);
        if (!Owns(FlagForLamp(value))) return;
        if (lamp == value) return;
        lamp = value;
        dirty = true;
        nextTry = 0f;
        SaveLocal();
        Raise();
    }
    private static int Packed()
    {
        return skin + lamp * 10;
    }
    private static void Unpack(int value)
    {
        skin = Mathf.Clamp(value % 10, 0, 3);
        lamp = Mathf.Clamp(value / 10, 0, 2);
        if (!Owns(FlagForSkin(skin))) skin = 0;
        if (!Owns(FlagForLamp(lamp))) lamp = 0;
    }
    private async void Pull()
    {
        working = true;
        nextTry = Time.unscaledTime + RetryDelay;
        PwCloudValue ownedValue = await PwCloud.LoadIntAsync(OwnedCloudKey);
        if (ownedValue == null || !ownedValue.Ok)
        {
            working = false;
            return;
        }
        PwCloudValue skinValue = await PwCloud.LoadIntAsync(SkinCloudKey);
        if (skinValue == null || !skinValue.Ok)
        {
            working = false;
            return;
        }
        int merged = owned | (ownedValue.Found ? ownedValue.Value : 0);
        bool needSave = merged != (ownedValue.Found ? ownedValue.Value : 0) || !ownedValue.Found;
        owned = merged;
        if (!dirty && skinValue.Found) Unpack(skinValue.Value);
        if (!Owns(FlagForSkin(skin))) skin = 0;
        if (!Owns(FlagForLamp(lamp))) lamp = 0;
        if (!skinValue.Found || skinValue.Value != Packed()) needSave = true;
        SaveLocal();
        Raise();
        if (needSave)
        {
            bool okOwned = await PwCloud.SaveIntAsync(OwnedCloudKey, owned);
            bool okSkin = await PwCloud.SaveIntAsync(SkinCloudKey, Packed());
            if (!okOwned || !okSkin)
            {
                working = false;
                return;
            }
        }
        dirty = false;
        synced = true;
        working = false;
    }
    private async void Push()
    {
        working = true;
        nextTry = Time.unscaledTime + RetryDelay;
        int sentOwned = owned;
        int sentSkin = Packed();
        bool okOwned = await PwCloud.SaveIntAsync(OwnedCloudKey, sentOwned);
        bool okSkin = await PwCloud.SaveIntAsync(SkinCloudKey, sentSkin);
        if (okOwned && okSkin && sentOwned == owned && sentSkin == Packed()) dirty = false;
        working = false;
    }
}
