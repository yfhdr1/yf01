using UnityEditor;
using Photon.Pun;
[InitializeOnLoad]
public static class PhotonCrashPreventer
{
    static PhotonCrashPreventer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
    }
}
