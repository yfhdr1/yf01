using UnityEngine;
public class DisableRenderer : MonoBehaviour
{
    private void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.enabled = false;
        }
    }
}
