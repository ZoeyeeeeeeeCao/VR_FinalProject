using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WorkbenchMixZone : MonoBehaviour
{
    [Header("SFX")]
    public AudioClip paintAppearSfx;     // drag click2 here
    public AudioSource sfxSource;        // optional (or auto-find)

    [System.Serializable]
    public class OutputMap
    {
        public ColorKind kind;
        public GameObject paintPrefab;
    }

    [Header("Snap Points")]
    public Transform powderSnap;
    public Transform kettleSnapIn;
    public Transform kettleSnapOut;

    [Header("Outputs")]
    public OutputMap[] outputs;

    [Header("Kettle Detect")]
    public string kettleTag = "Kettle"; // 给水壶 root 设置 Tag=Kettle（更稳）

    [Header("Animator")]
    public string pourTrigger = "Pour"; // Animator Trigger 参数名（你 Animator 里要有同名 Trigger）

    [Header("Options")]
    public bool lockGrabbableWhileSnapped = true;

    [Header("Debug")]
    public bool debugLog = true;

    GameObject currentPowder;
    PowderType currentPowderType;

    Transform currentKettleRoot;
    Animator currentKettleAnimator;

    bool busy;

    // kettle original rigidbody state
    Rigidbody kettleRb;
    bool kettleRbHad;
    bool kettleRbWasKinematic;

    // kettle grab release bookkeeping (so Snap overrides Grab)
    XRGrabInteractable kettleGrab;
    IXRSelectInteractor kettlePrevInteractor;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (debugLog) Debug.Log($"[WorkbenchMixZone] OnTriggerEnter by: {other.name}");

        if (busy)
        {
            if (debugLog) Debug.Log("[WorkbenchMixZone] ❌ Busy, ignore trigger");
            return;
        }

        // 1) 粉末检测
        var pType = other.GetComponentInParent<PowderType>();
        if (pType != null)
        {
            if (debugLog) Debug.Log($"[WorkbenchMixZone] ✅ Powder detected: {pType.name}");
            TryAcceptPowder(pType.gameObject, pType);
            return;
        }

        // 2) 水壶检测
        if (currentPowder == null)
        {
            if (debugLog) Debug.Log("[WorkbenchMixZone] ℹ️ No powder yet, kettle ignored");
            return;
        }

        var kettleRoot = other.transform.root;

        if (debugLog) Debug.Log($"[WorkbenchMixZone] Kettle candidate root: {kettleRoot.name} tag={kettleRoot.tag}");

        if (!string.IsNullOrEmpty(kettleTag) && !kettleRoot.CompareTag(kettleTag))
        {
            if (debugLog) Debug.Log($"[WorkbenchMixZone] ❌ Root tag mismatch: {kettleRoot.tag}");
            return;
        }

        if (debugLog) Debug.Log("[WorkbenchMixZone] ✅ Kettle accepted, start process");
        TryStartKettleProcess(kettleRoot);
    }

    // ---------------- Powder ----------------
    void TryAcceptPowder(GameObject powderObj, PowderType pType)
    {
        if (currentPowder != null) return;

        if (powderSnap == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ powderSnap not assigned!");
            return;
        }

        currentPowder = powderObj;
        currentPowderType = pType;

        // 强制取消抓取（如果正在抓）
        var grab = powderObj.GetComponentInChildren<XRGrabInteractable>();
        if (grab != null && grab.isSelected && grab.firstInteractorSelecting != null)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null) mgr.SelectExit(grab.firstInteractorSelecting, grab);
        }

        // 关物理避免抖动/弹走
        var rb = powderObj.GetComponent<Rigidbody>();
        if (rb == null) rb = powderObj.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Snap 到目标
        powderObj.transform.SetPositionAndRotation(powderSnap.position, powderSnap.rotation);

        // 禁用 grab（可选）
        if (lockGrabbableWhileSnapped && grab != null)
            grab.enabled = false;

        if (debugLog)
        {
            Debug.Log($"[WorkbenchMixZone] ✅ Powder snapped to {powderSnap.position}");
            Debug.Log($"[WorkbenchMixZone] PowderSnap local: {powderSnap.localPosition}  world: {powderSnap.position}");
        }
    }

    // ---------------- Kettle process ----------------
    void TryStartKettleProcess(Transform kettleRoot)
    {
        if (debugLog) Debug.Log("[WorkbenchMixZone] ▶ TryStartKettleProcess");

        if (busy)
        {
            if (debugLog) Debug.Log("[WorkbenchMixZone] ❌ Already busy");
            return;
        }

        if (kettleSnapIn == null || kettleSnapOut == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ kettleSnapIn / kettleSnapOut not assigned!");
            return;
        }

        busy = true;

        currentKettleRoot = kettleRoot;
        currentKettleAnimator = kettleRoot.GetComponentInChildren<Animator>();

        if (currentKettleAnimator == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ Kettle has NO Animator");
            busy = false;
            return;
        }

        if (debugLog) Debug.Log("[WorkbenchMixZone] 🎬 Animator found");

        // 记录并设为 Kinematic，避免吸附抖动
        kettleRb = kettleRoot.GetComponent<Rigidbody>();
        kettleRbHad = kettleRb != null;

        if (kettleRbHad)
        {
            kettleRbWasKinematic = kettleRb.isKinematic;
            kettleRb.velocity = Vector3.zero;
            kettleRb.angularVelocity = Vector3.zero;
            kettleRb.isKinematic = true;
            if (debugLog) Debug.Log("[WorkbenchMixZone] Rigidbody set to kinematic");
        }

        // ✅ 关键：先强制退出抓取，否则 Grab 会每帧把水壶拉回手上，导致“看起来没吸附”
        ForceReleaseGrab(kettleRoot);

        // 再锁 grab（避免立刻又被抓回去）
        if (lockGrabbableWhileSnapped)
            SetGrabEnabled(kettleRoot.gameObject, false);

        if (debugLog) Debug.Log("[WorkbenchMixZone] 📌 Snapping kettle IN");
        SnapTo(kettleRoot, kettleSnapIn);

        if (debugLog) Debug.Log("[WorkbenchMixZone] 🔥 Trigger animation");
        currentKettleAnimator.ResetTrigger(pourTrigger);
        currentKettleAnimator.SetTrigger(pourTrigger);

        // ⚠️ 后续结束逻辑由动画最后一帧 Animation Event 调用 OnPourFinished()
    }

    // 🔔 在 WaterAnima 动画最后一帧加 Animation Event 调这个
    public void OnPourFinished()
    {
        if (debugLog) Debug.Log("[WorkbenchMixZone] ⏹ OnPourFinished called");

        if (!busy)
        {
            Debug.LogWarning("[WorkbenchMixZone] ❌ Not busy, ignore event");
            return;
        }

        if (currentKettleRoot == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ currentKettleRoot NULL");
            busy = false;
            return;
        }

        if (debugLog) Debug.Log("[WorkbenchMixZone] 📌 Snapping kettle OUT");
        SnapTo(currentKettleRoot, kettleSnapOut);

        if (debugLog) Debug.Log("[WorkbenchMixZone] 🎨 Spawning paint");
        SpawnPaintAndConsumePowder();

        // 解锁 grab
        if (lockGrabbableWhileSnapped)
            SetGrabEnabled(currentKettleRoot.gameObject, true);

        // 恢复水壶原本 kinematic
        if (kettleRbHad && kettleRb != null)
            kettleRb.isKinematic = kettleRbWasKinematic;

        // 清理
        currentKettleRoot = null;
        currentKettleAnimator = null;

        kettleRb = null;
        kettleRbHad = false;

        kettleGrab = null;
        kettlePrevInteractor = null;

        busy = false;
        if (debugLog) Debug.Log("[WorkbenchMixZone] ✅ Process finished");
    }

    // ---------------- Spawn output ----------------
    void SpawnPaintAndConsumePowder()
    {
        if (currentPowder == null || currentPowderType == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ No powder to consume");
            return;
        }

        var prefab = FindPaintPrefab(currentPowderType.kind);

        if (prefab == null)
        {
            Debug.LogError($"[WorkbenchMixZone] ❌ No paint prefab mapped for kind: {currentPowderType.kind}");
            return;
        }

        Instantiate(prefab, powderSnap.position, powderSnap.rotation);

        // 你自己的进度系统
        if (PaintProgressManager.Instance != null)
            PaintProgressManager.Instance.RegisterPaintSpawned();

        // SFX
        PlaySfx(paintAppearSfx);

        Destroy(currentPowder);

        currentPowder = null;
        currentPowderType = null;
    }

    GameObject FindPaintPrefab(ColorKind kind)
    {
        foreach (var m in outputs)
            if (m.kind == kind) return m.paintPrefab;
        return null;
    }

    // ---------------- Helpers ----------------
    void ForceReleaseGrab(Transform root)
    {
        kettleGrab = root.GetComponentInChildren<XRGrabInteractable>();
        if (kettleGrab == null)
        {
            if (debugLog) Debug.LogWarning("[WorkbenchMixZone] ⚠️ Kettle has no XRGrabInteractable (can't force release).");
            return;
        }

        // 防止“松手自动回父物体”的奇怪行为（可选，但保险）
        kettleGrab.retainTransformParent = false;

        kettlePrevInteractor = kettleGrab.firstInteractorSelecting;

        if (kettleGrab.isSelected && kettlePrevInteractor != null)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null)
            {
                if (debugLog) Debug.Log("[WorkbenchMixZone] ✋ Force release kettle grab");
                mgr.SelectExit(kettlePrevInteractor, kettleGrab);
            }
        }
    }

    static void SnapTo(Transform obj, Transform target)
    {
        if (obj == null || target == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ SnapTo missing obj or target");
            return;
        }

        // 如果 obj 上还有刚体，清一下速度（更稳）
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        obj.position = target.position;
        obj.rotation = target.rotation;
    }

    static void SetGrabEnabled(GameObject go, bool enabled)
    {
        var grab = go.GetComponentInChildren<XRGrabInteractable>();
        if (grab != null) grab.enabled = enabled;
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D（如果你想3D就改成 1）
        }

        sfxSource.PlayOneShot(clip);
    }
}
