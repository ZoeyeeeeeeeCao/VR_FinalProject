using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using PaintIn3D;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRPaintIn3D : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    public P3dPaintSphere paintSphere; // Paint in 3D 的画笔组件
    public float brushRadius = 0.02f;

    void Update()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // 把画笔移动到射线命中点
            paintSphere.transform.position = hit.point;

            // 触发绘画（你可以换成 Trigger 按键）
            paintSphere.HandleHitPoint(
                true,
                hit.collider,
                hit.point,
                hit.normal
            );
        }
    }
}
