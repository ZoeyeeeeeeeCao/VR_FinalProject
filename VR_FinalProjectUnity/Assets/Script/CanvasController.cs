using UnityEngine;

public class CanvasController : MonoBehaviour
{
    public GameObject canvas; // 拖 Canvas 进来

    // 显示 Canvas（如果你需要）
    public void ShowCanvas()
    {
        canvas.SetActive(true);
    }

    // 关闭 Canvas（按钮点这个）
    public void CloseCanvas()
    {
        canvas.SetActive(false);
    }
}
