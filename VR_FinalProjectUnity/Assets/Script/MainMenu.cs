using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource bgm;

    public void Play()
    {
        if (bgm != null) bgm.Stop();
        SceneManager.LoadScene("Level1");
    }
}
