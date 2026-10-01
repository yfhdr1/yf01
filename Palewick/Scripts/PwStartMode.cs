using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
public class PwStartMode : MonoBehaviour
{
    public GameObject panel;
    public LobbyManager lobby;
    public Button onlineButton;
    public Button offlineButton;
    public Button closeButton;
    public Text onlineNote;
    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }
    private void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        bool net = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
        if (onlineButton != null) onlineButton.interactable = net;
        if (onlineNote != null)
        {
            bool show = !net;
            if (onlineNote.gameObject.activeSelf != show) onlineNote.gameObject.SetActive(show);
        }
    }
    public void Open()
    {
        if (lobby == null) lobby = FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (panel == null)
        {
            PlayOnline();
            return;
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
    public void PlayOnline()
    {
        Close();
        if (lobby == null) lobby = FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobby != null) lobby.OnStartPressed();
    }
    public void PlayOffline()
    {
        Close();
        if (lobby == null) lobby = FindAnyObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobby != null) lobby.OnSinglePlayerPressed();
    }
}
