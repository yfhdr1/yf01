using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
public class NetworkManager : MonoBehaviourPunCallbacks
{
    public string playerPrefabName = "WhiteclownPlayer";
    public Vector3 spawnPosition = new Vector3(474.12f, 24.93f, 166.33f);
    public GameObject loadingPanel;
    private const string RoomName = "Room1";
    private bool spawned;
    private bool autoJoin;
    private void Start()
    {
        Application.runInBackground = true;
        QualitySettings.pixelLightCount = 8;
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        if (PhotonNetwork.InRoom)
        {
            SpawnPlayer();
            return;
        }
        autoJoin = true;
        if (PhotonNetwork.IsConnectedAndReady)
        {
            JoinGameRoom();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    public override void OnConnectedToMaster()
    {
        if (!autoJoin || spawned)
        {
            return;
        }
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        if (!autoJoin || spawned)
        {
            return;
        }
        JoinGameRoom();
    }
    public override void OnJoinedRoom()
    {
        autoJoin = false;
        SpawnPlayer();
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Join room failed: " + message);
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create room failed: " + message);
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        autoJoin = false;
        Debug.LogWarning("Photon disconnected: " + cause);
    }
    private void JoinGameRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        PhotonNetwork.JoinOrCreateRoom(RoomName, roomOptions, TypedLobby.Default);
    }
    private void SpawnPlayer()
    {
        if (spawned)
        {
            return;
        }
        spawned = true;
        Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
        PhotonNetwork.Instantiate(playerPrefabName, spawnPosition + offset, Quaternion.identity);
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
}
