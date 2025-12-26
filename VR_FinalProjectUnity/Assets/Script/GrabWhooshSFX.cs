using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class GrabWhooshSFX : MonoBehaviour
{
    public AudioClip whoosh;
    private AudioSource source;

    void Awake()
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);

        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f; // 3D (VR)
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (whoosh != null) source.PlayOneShot(whoosh);
    }
}
