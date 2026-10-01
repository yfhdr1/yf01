using UnityEngine;
using UnityEditor;
using Photon.Pun;
public class DoorSetupTool
{
    [MenuItem("Tools/Setup All Doors")]
    static void SetupDoors()
    {
        int count = 0;
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer == -1)
        {
            Debug.LogError("Create a Layer named Interactable first");
            return;
        }
        MeshRenderer[] all = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        foreach (MeshRenderer r in all)
        {
            GameObject go = r.gameObject;
            string n = go.name.ToLower();
            if (!n.Contains("door") && !n.Contains("gate")) continue;
            if (n.Contains("lod1") || n.Contains("lod2") || n.Contains("_dm")) continue;
            if (go.transform.parent != null && go.transform.parent.name.EndsWith("_Hinge")) continue;
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            go.layer = layer;
            DoorController oldDc = go.GetComponent<DoorController>();
            if (oldDc != null) Object.DestroyImmediate(oldDc, true);
            PhotonView oldPv = go.GetComponent<PhotonView>();
            if (oldPv != null) Object.DestroyImmediate(oldPv, true);
            Vector3 s = go.transform.lossyScale;
            bool negative = s.x < 0f || s.y < 0f || s.z < 0f;
            BoxCollider box = go.GetComponent<BoxCollider>();
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (negative)
            {
                if (box != null) Object.DestroyImmediate(box, true);
                if (mc == null) mc = go.AddComponent<MeshCollider>();
                mc.convex = true;
            }
            else
            {
                if (mc != null) Object.DestroyImmediate(mc, true);
                if (box == null) box = go.AddComponent<BoxCollider>();
            }
            Bounds b = r.bounds;
            Vector3 edge = b.center + go.transform.right * (b.size.x * 0.5f);
            GameObject hinge = new GameObject(go.name + "_Hinge");
            hinge.transform.SetParent(go.transform.parent, false);
            hinge.transform.position = edge;
            hinge.transform.rotation = go.transform.rotation;
            hinge.layer = layer;
            go.transform.SetParent(hinge.transform, true);
            hinge.AddComponent<PhotonView>();
            hinge.AddComponent<DoorController>();
            EditorUtility.SetDirty(hinge);
            count++;
        }
        Debug.Log("Doors setup: " + count);
    }
}
