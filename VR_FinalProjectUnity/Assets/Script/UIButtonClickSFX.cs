using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class UIButtonClickSFX : MonoBehaviour
{
    public AudioClip clickSfx;

    AudioSource source;
    Button button;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        button = GetComponent<Button>();

        button.onClick.AddListener(PlayClick);
    }

    void PlayClick()
    {
        if (clickSfx != null)
            source.PlayOneShot(clickSfx);
    }
}
