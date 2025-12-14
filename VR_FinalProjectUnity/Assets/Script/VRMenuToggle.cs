using UnityEngine;
using UnityEngine.InputSystem;

public class VRMenuToggle : MonoBehaviour
{
    [Header("Menu Root (SetActive true/false)")]
    public GameObject menuRoot;          // 你的 VRMenuRoot
    public Transform head;               // XR Origin 的 Main Camera
    [Header("Placement")]
    public float distance = 0.6f;        // 面板离头显多远
    public float heightOffset = -0.1f;   // 稍微低一点更舒服
    public bool followHead = true;       // 菜单打开时是否跟随头显

    [Header("Toggle Button (X or Y)")]
    public InputActionProperty toggleAction; // 绑定到 X 或 Y

    bool _isOpen;

    void OnEnable()
    {
        if (toggleAction.action != null) toggleAction.action.Enable();
    }

    void OnDisable()
    {
        if (toggleAction.action != null) toggleAction.action.Disable();
    }

    void Update()
    {
        // 按一下切换（适配 Quest 的按钮，ReadValue<float> 也行，但 triggered 最省心）
        if (toggleAction.action != null && toggleAction.action.triggered)
        {
            SetOpen(!_isOpen);
        }

        if (_isOpen && followHead && head != null)
        {
            UpdatePose();
        }
    }

    public void SetOpen(bool open)
    {
        _isOpen = open;
        if (menuRoot != null) menuRoot.SetActive(open);

        if (open) UpdatePose(); // 打开瞬间摆到正确位置
    }

    void UpdatePose()
    {
        // 放在头显前方
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 pos = head.position + forward * distance;
        pos.y += heightOffset;
        menuRoot.transform.position = pos;

        // 面向头显（只绕 Y 轴）
        Vector3 lookDir = menuRoot.transform.position - head.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
            menuRoot.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
    }
}
