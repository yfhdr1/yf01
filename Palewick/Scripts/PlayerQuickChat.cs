using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
public class PlayerQuickChat : MonoBehaviourPun
{
    public float sendCooldown = 1f;
    private Button openBtn;
    private GameObject chatPanel;
    private Button[] btns = new Button[4];
    private Text notifText;
    private Coroutine clearRt;
    private string[] msgs = new string[] { "MONSTER HERE!", "HELP ME!", "FOLLOW ME!", "RUN!" };
    private bool isBound;
    private bool isLocal;
    private float nextBindTime;
    private float nextSendTime;
    private void Start()
    {
        isLocal = !PhotonNetwork.InRoom || photonView == null || photonView.IsMine;
        if (isLocal) TryBindUI();
    }
    private void Update()
    {
        if (!isLocal || isBound || Time.unscaledTime < nextBindTime) return;
        nextBindTime = Time.unscaledTime + 0.5f;
        TryBindUI();
    }
    private Transform FindQuickChatUI()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas c in canvases)
        {
            if (c == null) continue;
            Transform ui = c.transform.Find("QuickChatUI");
            if (ui != null) return ui;
        }
        return null;
    }
    private void TryBindUI()
    {
        Transform ui = FindQuickChatUI();
        if (ui == null) return;
        Transform openTr = ui.Find("OpenChatBtn");
        Transform panelTr = ui.Find("ChatPanel");
        Transform notifTr = ui.Find("ChatNotification");
        if (openTr == null || panelTr == null || notifTr == null) return;
        openBtn = openTr.GetComponent<Button>();
        chatPanel = panelTr.gameObject;
        notifText = notifTr.GetComponent<Text>();
        if (openBtn == null) return;
        for (int i = 0; i < 4; i++)
        {
            Transform bTr = panelTr.Find("Btn" + i);
            btns[i] = bTr != null ? bTr.GetComponent<Button>() : null;
        }
        openBtn.onClick.RemoveAllListeners();
        openBtn.onClick.AddListener(TogglePanel);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            if (btns[i] != null)
            {
                btns[i].onClick.RemoveAllListeners();
                btns[i].onClick.AddListener(() => SendMsg(index));
            }
        }
        chatPanel.SetActive(false);
        isBound = true;
    }
    private void TogglePanel()
    {
        if (chatPanel != null) chatPanel.SetActive(!chatPanel.activeSelf);
    }
    private void SendMsg(int i)
    {
        if (chatPanel != null) chatPanel.SetActive(false);
        if (Time.unscaledTime < nextSendTime) return;
        nextSendTime = Time.unscaledTime + sendCooldown;
        string n = PhotonNetwork.InRoom ? PhotonNetwork.NickName : "Player";
        if (string.IsNullOrEmpty(n)) n = "Player";
        if (PhotonNetwork.InRoom && photonView != null)
        {
            photonView.RPC(nameof(RPC_QChat), RpcTarget.All, i, n);
        }
        else
        {
            RPC_QChat(i, n);
        }
    }
    [PunRPC]
    private void RPC_QChat(int i, string n)
    {
        if (i < 0 || i >= msgs.Length) return;
        if (notifText == null)
        {
            Transform ui = FindQuickChatUI();
            if (ui != null)
            {
                Transform notifTr = ui.Find("ChatNotification");
                if (notifTr != null) notifText = notifTr.GetComponent<Text>();
            }
        }
        if (notifText == null) return;
        notifText.text = "[" + n + "]: " + msgs[i];
        notifText.gameObject.SetActive(true);
        if (clearRt != null) StopCoroutine(clearRt);
        clearRt = StartCoroutine(ClearTxt());
    }
    private IEnumerator ClearTxt()
    {
        yield return new WaitForSeconds(3.5f);
        if (notifText != null) notifText.text = "";
        clearRt = null;
    }
    private void OnDestroy()
    {
        if (!isBound) return;
        if (openBtn != null) openBtn.onClick.RemoveListener(TogglePanel);
        for (int i = 0; i < btns.Length; i++)
        {
            if (btns[i] != null) btns[i].onClick.RemoveAllListeners();
        }
    }
}
