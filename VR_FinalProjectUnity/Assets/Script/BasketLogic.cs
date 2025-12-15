using UnityEngine;
using System.Collections.Generic;

public class BasketLogic : MonoBehaviour
{
    [System.Serializable]
    public struct FlowerSlot
    {
        public FlowerType type;
        public Transform slot;
        public GameObject displayPrefab;
        public GameObject realFlowerPrefab; // full-size, grabbable prefab
    }

    [Header("Flower Slots")]
    public FlowerSlot[] slots;

    [Header("Basket Physics")]
    [Tooltip("The SOLID collider used for grabbing the basket (NOT the trigger collider).")]
    public Collider basketBodyCollider;

    private Dictionary<FlowerType, int> counts = new Dictionary<FlowerType, int>();
    private Dictionary<FlowerType, GameObject> displays = new Dictionary<FlowerType, GameObject>();

    // =========================================================
    // Called by BasketTrigger when a flower enters the basket
    // =========================================================
    public void CollectFlower(GameObject flower)
    {
        // ---------- Guard against double collection ----------
        FlowerCollectedFlag flag = flower.GetComponent<FlowerCollectedFlag>();
        if (flag == null || flag.collected)
            return;

        flag.collected = true;

        // ---------- Ignore collision between flower and basket body ----------
        Collider flowerCollider = flower.GetComponent<Collider>();
        if (flowerCollider != null && basketBodyCollider != null)
        {
            Physics.IgnoreCollision(flowerCollider, basketBodyCollider, true);
        }

        // ---------- Stop flower physics immediately ----------
        Rigidbody rb = flower.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // ---------- Get flower data ----------
        FlowerData data = flower.GetComponent<FlowerData>();
        if (data == null)
            return;

        FlowerType type = data.flowerType;

        // ---------- Increase count ----------
        if (!counts.ContainsKey(type))
            counts[type] = 0;

        counts[type]++;

        Debug.Log($"[Basket] Added {type}. Count = {counts[type]}");

        // ---------- Create display if first flower ----------
        if (!displays.ContainsKey(type))
        {
            CreateDisplay(type);
        }

        // ---------- Remove the real flower ----------
        Destroy(flower);
    }

    // =========================================================
    // Create visual (display-only) flower
    // =========================================================
    void CreateDisplay(FlowerType type)
    {
        foreach (var s in slots)
        {
            if (s.type != type)
                continue;

            GameObject display = Instantiate(
                s.displayPrefab,
                s.slot.position,
                s.slot.rotation,
                s.slot
            );

            displays[type] = display;

            Debug.Log($"[Basket] Display created for {type}");
            return;
        }

        Debug.LogWarning($"[Basket] No slot configured for {type}");
    }

    // =========================================================
    // Called when player takes a flower from the basket
    // =========================================================
    public GameObject TakeFlower(FlowerType type, Transform handTransform)
    {
        if (!counts.ContainsKey(type))
            return null;

        if (counts[type] <= 0)
            return null;

        counts[type]--;

        Debug.Log($"[Basket] Took {type}. Count = {counts[type]}");

        // ---------- Remove display if this was the last one ----------
        if (counts[type] == 0 && displays.ContainsKey(type))
        {
            GameObject display = displays[type];

            // Hide immediately (important for XR timing)
            display.SetActive(false);

            Debug.Log($"[Basket] Destroying display instance ID = {display.GetInstanceID()}");

            Destroy(display);
            displays.Remove(type);

            Debug.Log($"[Basket] Display removed for {type}");
        }

        // ---------- Spawn real flower prefab ----------
        foreach (var s in slots)
        {
            if (s.type != type)
                continue;

            if (s.realFlowerPrefab == null)
            {
                Debug.LogError($"[Basket] RealFlowerPrefab not assigned for {type}");
                return null;
            }

            GameObject realFlower = Instantiate(
                s.realFlowerPrefab,
                handTransform.position,
                handTransform.rotation
            );

            return realFlower;
        }

        return null;
    }

    // =========================================================
    // Optional helper
    // =========================================================
    public int GetCount(FlowerType type)
    {
        return counts.ContainsKey(type) ? counts[type] : 0;
    }
}
