using System;
using UnityEngine;
using UnityEngine.UI;
public class PwShopUI : MonoBehaviour
{
    [Serializable]
    public class Card
    {
        public int flag;
        public int price;
        public int skinIndex;
        public Button button;
        public Text priceText;
        public Text stateText;
    }
    public GameObject panel;
    public Button closeButton;
    public Text pointsText;
    public Text messageText;
    public Card[] cards;
    private float messageUntil;
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (cards == null) return;
        for (int i = 0; i < cards.Length; i++)
        {
            Card card = cards[i];
            if (card == null || card.button == null) continue;
            int index = i;
            card.button.onClick.AddListener(() => OnCard(index));
        }
    }
    private void OnEnable()
    {
        PwShop.Changed += Refresh;
        PwPoints.Changed += OnPoints;
        Refresh();
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
        Refresh();
    }
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
    private void OnCard(int index)
    {
        if (cards == null || index < 0 || index >= cards.Length) return;
        Card card = cards[index];
        if (card == null) return;
        if (card.flag != 0 && !PwShop.Owns(card.flag))
        {
            if (!PwShop.Buy(card.flag, card.price))
            {
                Message("Not enough points");
                return;
            }
        }
        if (card.skinIndex >= 0) PwShop.Equip(card.skinIndex);
        Refresh();
        ApplyPreview();
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
            bool owns = PwShop.Owns(card.flag);
            bool equipped = card.skinIndex >= 0 && PwShop.Skin == card.skinIndex && owns;
            if (card.priceText != null)
            {
                bool showPrice = !owns;
                if (card.priceText.gameObject.activeSelf != showPrice) card.priceText.gameObject.SetActive(showPrice);
                if (showPrice) card.priceText.text = card.price.ToString();
            }
            if (card.stateText != null)
            {
                if (equipped) card.stateText.text = "Equipped";
                else if (owns) card.stateText.text = card.skinIndex >= 0 ? "Equip" : "Owned";
                else card.stateText.text = "Buy";
            }
            if (card.button != null) card.button.interactable = !equipped && !(owns && card.skinIndex < 0);
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
