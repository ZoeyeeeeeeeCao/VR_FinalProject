using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(AudioSource))]
public class GrabSfx : MonoBehaviour
{
    public AudioClip grabSfx;
    public AudioClip releaseSfx;
    [Range(0f, 1f)] public float volume = 1f;
    public float minInterval = 0.05f;

    AudioSource source;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    float lastTime;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    }

    void OnEnable()
    {
        if (interactable == null) return;
        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        if (interactable == null) return;
        interactable.selectEntered.RemoveListener(OnGrabbed);
        interactable.selectExited.RemoveListener(OnReleased);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (Time.time - lastTime < minInterval) return;
        lastTime = Time.time;

        if (grabSfx != null) source.PlayOneShot(grabSfx, volume);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (releaseSfx != null) source.PlayOneShot(releaseSfx, volume);
    }
}
