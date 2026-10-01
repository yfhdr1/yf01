using UnityEngine;
using UnityEngine.UI;
public class PwPlayMode : MonoBehaviour
{
    public GameObject panel;
    public Button onlineButton;
    public Button offlineButton;
    public Button closeButton;
    public Text hintText;
    public LobbyManager lobby;
    private void Awake()
    {
        if (lobby == null) lobby = GetComponent<LobbyManager>();
        if (panel != null) panel.SetActive(false);
    }
    public void Open()
    {
        if (panel == null)
        {
            PlayOnline();
            return;
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (onlineButton != null) onlineButton.interactable = Application.internetReachability != NetworkReachability.NotReachable;
        if (hintText != null) hintText.gameObject.SetActive(onlineButton != null && !onlineButton.interactable);
    }
    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }
    public void PlayOnline()
    {
        Close();
        if (lobby != null) lobby.OnStartPressed();
    }
    public void PlayOffline()
    {
        Close();
        if (lobby != null) lobby.OnSinglePlayerPressed();
    }
}
