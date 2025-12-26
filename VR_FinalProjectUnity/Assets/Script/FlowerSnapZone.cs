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

    [Tooltip("When a flower is grabbed/released, prevent it from being re-snapped for a short time.")]
    public float reSnapCooldown = 0.25f;

    [Header("State")]
    public bool allowSnap = true;   // ✅ 外部可以锁住它

    [Header("Debug")]
    public bool debugLog = false;

    public IReadOnlyList<GameObject> CurrentFlowers => _flowers;

    private readonly List<GameObject> _flowers = new List<GameObject>(4);
    private readonly Dictionary<GameObject, Vector3> _originalScale = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, int> _flowerToSlot = new Dictionary<GameObject, int>();
    private readonly bool[] _slotOccupied = new bool[4];

    // ✅ 花的短暂冷却：避免刚抓起/刚放下就被 OnTriggerEnter 又 snap
    private readonly Dictionary<GameObject, float> _cooldownUntil = new Dictionary<GameObject, float>();

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
        if (!allowSnap) return;

        GameObject flower = GetFlowerRootGameObject(other);
        if (flower == null) return;

        if (!IsFlowerTag(flower.tag)) return;

        // cooldown check
        if (_cooldownUntil.TryGetValue(flower, out float until) && Time.time < until)
        {
            if (debugLog) Debug.Log($"[FlowerSnapZone] ⏳ Cooldown skip: {flower.name}");
            return;
        }

        if (_flowerToSlot.ContainsKey(flower)) return; // already snapped

        int slotIndex = GetFirstEmptySlotIndex_ByRecord();
        if (slotIndex < 0)
        {
            if (debugLog) Debug.Log("[FlowerSnapZone] ❌ No empty slot");
            return;
        }

        SnapFlowerToSlot(flower, slotIndex);
    }

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

        // ✅ 防止松手自动回slot（关键）
        if (grab != null) grab.retainTransformParent = false;

        // Force release if currently selected
        if (grab != null && grab.isSelected)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null && grab.firstInteractorSelecting != null)
                mgr.SelectExit(grab.firstInteractorSelecting, grab);
        }

        if (!_originalScale.ContainsKey(flower))
            _originalScale[flower] = flower.transform.localScale;

        // physics
        var rb = flower.GetComponent<Rigidbody>();
        if (rb == null) rb = flower.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (makeKinematic) rb.isKinematic = true;
        }

        // snap
        flower.transform.SetPositionAndRotation(slot.position, slot.rotation);
        flower.transform.localScale = _originalScale[flower] * snappedScaleMultiplier;

        if (parentToSlot)
            flower.transform.SetParent(slot, true);

        // record
        _slotOccupied[slotIndex] = true;
        _flowerToSlot[flower] = slotIndex;
        if (!_flowers.Contains(flower)) _flowers.Add(flower);

        // listen to grab/release
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnFlowerSelectEntered);
            grab.selectEntered.AddListener(OnFlowerSelectEntered);

            grab.selectExited.RemoveListener(OnFlowerSelectExited);
            grab.selectExited.AddListener(OnFlowerSelectExited);
        }

        if (debugLog) Debug.Log($"[FlowerSnapZone] ✅ Snapped {flower.name} -> slot {slotIndex}");
    }

    // ✅ 一抓起：立刻释放 slot + detach，避免 slot 被占用
    void OnFlowerSelectEntered(SelectEnterEventArgs args)
    {
        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null) return;

        GameObject flower = grab.transform.root.gameObject;

        // 立刻释放slot
        ReleaseFlower(flower, restorePhysics: true);

        // cooldown 防止刚抓起还在 zone 内被重新 snap
        _cooldownUntil[flower] = Time.time + reSnapCooldown;

        if (debugLog) Debug.Log($"[FlowerSnapZone] ✋ Grabbed -> released slot: {flower.name}");
    }

    // ✅ 松手：也给 cooldown，避免松手时碰撞又进 zone 被吸回
    void OnFlowerSelectExited(SelectExitEventArgs args)
    {
        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null) return;

        GameObject flower = grab.transform.root.gameObject;
        _cooldownUntil[flower] = Time.time + reSnapCooldown;

        if (debugLog) Debug.Log($"[FlowerSnapZone] 🫳 Released -> cooldown: {flower.name}");
    }

    public void ReleaseFlower(GameObject flower, bool restorePhysics)
    {
        if (flower == null) return;

        // Restore scale
        if (_originalScale.TryGetValue(flower, out var original))
            flower.transform.localScale = original;

        // Detach from slot
        if (flower.transform.parent != null)
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

    // ✅ 外部调用：生成 powder 后，把 slot 状态清干净（不然下一轮 snap 不进去）
    public void ResetSlotsAfterProcessing(bool destroyChildren = false)
    {
        for (int i = 0; i < _slotOccupied.Length; i++)
            _slotOccupied[i] = false;

        int len = Mathf.Min(4, slots.Length);
        for (int i = 0; i < len; i++)
        {
            if (slots[i] == null) continue;

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
        _cooldownUntil.Clear();

        if (debugLog) Debug.Log("[FlowerSnapZone] 🔄 ResetSlotsAfterProcessing done.");
    }

    public void SetLocked(bool locked)
    {
        allowSnap = !locked;
        if (debugLog) Debug.Log($"[FlowerSnapZone] 🔒 allowSnap = {allowSnap}");
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
        if (other.attachedRigidbody != null) return other.attachedRigidbody.gameObject;
        return other.transform.root.gameObject;
    }
}
