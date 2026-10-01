using UnityEngine;
public class SafeAudioListener : MonoBehaviour
{
    public float listenerScanInterval = 2f;
    private static Transform follower;
    private Camera cachedMainCamera;
    private float scanTimer;
    private void Awake()
    {
        DisableForeignListeners();
        if (follower == null)
        {
            GameObject go = new GameObject("PW_Global_AudioListener");
            go.AddComponent<AudioListener>();
            Object.DontDestroyOnLoad(go);
            follower = go.transform;
        }
    }
    private void LateUpdate()
    {
        if (follower == null) return;
        if (cachedMainCamera == null || !cachedMainCamera.gameObject.activeInHierarchy)
        {
            cachedMainCamera = Camera.main;
        }
        if (cachedMainCamera != null)
        {
            follower.SetPositionAndRotation(cachedMainCamera.transform.position, cachedMainCamera.transform.rotation);
        }
        scanTimer += Time.deltaTime;
        if (scanTimer >= listenerScanInterval)
        {
            scanTimer = 0f;
            DisableForeignListeners();
        }
    }
    private void DisableForeignListeners()
    {
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
        foreach (var l in listeners)
        {
            if (l != null && l.gameObject.name != "PW_Global_AudioListener")
            {
                l.enabled = false;
            }
        }
    }
}
