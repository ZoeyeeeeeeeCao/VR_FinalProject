using UnityEngine;
using UnityEngine.SceneManagement;

public class PaintProgressManager : MonoBehaviour
{
    public static PaintProgressManager Instance;

    [Header("Config")]
    public int targetPaintCount = 3;

    [Header("UI")]
    public GameObject proceedPanel;   // 你的UI面板（默认隐藏）

    private int currentPaintCount = 0;
    private bool panelShown = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 如果你希望切关也保留这个manager，就打开这行
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (proceedPanel) proceedPanel.SetActive(false);
    }

    // ✅ 在你“生成颜料prefab成功”的时候调用这个
    public void RegisterPaintSpawned()
    {
        currentPaintCount++;

        if (!panelShown && currentPaintCount >= targetPaintCount)
        {
            panelShown = true;
            if (proceedPanel) proceedPanel.SetActive(true);

            // 可选：弹窗时暂停游戏
            // Time.timeScale = 0f;
        }
    }

    // ✅ 按钮 OnClick 绑定这个
    public void ProceedToNextLevel()
    {
        // 可选：恢复时间
        // Time.timeScale = 1f;

        int curIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = curIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            Debug.LogWarning("No next level in Build Settings!");
    }
}

