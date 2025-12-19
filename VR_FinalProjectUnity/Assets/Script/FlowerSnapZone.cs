using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FlowerSnapZone : MonoBehaviour
{
    [Header("4 Slots (fixed positions)")]
    public Transform[] slots = new Transform[4];

    [Header("Accepted Flower Tags (cannot change in your project)")]
    public string[] flowerTags = { "FlowerRed", "FlowerBlue", "FlowerYellow" };

    [Header("Snap Settings")]
    [Range(0.1f, 1f)]
    public float snappedScaleMultiplier = 0.4f;

    public bool parentToSlot = true;
    public bool makeKinematic = true;

    [Header("Debug")]
    public bool debugLog = false;

    public IReadOnlyList<GameObject> CurrentFlowers => _flowers;

    // 当前已占用slot的花（root GameObject）
    private readonly List<GameObject> _flowers = new List<GameObject>(4);

    // 记录每个花原始缩放
    private readonly Dictionary<GameObject, Vector3> _originalScale = new Dictionary<GameObject, Vector3>();

    // 记录花 -> slotIndex（用来释放slot）
    private readonly Dictionary<GameObject, int> _flowerToSlot = new Dictionary<GameObject, int>();

    // slot占用表（不要再靠 childCount 判断，因为 XR 可能把物体 parent 回来）
    private readonly bool[] _slotOccupied = new bool[4];

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
        GameObject flower = GetFlowerRootGameObject(other);
        if (flower == null) return;

        if (!IsFlowerTag(flower.tag)) return;
        if (_flowerToSlot.ContainsKey(flower)) return; // 已经在某个slot里了

        int slotIndex = GetFirstEmptySlotIndex_ByRecord();
        if (slotIndex < 0) return;

        SnapFlowerToSlot(flower, slotIndex);
    }

    // ⚠️ 这里不靠 OnTriggerExit 做释放，因为你是“grab拿走”的，不一定能稳定触发 exit
    // 释放slot统一在 grab/selectEntered 里做（最稳），以及外部 ResetSlotsAfterProcessing 做（更稳）。

    int GetFirstEmptySlotIndex_ByRecord()
    {
        int len = Mathf.Min(4, slots.Length);
        for (int i = 0; i < len; i++)
        {
            if (slots[i] == null) continue;
            if (_slotOccupied[i]) continue;
            return i;
        }
        return -1;
    }

    void SnapFlowerToSlot(GameObject flower, int slotIndex)
    {
        Transform slot = slots[slotIndex];
        if (slot == null) return;

        var grab = flower.GetComponentInChildren<XRGrabInteractable>();

        // ✅ 关键：防止松手自动回到slot（你之前遇到的“松开又回slot”就是这个）
        if (grab != null) grab.retainTransformParent = false;

        // Force release（如果它正在被抓着）
        if (grab != null && grab.isSelected)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null && grab.firstInteractorSelecting != null)
            {
                mgr.SelectExit(grab.firstInteractorSelecting, grab);
            }
        }

        // Store original scale
        if (!_originalScale.ContainsKey(flower))
            _originalScale[flower] = flower.transform.localScale;

        // Disable physics
        var rb = flower.GetComponent<Rigidbody>();
        if (rb == null) rb = flower.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (makeKinematic) rb.isKinematic = true;
        }

        // Snap position/rotation/scale
        flower.transform.SetPositionAndRotation(slot.position, slot.rotation);
        flower.transform.localScale = _originalScale[flower] * snappedScaleMultiplier;

        // Parent
        if (parentToSlot)
            flower.transform.SetParent(slot, true);

        // Record occupied
        _slotOccupied[slotIndex] = true;
        _flowerToSlot[flower] = slotIndex;
        if (!_flowers.Contains(flower)) _flowers.Add(flower);

        // Listen for grab -> release slot
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnFlowerGrabbed);
            grab.selectEntered.AddListener(OnFlowerGrabbed);
        }

        if (debugLog) Debug.Log($"[FlowerSnapZone] ✅ Snapped {flower.name} to slot {slotIndex}");
    }

    void OnFlowerGrabbed(SelectEnterEventArgs args)
    {
        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null) return;

        GameObject flower = grab.transform.root.gameObject;

        ReleaseFlower(flower, restorePhysics: true);
    }

    /// <summary>
    /// 释放某一朵花占用的slot（被抓走/被外部逻辑移走时调用）
    /// </summary>
    public void ReleaseFlower(GameObject flower, bool restorePhysics)
    {
        if (flower == null) return;

        // Restore scale
        if (_originalScale.TryGetValue(flower, out var original))
            flower.transform.localScale = original;

        // Detach from slot
        flower.transform.SetParent(null, true);

        // Restore physics
        if (restorePhysics)
        {
            var rb = flower.GetComponent<Rigidbody>();
            if (rb == null) rb = flower.GetComponentInChildren<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }

        // Free slot record
        if (_flowerToSlot.TryGetValue(flower, out int idx))
        {
            if (idx >= 0 && idx < _slotOccupied.Length)
                _slotOccupied[idx] = false;

            _flowerToSlot.Remove(flower);
        }

        _flowers.Remove(flower);

        if (debugLog) Debug.Log($"[FlowerSnapZone] 🧹 Released {flower.name}");
    }

    /// <summary>
    /// ✅ 你生成powder成功后必须调用这个：
    /// 释放全部slot记录，并把slot下面残留子物体detach（避免“slot被占用”）。
    /// destroyChildren=false：你已经Destroy花了就传false
    /// </summary>
    public void ResetSlotsAfterProcessing(bool destroyChildren = false)
    {
        // 释放记录
        for (int i = 0; i < _slotOccupied.Length; i++)
            _slotOccupied[i] = false;

        // 把 slot 下子物体全部 detach（有时候XR会把物体 parent 回来，childCount会骗你）
        int len = Mathf.Min(4, slots.Length);
        for (int i = 0; i < len; i++)
        {
            if (slots[i] == null) continue;

            // 先收集，避免边遍历边改 parent
            List<Transform> children = new List<Transform>();
            for (int c = 0; c < slots[i].childCount; c++)
                children.Add(slots[i].GetChild(c));

            foreach (var ch in children)
            {
                if (ch == null) continue;
                if (destroyChildren) Destroy(ch.gameObject);
                else ch.SetParent(null, true);
            }
        }

        _flowers.Clear();
        _flowerToSlot.Clear();

        if (debugLog) Debug.Log("[FlowerSnapZone] 🔄 ResetSlotsAfterProcessing done.");
    }

    bool IsFlowerTag(string tag)
    {
        foreach (var t in flowerTags)
            if (tag == t) return true;
        return false;
    }

    GameObject GetFlowerRootGameObject(Collider other)
    {
        if (other == null) return null;

        // 你花多数情况是 Rigidbody 在 root，上面这句最稳
        if (other.attachedRigidbody != null) return other.attachedRigidbody.gameObject;

        // 退化：用 root
        return other.transform.root.gameObject;
    }
}
