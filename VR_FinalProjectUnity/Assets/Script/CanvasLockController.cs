using UnityEngine;


public class CanvasLockController : MonoBehaviour
{
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    public Transform easelSlot;

    private bool isLocked = true;

    void Awake()
    {
        if (!grab)
            grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        LockCanvas();
    }

    public void UnlockCanvas()
    {
        isLocked = false;
        grab.enabled = true;
    }

    public void LockCanvas()
    {
        isLocked = true;
        grab.enabled = false;

        // Snap back to easel
        transform.position = easelSlot.position;
        transform.rotation = easelSlot.rotation;
        transform.SetParent(easelSlot);
    }
}
