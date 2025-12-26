using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BasketLogic : MonoBehaviour
{
    [System.Serializable]
    public struct FlowerSlot
    {
        public FlowerType type;
        public Transform slot;
        public GameObject displayPrefab;
        public GameObject realFlowerPrefab;
    }

    [Header("Flower Slots")]
    public FlowerSlot[] slots;

    private Dictionary<FlowerType, int> counts = new Dictionary<FlowerType, int>();
    private Dictionary<FlowerType, GameObject> displays = new Dictionary<FlowerType, GameObject>();

    private XRGrabInteractable basketGrab;
    private bool isSnappedToTable = false;

    // Expose read-only state
    public bool IsLockedToTable => isSnappedToTable;

    void Awake()
    {
        basketGrab = GetComponent<XRGrabInteractable>();
    }

    // =========================================================
    // Collect flower (ONLY when basket is NOT locked)
    // =========================================================
    public void CollectFlower(GameObject flower)
    {
        if (isSnappedToTable)
            return;

        FlowerCollectedFlag flag = flower.GetComponent<FlowerCollectedFlag>();
        if (flag == null || flag.collected)
            return;

        flag.collected = true;

        FlowerData data = flower.GetComponent<FlowerData>();
        if (data == null)
            return;

        FlowerType type = data.flowerType;

        if (!counts.ContainsKey(type))
            counts[type] = 0;

        counts[type]++;

        Debug.Log($"[Basket] Added {type}. Count = {counts[type]}");

        if (!displays.ContainsKey(type))
            CreateDisplay(type);

        Destroy(flower);
    }

    void CreateDisplay(FlowerType type)
    {
        foreach (var s in slots)
        {
            if (s.type != type) continue;

            GameObject display = Instantiate(
                s.displayPrefab,
                s.slot.position,
                s.slot.rotation,
                s.slot
            );

            displays[type] = display;
            return;
        }
    }

    // =========================================================
    // Take flower OUT of basket
    // =========================================================
    public GameObject TakeFlower(FlowerType type, Transform handTransform)
    {
        if (!counts.ContainsKey(type) || counts[type] <= 0)
            return null;

        counts[type]--;

        Debug.Log($"[Basket] Took {type}. Count = {counts[type]}");

        if (counts[type] == 0 && displays.ContainsKey(type))
        {
            Destroy(displays[type]);
            displays.Remove(type);
        }

        // Unlock basket when empty
        if (isSnappedToTable && GetTotalFlowerCount() == 0)
            UnlockBasket();

        foreach (var s in slots)
        {
            if (s.type != type) continue;

            return Instantiate(
                s.realFlowerPrefab,
                handTransform.position,
                handTransform.rotation
            );
        }

        return null;
    }

    // =========================================================
    // Snap & lock basket
    // =========================================================
    public void SnapToTable(Transform snapPoint)
    {
        if (isSnappedToTable)
            return;

        isSnappedToTable = true;

        // Force release if held
        if (basketGrab.isSelected && basketGrab.interactionManager != null)
        {
            basketGrab.interactionManager.SelectExit(
                basketGrab.firstInteractorSelecting,
                basketGrab
            );
        }

        transform.SetPositionAndRotation(
            snapPoint.position,
            snapPoint.rotation
        );

        basketGrab.enabled = false;

        Debug.Log("[Basket] Snapped and locked");
    }

    void UnlockBasket()
    {
        isSnappedToTable = false;
        basketGrab.enabled = true;

        Debug.Log("[Basket] Unlocked (empty)");
    }

    int GetTotalFlowerCount()
    {
        int total = 0;
        foreach (var kvp in counts)
            total += kvp.Value;
        return total;
    }
}
