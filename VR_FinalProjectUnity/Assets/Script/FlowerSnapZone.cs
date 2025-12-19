using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FlowerSnapZone : MonoBehaviour
{
    [Header("4 Slots (fixed positions)")]
    public Transform[] slots = new Transform[4];

    [Header("Accepted Flower Tags")]
    public string[] flowerTags = { "FlowerRed", "FlowerBlue", "FlowerYellow" };

    [Header("Snap Settings")]
    [Range(0.1f, 1f)]
    public float snappedScaleMultiplier = 0.4f;

    public bool parentToSlot = true;
    public bool makeKinematic = true;
    public bool disableGrabWhileSnapped = true;

    // 当前已放入的花（顺序 = slot 顺序）
    public IReadOnlyList<GameObject> CurrentFlowers => _flowers;
    private readonly List<GameObject> _flowers = new List<GameObject>(4);

    private readonly Dictionary<GameObject, Vector3> _originalScale
        = new Dictionary<GameObject, Vector3>();

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
        // 找 root（避免子 collider）
        Transform root = other.attachedRigidbody
            ? other.attachedRigidbody.transform.root
            : other.transform.root;

        // ✅ 判断是否是合法花 Tag
        if (!IsFlowerTag(root.tag)) return;

        if (_flowers.Contains(root.gameObject)) return;

        int slotIndex = GetFirstEmptySlotIndex();
        if (slotIndex < 0) return;

        SnapFlowerToSlot(root.gameObject, slots[slotIndex]);
        _flowers.Add(root.gameObject);

        Debug.Log($"[FlowerSnapZone] 🌸 {root.name} snapped to slot {slotIndex}");
    }

    bool IsFlowerTag(string tag)
    {
        foreach (var t in flowerTags)
            if (tag == t) return true;
        return false;
    }

    int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (parentToSlot && slots[i].childCount > 0) continue;
            return i;
        }
        return -1;
    }

    void SnapFlowerToSlot(GameObject flower, Transform slot)
    {
        // 1️⃣ 强制放手
        var grab = flower.GetComponentInChildren<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null)
                mgr.SelectExit(grab.firstInteractorSelecting, grab);
        }

        // 2️⃣ 记录原始 scale
        if (!_originalScale.ContainsKey(flower))
            _originalScale[flower] = flower.transform.localScale;

        // 3️⃣ 关物理
        var rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (makeKinematic) rb.isKinematic = true;
        }

        // 4️⃣ 吸附
        flower.transform.SetPositionAndRotation(slot.position, slot.rotation);

        // 5️⃣ 缩放
        flower.transform.localScale = _originalScale[flower] * snappedScaleMultiplier;

        // 6️⃣ parent（防乱飞）
        if (parentToSlot)
            flower.transform.SetParent(slot, true);

        // 7️⃣ 禁用抓取
        if (disableGrabWhileSnapped && grab != null)
            grab.enabled = false;
    }

    // 给你后面系统用
    public void ClearAllFlowers(bool destroy)
    {
        foreach (var f in _flowers)
        {
            if (f == null) continue;
            if (destroy) Destroy(f);
        }
        _flowers.Clear();
        _originalScale.Clear();
    }
}
