using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WorkbenchMixZone : MonoBehaviour
{
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
    public string kettleTag = "Kettle";

    [Header("Animator")]
    public string pourTrigger = "Pour";

    [Header("Options")]
    public bool lockGrabbableWhileSnapped = true;

    // ✅ AUDIO (ADDED ONLY)
    [Header("SFX")]
    public AudioClip waterToPowderSfx;   // W1
    public AudioClip powderToColorSfx;   // click2
    AudioSource sfxSource;

    GameObject currentPowder;
    PowderType currentPowderType;

    Transform currentKettleRoot;
    Animator currentKettleAnimator;

    bool busy;

    Rigidbody kettleRb;
    bool kettleRbHad;
    bool kettleRbWasKinematic;

    void Awake() // ✅ ADDED ONLY
    {
        sfxSource = GetComponent<AudioSource>();
    }

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
        Debug.Log($"[WorkbenchMixZone] OnTriggerEnter by: {other.name}");

        if (busy)
        {
            Debug.Log("[WorkbenchMixZone] ❌ Busy, ignore trigger");
            return;
        }

        // 1️⃣ 粉末检测
        var pType = other.GetComponentInParent<PowderType>();
        if (pType != null)
        {
            Debug.Log($"[WorkbenchMixZone] ✅ Powder detected: {pType.name}");
            TryAcceptPowder(pType.gameObject, pType);
            return;
        }

        // 2️⃣ 水壶检测
        if (currentPowder == null)
        {
            Debug.Log("[WorkbenchMixZone] ℹ️ No powder yet, kettle ignored");
            return;
        }

        var kettleRoot = other.transform.root;
        Debug.Log($"[WorkbenchMixZone] Kettle candidate root: {kettleRoot.name}");

        if (!string.IsNullOrEmpty(kettleTag) && !kettleRoot.CompareTag(kettleTag))
        {
            Debug.Log($"[WorkbenchMixZone] ❌ Root tag mismatch: {kettleRoot.tag}");
            return;
        }

        Debug.Log("[WorkbenchMixZone] ✅ Kettle accepted, start process");
        TryStartKettleProcess(kettleRoot);
    }

    void TryAcceptPowder(GameObject powderObj, PowderType pType)
    {
        if (currentPowder != null) return;

        currentPowder = powderObj;
        currentPowderType = pType;

        // 1) 强制取消抓取（很关键）
        var grab = powderObj.GetComponentInChildren<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null) mgr.SelectExit(grab.firstInteractorSelecting, grab);
        }

        // 2) 关物理避免抖动/弹走
        var rb = powderObj.GetComponent<Rigidbody>();
        if (rb == null) rb = powderObj.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 3) Snap 到目标
        powderObj.transform.SetPositionAndRotation(powderSnap.position, powderSnap.rotation);

        // 4) 禁用 grab（可选）
        if (lockGrabbableWhileSnapped && grab != null)
            grab.enabled = false;

        Debug.Log($"[WorkbenchMixZone] ✅ Powder snapped to {powderSnap.position}");
        Debug.Log($"PowderSnap local: {powderSnap.localPosition}  world: {powderSnap.position}");
    }

    void TryStartKettleProcess(Transform kettleRoot)
    {
        Debug.Log("[WorkbenchMixZone] ▶ TryStartKettleProcess");

        if (busy)
        {
            Debug.Log("[WorkbenchMixZone] ❌ Already busy");
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

        Debug.Log("[WorkbenchMixZone] 🎬 Animator found");

        kettleRb = kettleRoot.GetComponent<Rigidbody>();
        kettleRbHad = kettleRb != null;

        if (kettleRbHad)
        {
            kettleRbWasKinematic = kettleRb.isKinematic;
            kettleRb.velocity = Vector3.zero;
            kettleRb.angularVelocity = Vector3.zero;
            kettleRb.isKinematic = true;
            Debug.Log("[WorkbenchMixZone] Rigidbody set to kinematic");
        }

        Debug.Log("[WorkbenchMixZone] 📌 Snapping kettle IN");
        SnapTo(kettleRoot, kettleSnapIn, false);

        if (lockGrabbableWhileSnapped)
            SetGrabEnabled(kettleRoot.gameObject, false);

        // ✅ PLAY W1 EXACTLY WHEN POUR STARTS (ADDED ONLY)
        if (sfxSource != null && waterToPowderSfx != null)
            sfxSource.PlayOneShot(waterToPowderSfx);

        Debug.Log("[WorkbenchMixZone] 🔥 Trigger animation");
        currentKettleAnimator.ResetTrigger(pourTrigger);
        currentKettleAnimator.SetTrigger(pourTrigger);
    }

    // 🔔 Animation Event 调这个
    public void OnPourFinished()
    {
        Debug.Log("[WorkbenchMixZone] ⏹ OnPourFinished called");

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

        Debug.Log("[WorkbenchMixZone] 📌 Snapping kettle OUT");
        SnapTo(currentKettleRoot, kettleSnapOut, false);

        Debug.Log("[WorkbenchMixZone] 🎨 Spawning paint");
        SpawnPaintAndConsumePowder();

        if (lockGrabbableWhileSnapped)
            SetGrabEnabled(currentKettleRoot.gameObject, true);

        if (kettleRbHad && kettleRb != null)
            kettleRb.isKinematic = kettleRbWasKinematic;

        currentKettleRoot = null;
        currentKettleAnimator = null;
        kettleRb = null;
        kettleRbHad = false;

        busy = false;
        Debug.Log("[WorkbenchMixZone] ✅ Process finished");
    }

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
            Debug.LogError($"[WorkbenchMixZone] ❌ No prefab for {currentPowderType.kind}");
            return;
        }

        // ✅ PLAY CLICK2 EXACTLY WHEN POWDER BECOMES PAINT (ADDED ONLY)
        if (sfxSource != null && powderToColorSfx != null)
            sfxSource.PlayOneShot(powderToColorSfx);

        Instantiate(prefab, powderSnap.position, powderSnap.rotation);
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

    static void SnapTo(Transform obj, Transform target, bool setKinematic)
    {
        if (obj == null || target == null)
        {
            Debug.LogError("[WorkbenchMixZone] ❌ SnapTo missing obj or target");
            return;
        }

        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null && setKinematic)
            rb.isKinematic = true;

        obj.position = target.position;
        obj.rotation = target.rotation;
    }

    static void SetGrabEnabled(GameObject go, bool enabled)
    {
        var grab = go.GetComponentInChildren<XRGrabInteractable>();
        if (grab != null) grab.enabled = enabled;
    }
    
}
