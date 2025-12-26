using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CanvasLockController : MonoBehaviour
{
    public XRGrabInteractable grab;

    [HideInInspector] public bool isLocked;

    void Awake()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();
    }

    public void LockTo(Transform slot)
    {
        isLocked = true;

        // Stop grabbing
        if (grab) grab.enabled = false;

        // Snap + parent
        transform.SetParent(slot, true);
        transform.position = slot.position;
        transform.rotation = slot.rotation;
    }

    public void Unlock()
    {
        isLocked = false;

        // Unparent so it can move freely
        transform.SetParent(null, true);

        if (grab) grab.enabled = true;
    }
}
