using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
public class PlayerSetup : MonoBehaviourPun, IPunObservable
{
    public Camera mainPlayerCamera;
    public AudioListener audioListener;
    public GameObject flashlight;
    public CharController_Motor motor;
    public CameraViewSwitcher viewSwitcher;
    public StaminaSystem stamina;
    public PlayerInteraction interaction;
    public GameObject characterMesh;
    private Quaternion flashTargetRotation;
    private bool flashTargetActive;
    private Transform spotLightTransform;
    private Quaternion spotOriginalLocalRotation;
    private bool fpAimActive;
    private TextMesh nameTag;
    private void Awake()
    {
        RemoveAllLines();
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null && !pv.ObservedComponents.Contains(this))
        {
            pv.ObservedComponents.Add(this);
        }
    }
    private void Start()
    {
        RemoveAllLines();
        bool isLocal = !PhotonNetwork.InRoom || photonView == null || photonView.IsMine;
        if (flashlight != null)
        {
            Light spot = flashlight.GetComponentInChildren<Light>(true);
            if (spot != null)
            {
                spotLightTransform = spot.transform;
                spot.type = LightType.Spot;
                spot.range = 30f;
                spot.spotAngle = 80f;
                spot.color = Color.white;
                spot.intensity = 4f;
                spot.shadows = LightShadows.None;
                spot.cookie = null;
                spot.flare = null;
                spot.renderMode = LightRenderMode.ForcePixel;
                spotOriginalLocalRotation = spotLightTransform.localRotation;
            }
            flashTargetRotation = spotLightTransform != null ? spotLightTransform.rotation : Quaternion.identity;
            flashTargetActive = flashlight.activeSelf;
        }
        CreateNameTag();
        if (isLocal)
        {
            EnableLocalPlayer();
        }
        else
        {
            DisableRemotePlayer();
        }
    }
    private void RemoveAllLines()
    {
        LineRenderer[] lines = GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null)
            {
                lines[i].enabled = false;
            }
        }
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            if (trails[i] != null)
            {
                trails[i].enabled = false;
            }
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (spotLightTransform == null)
            {
                stream.SendNext(Quaternion.identity);
                stream.SendNext(flashlight != null && flashlight.activeSelf);
                return;
            }
            stream.SendNext(spotLightTransform.rotation);
            stream.SendNext(flashlight.activeSelf);
        }
        else
        {
            flashTargetRotation = (Quaternion)stream.ReceiveNext();
            flashTargetActive = (bool)stream.ReceiveNext();
        }
    }
    private void LateUpdate()
    {
        if (nameTag != null && Camera.main != null)
        {
            nameTag.transform.rotation = Camera.main.transform.rotation;
        }
        if (spotLightTransform != null)
        {
            bool isLocal = !PhotonNetwork.InRoom || photonView == null || photonView.IsMine;
            if (isLocal)
            {
                if (viewSwitcher != null && !viewSwitcher.IsThirdPerson && motor != null)
                {
                    float pitch = Mathf.Clamp(motor.CameraPitch, -30f, 60f);
                    spotLightTransform.rotation = Quaternion.Euler(pitch, transform.eulerAngles.y + motor.CameraYawOffset, 0f);
                    fpAimActive = true;
                }
                else if (fpAimActive)
                {
                    spotLightTransform.localRotation = spotOriginalLocalRotation;
                    fpAimActive = false;
                }
            }
            else
            {
                spotLightTransform.rotation = Quaternion.Slerp(spotLightTransform.rotation, flashTargetRotation, Time.deltaTime * 15f);
            }
        }
        if (!PhotonNetwork.InRoom || photonView == null || photonView.IsMine || flashlight == null)
        {
            return;
        }
        if (flashlight.activeSelf != flashTargetActive)
        {
            flashlight.SetActive(flashTargetActive);
        }
    }
    private void CreateNameTag()
    {
        GameObject tagObject = new GameObject("NameTag");
        tagObject.transform.SetParent(transform, false);
        tagObject.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        nameTag = tagObject.AddComponent<TextMesh>();
        string playerName = GetPlayerName();
        Font rtlFont = HasRtl(playerName) ? Resources.Load<Font>("Fonts/UniMahanBilal") : null;
        nameTag.text = rtlFont != null ? PwRtl.Visual(playerName) : playerName;
        nameTag.font = rtlFont != null ? rtlFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameTag.fontSize = 48;
        nameTag.characterSize = 0.06f;
        nameTag.anchor = TextAnchor.MiddleCenter;
        nameTag.alignment = TextAlignment.Center;
        nameTag.color = Color.white;
        tagObject.GetComponent<MeshRenderer>().sharedMaterial = nameTag.font.material;
    }
    private static bool HasRtl(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return false;
        }
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'))
            {
                return true;
            }
        }
        return false;
    }
    private string GetPlayerName()
    {
        if (PhotonNetwork.InRoom && photonView != null && photonView.Owner != null && !string.IsNullOrEmpty(photonView.Owner.NickName))
        {
            return photonView.Owner.NickName;
        }
        string savedName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedName))
        {
            return savedName;
        }
        return "Player";
    }
    private void EnableLocalPlayer()
    {
        if (mainPlayerCamera != null)
        {
            mainPlayerCamera.gameObject.SetActive(true);
        }
        if (audioListener != null)
        {
            audioListener.enabled = true;
        }
        if (motor != null)
        {
            motor.enabled = true;
        }
        if (viewSwitcher != null)
        {
            viewSwitcher.enabled = true;
        }
        if (stamina != null)
        {
            stamina.enabled = true;
        }
        if (interaction != null)
        {
            interaction.enabled = true;
        }
        if (characterMesh != null)
        {
            characterMesh.SetActive(false);
        }
        if (motor != null)
        {
            Joystick joystick = FindAnyObjectByType<Joystick>(FindObjectsInactive.Include);
            if (joystick != null)
            {
                motor.moveJoystick = joystick;
            }
        }
        FlashlightController fc = FindAnyObjectByType<FlashlightController>(FindObjectsInactive.Include);
        if (fc != null && flashlight != null)
        {
            fc.SetFlashlight(flashlight);
        }
        AutoRunButton autoRunBtn = FindAnyObjectByType<AutoRunButton>(FindObjectsInactive.Include);
        if (autoRunBtn != null && motor != null)
        {
            autoRunBtn.playerMotor = motor;
        }
        Button viewBtn = FindButton("ViewSwitchBtn");
        if (viewBtn != null && viewSwitcher != null)
        {
            viewBtn.onClick.RemoveAllListeners();
            viewBtn.onClick.AddListener(viewSwitcher.ToggleView);
        }
        Button interactBtn = FindButton("InteractButton");
        if (interactBtn != null && interaction != null)
        {
            interactBtn.onClick.RemoveAllListeners();
            interactBtn.onClick.AddListener(interaction.OnInteractButtonPressed);
            interaction.interactButtonUI = interactBtn.gameObject;
            interactBtn.gameObject.SetActive(false);
        }
    }
    private void DisableRemotePlayer()
    {
        if (mainPlayerCamera != null)
        {
            mainPlayerCamera.gameObject.SetActive(false);
        }
        if (audioListener != null)
        {
            audioListener.enabled = false;
        }
        if (motor != null)
        {
            motor.enabled = false;
        }
        if (viewSwitcher != null)
        {
            viewSwitcher.enabled = false;
        }
        if (stamina != null)
        {
            stamina.enabled = false;
        }
        if (interaction != null)
        {
            interaction.enabled = false;
        }
        if (characterMesh != null)
        {
            characterMesh.SetActive(true);
        }
    }
    private static Button FindButton(string objectName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name == objectName)
            {
                return buttons[i];
            }
        }
        return null;
    }
}
