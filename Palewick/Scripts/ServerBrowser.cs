using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
public class ServerBrowser : MonoBehaviourPunCallbacks
{
    public GameObject serverPanel;
    public TMP_InputField serverNameInput;
    public Transform serverListContent;
    public GameObject serverItemPrefab;
    public GameObject loadingPanel;
    public TMP_Text statusText;
    private const float ReconnectDelay = 2f;
    private const float OfflineRetryDelay = 5f;
    private const float ConnectTimeout = 10f;
    private const string OfflineRoomName = "Offline";
    private readonly Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();
    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private bool waitingToOpen;
    private bool reconnectPending;
    private bool offlinePending;
    private bool startingOffline;
    private static bool NoInternet()
    {
        return Application.internetReachability == NetworkReachability.NotReachable;
    }
    private void Start()
    {
        Application.runInBackground = true;
        PhotonNetwork.KeepAliveInBackground = 60f;
        PhotonNetwork.SendRate = 20;
        PhotonNetwork.SerializationRate = 10;
        PhotonNetwork.AutomaticallySyncScene = true;
        if (serverItemPrefab != null) serverItemPrefab.SetActive(false);
        if (serverPanel != null) serverPanel.SetActive(false);
        if (PhotonNetwork.OfflineMode) PhotonNetwork.OfflineMode = false;
        if (!PhotonNetwork.IsConnected)
        {
            if (NoInternet())
            {
                SetStatus("Offline");
                ScheduleReconnect(OfflineRetryDelay);
                return;
            }
            SetStatus("Connecting...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }
    private void OnDestroy()
    {
        CancelInvoke();
    }
    public void OpenServerPanel()
    {
        if (startingOffline) return;
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby && !PhotonNetwork.OfflineMode)
        {
            ShowPanel();
            return;
        }
        if (NoInternet())
        {
            PlayOffline();
            return;
        }
        waitingToOpen = true;
        if (loadingPanel != null) loadingPanel.SetActive(true);
        SetStatus("Connecting...");
        CancelInvoke(nameof(OnConnectTimeout));
        Invoke(nameof(OnConnectTimeout), ConnectTimeout);
        if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
        else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && !PhotonNetwork.InLobby) PhotonNetwork.JoinLobby();
    }
    private void OnConnectTimeout()
    {
        if (waitingToOpen) PlayOffline();
    }
    public void PlayOffline()
    {
        if (startingOffline) return;
        waitingToOpen = false;
        CancelInvoke(nameof(OnConnectTimeout));
        CancelInvoke(nameof(Reconnect));
        reconnectPending = false;
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (serverPanel != null) serverPanel.SetActive(false);
        SetStatus("Offline");
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
        {
            offlinePending = true;
            PhotonNetwork.Disconnect();
            return;
        }
        StartOffline();
    }
    private void StartOffline()
    {
        offlinePending = false;
        startingOffline = true;
        if (PhotonNetwork.OfflineMode)
        {
            if (PhotonNetwork.InRoom) OnJoinedRoom();
            else PhotonNetwork.CreateRoom(OfflineRoomName, OfflineOptions());
            return;
        }
        PhotonNetwork.OfflineMode = true;
    }
    private static RoomOptions OfflineOptions()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 1;
        options.IsVisible = false;
        options.IsOpen = false;
        return options;
    }
    private void ShowPanel()
    {
        waitingToOpen = false;
        CancelInvoke(nameof(OnConnectTimeout));
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (serverPanel != null) serverPanel.SetActive(true);
        SetStatus("Ready");
        RefreshList();
    }
    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode)
        {
            if (startingOffline) PhotonNetwork.CreateRoom(OfflineRoomName, OfflineOptions());
            return;
        }
        SetStatus("Connecting...");
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        SetStatus("Ready");
        if (waitingToOpen) ShowPanel();
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        cachedRooms.Clear();
        RefreshList();
        if (offlinePending)
        {
            StartOffline();
            return;
        }
        if (startingOffline) return;
        if (waitingToOpen)
        {
            PlayOffline();
            return;
        }
        if (cause == DisconnectCause.DisconnectByClientLogic)
        {
            SetStatus("Disconnected");
            return;
        }
        SetStatus(NoInternet() ? "Offline" : "Reconnecting...");
        ScheduleReconnect(NoInternet() ? OfflineRetryDelay : ReconnectDelay);
    }
    private void ScheduleReconnect(float delay)
    {
        if (reconnectPending) return;
        reconnectPending = true;
        Invoke(nameof(Reconnect), delay);
    }
    private void Reconnect()
    {
        reconnectPending = false;
        if (startingOffline || PhotonNetwork.OfflineMode || PhotonNetwork.IsConnected) return;
        if (NoInternet())
        {
            SetStatus("Offline");
            ScheduleReconnect(OfflineRetryDelay);
            return;
        }
        SetStatus("Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible) cachedRooms.Remove(room.Name);
            else cachedRooms[room.Name] = room;
        }
        RefreshList();
    }
    private void RefreshList()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null) Destroy(spawnedItems[i]);
        }
        spawnedItems.Clear();
        if (serverItemPrefab == null || serverListContent == null) return;
        foreach (KeyValuePair<string, RoomInfo> pair in cachedRooms)
        {
            RoomInfo room = pair.Value;
            GameObject item = Instantiate(serverItemPrefab, serverListContent);
            item.SetActive(true);
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = room.Name + "   " + room.PlayerCount + "/" + room.MaxPlayers;
            string roomName = room.Name;
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => JoinServer(roomName));
            }
            spawnedItems.Add(item);
        }
    }
    public void CreateServer()
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.OfflineMode)
        {
            SetStatus("Not connected");
            return;
        }
        string roomName = "Server";
        if (serverNameInput != null && serverNameInput.text.Trim().Length > 0) roomName = serverNameInput.text.Trim();
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        options.IsVisible = true;
        options.IsOpen = true;
        options.CleanupCacheOnLeave = true;
        SetStatus("Creating...");
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }
    public void JoinServer(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady || PhotonNetwork.OfflineMode)
        {
            SetStatus("Not connected");
            return;
        }
        SetStatus("Joining...");
        PhotonNetwork.JoinRoom(roomName);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (PhotonNetwork.OfflineMode)
        {
            startingOffline = false;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            SetStatus("Create failed");
            return;
        }
        SetStatus(returnCode == ErrorCode.GameIdAlreadyExists ? "Name taken" : "Create failed");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        SetStatus(returnCode == ErrorCode.GameFull ? "Server full" : "Join failed");
    }
    public override void OnJoinedRoom()
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("Scene_A");
    }
    public void CloseServerPanel()
    {
        if (serverPanel != null) serverPanel.SetActive(false);
    }
    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}
