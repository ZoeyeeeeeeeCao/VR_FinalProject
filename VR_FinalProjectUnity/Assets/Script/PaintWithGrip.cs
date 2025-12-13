using UnityEngine;
using UnityEngine.InputSystem;
using PaintIn3D;

public class PaintWithGrip : MonoBehaviour
{
    public CwHitBetween hitBetween;
    public InputActionProperty gripAction;
    public LineRenderer line; // 可选：只在画时显示线

    void OnEnable() => gripAction.action?.Enable();
    void OnDisable() => gripAction.action?.Disable();

    void Update()
    {
        bool painting = gripAction.action != null && gripAction.action.ReadValue<float>() > 0.1f;

        // ✅ 关键：不按就彻底停掉 hit 提交
        if (hitBetween != null) hitBetween.enabled = painting;

        // 线只在画时显示
        if (line != null) line.enabled = painting;
    }
}
