using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndMenuButtons : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource clickAudio;   // 拖 Button Click 音效进来
    public float delayTime = 3f;     // 等待时间（3 秒）

    private bool isProcessing = false;

    // Restart 按钮
    public void RestartGame()
    {
        if (isProcessing) return;
        StartCoroutine(RestartAfterDelay());
    }

    // Exit 按钮
    public void ExitGame()
    {
        if (isProcessing) return;
        StartCoroutine(ExitAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        isProcessing = true;

        if (clickAudio)
            clickAudio.Play();

        yield return new WaitForSeconds(delayTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    IEnumerator ExitAfterDelay()
    {
        isProcessing = true;

        if (clickAudio)
            clickAudio.Play();

        yield return new WaitForSeconds(delayTime);

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

