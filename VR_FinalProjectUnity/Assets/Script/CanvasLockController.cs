using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CanvasLockController : MonoBehaviour
{
    public XRGrabInteractable grab;

    [HideInInspector] public bool isLocked;

    // ✅ NEW (SFX)
    public AudioClip grabSfx;
    [Range(0f, 1f)] public float grabVolume = 1f;
    AudioSource audioSource;

    void Awake()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();

        // ✅ NEW (SFX)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (grab) grab.selectEntered.AddListener(OnGrabbed); // ✅ NEW
    }

    void OnDestroy()
    {
        if (grab) grab.selectEntered.RemoveListener(OnGrabbed); // ✅ NEW
    }

    // ✅ NEW
    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isLocked) return; // don’t play when locked
        if (grabSfx != null) audioSource.PlayOneShot(grabSfx, grabVolume);
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
