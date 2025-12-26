using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UIClickSoundManager : MonoBehaviour
{
    public AudioClip clickSfx;
    [Range(0f, 1f)] public float volume = 1f;

    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D UI sound
    }

    // 👇 THIS IS WHAT YOU CALL FROM BUTTON INSPECTOR
    public void PlayClick()
    {
        if (clickSfx != null)
            source.PlayOneShot(clickSfx, volume);
    }
}
