using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(AudioSource))]
public class CanvasLockController : MonoBehaviour
{
    public XRGrabInteractable grab;

    [Header("SFX")]
    public AudioClip grabSfx;
    [Range(0f, 1f)] public float volume = 1f;
    public float minInterval = 0.05f;

    [HideInInspector] public bool isLocked;

    AudioSource source;
    float lastTime;

    void Awake()
    {
        if (!grab) grab = GetComponent<XRGrabInteractable>();
        source = GetComponent<AudioSource>();

        if (grab != null)
            grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isLocked) return;
        if (Time.time - lastTime < minInterval) return;
        lastTime = Time.time;

        if (grabSfx != null && source != null)
            source.PlayOneShot(grabSfx, volume);
    }

    public void LockTo(Transform slot)
    {
        isLocked = true;

        if (grab) grab.enabled = false;

        transform.SetParent(slot, true);
        transform.position = slot.position;
        transform.rotation = slot.rotation;
    }

    public void Unlock()
    {
        isLocked = false;

        transform.SetParent(null, true);

        if (grab) grab.enabled = true;
    }
}
