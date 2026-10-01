using System;
using UnityEngine;
using UnityEngine.UI;
public class PwShopUI : MonoBehaviour
{
    [Serializable]
    public class Card
    {
        public int section;
        public int kind;
        public int flag;
        public int price;
        public int skinIndex = -1;
        public int lampIndex = -1;
        public Button button;
        public Text priceText;
        public Text stateText;
    }
    [Serializable]
    public class Section
    {
        public GameObject root;
        public Button tab;
        public Image tabFill;
        public Text tabText;
    }
    private const float AdCooldown = 60f;
    public GameObject panel;
    public Button closeButton;
    public Text pointsText;
    public Text messageText;
    public Section[] sections;
    public Card[] cards;
    private int current;
    private float messageUntil;
    private float adReadyAt;
    private bool adBusy;
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                if (card == null || card.button == null) continue;
                int index = i;
                card.button.onClick.AddListener(() => OnCard(index));
            }
        }
        if (sections == null) return;
        for (int i = 0; i < sections.Length; i++)
        {
            Section section = sections[i];
            if (section == null || section.tab == null) continue;
            int index = i;
            section.tab.onClick.AddListener(() => Select(index));
        }
    }
    private void OnEnable()
    {
        PwShop.Changed += Refresh;
        PwPoints.Changed += OnPoints;
        Select(current);
    }
    private void OnDisable()
    {
        PwShop.Changed -= Refresh;
        PwPoints.Changed -= OnPoints;
    }
    private void Update()
    {
        if (messageText != null && messageText.gameObject.activeSelf && Time.unscaledTime > messageUntil) messageText.gameObject.SetActive(false);
    }
    private void OnPoints(int value)
    {
        Refresh();
    }
    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Select(current);
    }
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
    public void Select(int index)
    {
        current = index;
        if (sections != null)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                Section section = sections[i];
                if (section == null) continue;
                bool on = i == index;
                if (section.root != null && section.root.activeSelf != on) section.root.SetActive(on);
                if (section.tabFill != null) section.tabFill.color = on ? new Color(0.55f, 0.08f, 0.07f, 1f) : new Color(0.14f, 0.12f, 0.13f, 1f);
                if (section.tabText != null) section.tabText.color = on ? new Color(1f, 0.92f, 0.85f, 1f) : new Color(0.72f, 0.66f, 0.64f, 1f);
            }
        }
        Refresh();
    }
    private void OnCard(int index)
    {
        if (cards == null || index < 0 || index >= cards.Length) return;
        Card card = cards[index];
        if (card == null) return;
        if (card.kind == 1)
        {
            WatchAd();
            return;
        }
        if (card.flag != 0 && !PwShop.Owns(card.flag))
        {
            if (!PwShop.Buy(card.flag, card.price))
            {
                Message("Not enough points");
                return;
            }
        }
        if (card.skinIndex >= 0) PwShop.Equip(card.skinIndex);
        if (card.lampIndex >= 0) PwShop.EquipLamp(card.lampIndex);
        Refresh();
        ApplyPreview();
    }
    private void WatchAd()
    {
        if (adBusy) return;
        if (Time.unscaledTime < adReadyAt)
        {
            Message("Please wait");
            return;
        }
        if (!PwAds.Available || !PwAds.Ready)
        {
            PwAds.Preload();
            Message("Ad not ready");
            return;
        }
        adBusy = true;
        Message("Loading ad...");
        PwAds.Show(OnAdDone);
    }
    private void OnAdDone(bool rewarded)
    {
        adBusy = false;
        if (rewarded)
        {
            PwPoints.Add(1);
            PwPointsHud.Flash(1);
            adReadyAt = Time.unscaledTime + AdCooldown;
        }
        else
        {
            Message("Ad not ready");
        }
        Refresh();
    }
    private void Message(string value)
    {
        if (messageText == null) return;
        messageText.text = value;
        messageText.gameObject.SetActive(true);
        messageUntil = Time.unscaledTime + 2.5f;
    }
    private void Refresh()
    {
        if (pointsText != null) pointsText.text = PwPoints.Points.ToString();
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            Card card = cards[i];
            if (card == null) continue;
            if (card.kind == 1)
            {
                bool wait = Time.unscaledTime < adReadyAt || adBusy;
                if (card.priceText != null && card.priceText.gameObject.activeSelf) card.priceText.gameObject.SetActive(false);
                if (card.stateText != null) card.stateText.text = wait ? "Please wait" : "Watch Ad";
                if (card.button != null) card.button.interactable = !wait;
                continue;
            }
            bool owns = PwShop.Owns(card.flag);
            bool equipped = owns && ((card.skinIndex >= 0 && PwShop.Skin == card.skinIndex) || (card.lampIndex >= 0 && PwShop.Lamp == card.lampIndex));
            if (card.priceText != null)
            {
                bool showPrice = !owns;
                if (card.priceText.gameObject.activeSelf != showPrice) card.priceText.gameObject.SetActive(showPrice);
                if (showPrice) card.priceText.text = card.price.ToString();
            }
            if (card.stateText != null)
            {
                if (equipped) card.stateText.text = "Equipped";
                else if (owns) card.stateText.text = card.skinIndex >= 0 || card.lampIndex >= 0 ? "Equip" : "Owned";
                else card.stateText.text = "Buy";
            }
            if (card.button != null) card.button.interactable = !equipped && !(owns && card.skinIndex < 0 && card.lampIndex < 0);
        }
        ApplyPreview();
    }
    private void ApplyPreview()
    {
        LobbyManager lobby = FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobby == null || lobby.stageModel == null) return;
        PwLoadout.Tint(lobby.stageModel, PwShop.Skin);
    }
}
