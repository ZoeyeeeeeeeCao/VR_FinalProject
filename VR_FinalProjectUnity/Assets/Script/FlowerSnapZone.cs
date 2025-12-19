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
        Transform root = other.attachedRigidbody
            ? other.attachedRigidbody.transform.root
            : other.transform.root;

        if (!IsFlowerTag(root.tag)) return;
        if (_flowers.Contains(root.gameObject)) return;

        int slotIndex = GetFirstEmptySlotIndex();
        if (slotIndex < 0) return;

        SnapFlowerToSlot(root.gameObject, slots[slotIndex]);
        _flowers.Add(root.gameObject);
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
            if (slots[i].childCount > 0) continue;
            return i;
        }
        return -1;
    }

    void SnapFlowerToSlot(GameObject flower, Transform slot)
    {
        var grab = flower.GetComponentInChildren<XRGrabInteractable>();

        // Force release
        if (grab != null && grab.isSelected)
        {
            var mgr = FindObjectOfType<XRInteractionManager>();
            if (mgr != null)
                mgr.SelectExit(grab.firstInteractorSelecting, grab);
        }

        // Store original scale
        if (!_originalScale.ContainsKey(flower))
            _originalScale[flower] = flower.transform.localScale;

        // Disable physics
        var rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            if (makeKinematic) rb.isKinematic = true;
        }

        // Snap
        flower.transform.SetPositionAndRotation(slot.position, slot.rotation);
        flower.transform.localScale = _originalScale[flower] * snappedScaleMultiplier;

        // Parent to slot
        flower.transform.SetParent(slot, true);

        // Listen for grab to REMOVE it
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnFlowerGrabbed);
            grab.selectEntered.AddListener(OnFlowerGrabbed);
        }
    }

    void OnFlowerGrabbed(SelectEnterEventArgs args)
    {
        var grab = args.interactableObject as XRGrabInteractable;
        if (grab == null) return;

        GameObject flower = grab.transform.root.gameObject;

        // Restore scale
        if (_originalScale.TryGetValue(flower, out var original))
            flower.transform.localScale = original;

        // Detach from slot (THIS FREES THE SLOT)
        flower.transform.SetParent(null, true);

        // Restore physics
        var rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;

        // Remove from tracking list
        _flowers.Remove(flower);
    }

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
