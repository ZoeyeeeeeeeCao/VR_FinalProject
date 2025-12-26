using UnityEngine;

public class CanvasController : MonoBehaviour
{
    [SerializeField] private GameObject canvasRoot;

    void Start()
    {
        if (canvasRoot == null)
        {
            Debug.LogError("canvasRoot 没有绑定！请把 Canvas 拖进 Inspector");
            return;
        }

        canvasRoot.SetActive(true);  // ✅ 进游戏自动开启
    }

    public void CloseCanvas()
    {
        canvasRoot.SetActive(false); // ✅ 按钮关闭
    }
}
