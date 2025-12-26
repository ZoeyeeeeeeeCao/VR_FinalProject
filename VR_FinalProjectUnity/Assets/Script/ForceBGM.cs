using UnityEngine;

public class ForceBGM : MonoBehaviour
{
    void Start()
    {
        var a = GetComponent<AudioSource>();
        if (a != null && a.clip != null)
        {
            a.Play();
            Debug.Log("BGM STARTED: " + a.clip.name);
        }
        else
        {
            Debug.Log("BGM MISSING AUDIOSOURCE OR CLIP");
        }
    }
}
