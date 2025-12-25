using UnityEngine;
using UnityEngine.InputSystem;
using PaintIn3D;

public class PaintWithGrip : MonoBehaviour
{
    public CwHitBetween hitBetween;
    public InputActionProperty gripAction;
    public LineRenderer line;

    void OnEnable() => gripAction.action?.Enable();
    void OnDisable() => gripAction.action?.Disable();

    void Update()
    {
        bool inPaintMode = PaintModeManager.PaintMode;
        bool gripHeld = gripAction.action != null && gripAction.action.ReadValue<float>() > 0.1f;

        // 不在上色模式：彻底关闭
        if (!inPaintMode)
        {
            if (hitBetween) hitBetween.enabled = false;
            if (line) line.enabled = false;
            return;
        }

        // 在上色模式：只有按住Grip才启用绘画&显示线
        if (hitBetween) hitBetween.enabled = gripHeld;
        if (hitBetween) hitBetween.Pressure = gripHeld ? 1f : 0f;

        if (line) line.enabled = gripHeld;
    }
}
