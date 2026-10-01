using Photon.Pun;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Palewick.EditorTools
{
    public static class PwPointsBuilder
    {
        private const string ArtFolder = "Assets/UI_Lobby";
        private const string MaterialPath = "Assets/UI_Lobby/PointPickupMat.mat";
        private static readonly Color TextColor = new Color(0.93f, 0.86f, 0.8f, 1f);
        private static readonly Color HintColor = new Color(0.75f, 0.68f, 0.66f, 0.85f);
        private static Font font;
        [MenuItem("Palewick/Build Points HUD")]
        public static void BuildHud()
        {
            Canvas canvas = FindGameCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Points", "Open Scene_A first (Canvas with PubgPauseMenu not found).", "OK");
                return;
            }
            font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Creepster.ttf");
            Undo.SetCurrentGroupName("Build Points HUD");
            int group = Undo.GetCurrentGroup();
            Transform root = canvas.transform;
            Transform old = root.Find("PointsBadge");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            RectTransform badge = Node("PointsBadge", root);
            Place(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(330f, 84f));
            Img(badge.gameObject, Spr("lobby_nameplate.png"), new Color(1f, 1f, 1f, 0.9f), false);
            PwPointsHud hud = badge.gameObject.AddComponent<PwPointsHud>();
            RectTransform icon = Node("Icon", badge);
            Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(58f, 0f), new Vector2(50f, 50f));
            Img(icon.gameObject, Spr("lobby_ember.png"), new Color(1f, 0.76f, 0.3f, 1f), false);
            hud.icon = icon;
            RectTransform value = Node("PointsValue", badge);
            Place(value, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(120f, 64f));
            hud.valueText = Label(value.gameObject, "0", 48, TextColor, TextAnchor.MiddleLeft);
            RectTransform label = Node("PointsLabel", badge);
            Place(label, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(170f, 54f));
            Label(label.gameObject, "Points", 32, HintColor, TextAnchor.MiddleRight);
            RectTransform dot = Node("SyncDot", badge);
            Place(dot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(26f, -16f), new Vector2(18f, 18f));
            hud.syncDot = Img(dot.gameObject, Spr("lobby_ember.png"), Color.white, false);
            RectTransform gain = Node("PointsGain", badge);
            Place(gain, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(180f, 64f));
            hud.gainText = Label(gain.gameObject, "+1", 44, new Color(1f, 0.85f, 0.35f, 1f), TextAnchor.MiddleCenter);
            gain.gameObject.SetActive(false);
            EditorUtility.SetDirty(hud);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = badge.gameObject;
            EditorUtility.DisplayDialog("Points", "Points badge added to the HUD. Press Ctrl+S to save the scene.", "OK");
        }
        [MenuItem("Palewick/Create Point Pickup")]
        public static void CreatePickup()
        {
            Vector3 pos = Vector3.zero;
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null) pos = view.pivot;
            GameObject go = new GameObject("PointPickup");
            Undo.RegisterCreatedObjectUndo(go, "Point Pickup");
            go.transform.position = pos;
            SphereCollider trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.1f;
            trigger.center = new Vector3(0f, 0.9f, 0f);
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localRotation = Quaternion.Euler(35f, 45f, 15f);
            visual.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = PickupMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            GameObject lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Light glow = lightGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.72f, 0.28f, 1f);
            glow.intensity = 2.2f;
            glow.range = 6f;
            glow.shadows = LightShadows.None;
            glow.renderMode = LightRenderMode.ForcePixel;
            PhotonView pv = go.AddComponent<PhotonView>();
            pv.Synchronization = ViewSynchronization.Off;
            pv.OwnershipTransfer = OwnershipOption.Fixed;
            PointPickup pickup = go.AddComponent<PointPickup>();
            pickup.visual = visual.transform;
            pickup.glow = glow;
            pickup.value = 1;
            pickup.autoPickup = true;
            EditorUtility.SetDirty(go);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        private static Material PickupMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null) return mat;
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            mat = new Material(shader);
            mat.name = "PointPickupMat";
            mat.color = new Color(0.9f, 0.25f, 0.1f, 1f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.15f, 1f) * 2.2f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.6f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.75f);
            if (AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateAsset(mat, MaterialPath);
            return mat;
        }
        private static Canvas FindGameCanvas()
        {
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].isRootCanvas && all[i].transform.Find("PubgPauseMenu") != null) return all[i];
            }
            return null;
        }
        private static Sprite Spr(string file)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + file);
        }
        private static RectTransform Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            Undo.RegisterCreatedObjectUndo(go, "Points");
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }
        private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
        private static Image Img(GameObject go, Sprite sprite, Color color, bool raycast)
        {
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }
        private static Text Label(GameObject go, string text, int size, Color color, TextAnchor align)
        {
            Text t = go.AddComponent<Text>();
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            Shadow sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(3f, -3f);
            return t;
        }
    }
}
