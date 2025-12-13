using UnityEngine;

public class PaintModeManager : MonoBehaviour
{
    public static bool PaintMode;          // 全局开关：外部用这个判断
    public GameObject confirmEnterUI;      // 进入确认面板
    public GameObject confirmExitUI;       // 离开确认面板
    public GameObject colorUI;             // 你换颜色的那个面板（只在PaintMode显示）

    int insideCount; // 防止多个Collider触发抖动（可选但推荐）

    void Start()
    {
        SetPaintMode(false);
        if (confirmEnterUI) confirmEnterUI.SetActive(false);
        if (confirmExitUI) confirmExitUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("ENTER PaintZone by: " + other.name + " layer=" + LayerMask.LayerToName(other.gameObject.layer) + " tag=" + other.tag);
        if (!other.CompareTag("Player")) return; // 给XR Origin加Tag=Player
        insideCount++;
        if (!PaintMode && confirmEnterUI) confirmEnterUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        insideCount = Mathf.Max(0, insideCount - 1);

        if (insideCount == 0 && PaintMode && confirmExitUI)
            confirmExitUI.SetActive(true);
    }

    public void ConfirmEnterPaint()
    {
        if (confirmEnterUI) confirmEnterUI.SetActive(false);
        SetPaintMode(true);
    }

    public void CancelEnterPaint()
    {
        if (confirmEnterUI) confirmEnterUI.SetActive(false);
    }

    public void ConfirmExitPaint()
    {
        if (confirmExitUI) confirmExitUI.SetActive(false);
        SetPaintMode(false);
    }

    public void CancelExitPaint()
    {
        if (confirmExitUI) confirmExitUI.SetActive(false);
    }

    void SetPaintMode(bool on)
    {
        PaintMode = on;
        if (colorUI) colorUI.SetActive(on);
    }
}
