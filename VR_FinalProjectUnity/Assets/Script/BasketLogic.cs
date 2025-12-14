using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

public class BasketLogic : MonoBehaviour
{
    [System.Serializable]
    public struct FlowerSlot
    {
        public FlowerType type;
        public Transform slot;
        public GameObject displayPrefab;
        public GameObject realFlowerPrefab; // full-size, grabbable
    }

    public FlowerSlot[] slots;

    private Dictionary<FlowerType, int> counts = new();
    private Dictionary<FlowerType, GameObject> displays = new();

    // Called by basket trigger when a flower enters
    public void CollectFlower(GameObject flower)
    {
        // Guard against multiple trigger hits
        FlowerCollectedFlag flag = flower.GetComponent<FlowerCollectedFlag>();
        if (flag == null) return;
        if (flag.collected) return;
        flag.collected = true;

        // Disable all colliders immediately to prevent re-triggers this frame
        foreach (var col in flower.GetComponentsInChildren<Collider>())
            col.enabled = false;

        FlowerData data = flower.GetComponent<FlowerData>();
        if (data == null) return;

        FlowerType type = data.flowerType;

        if (!counts.ContainsKey(type))
            counts[type] = 0;

        counts[type]++;

        Debug.Log($"[Basket] Added {type}. Count = {counts[type]}");

        // Create display only when count becomes > 0 for the first time
        if (!displays.ContainsKey(type))
        {
            CreateDisplay(type);
        }

        Destroy(flower);
    }

    // Creates the visual (display-only) flower for a type
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

            Debug.Log($"[Basket] Display created for {type}");
            return;
        }
    }

    // Called when the player takes a flower from the basket
    // Spawns a full-size, grabbable flower and returns it
    public GameObject TakeFlower(FlowerType type, Transform handTransform)
    {
        if (!counts.ContainsKey(type)) return null;
        if (counts[type] <= 0) return null;

        counts[type]--;

        Debug.Log($"[Basket] Took {type}. Count = {counts[type]}");

        // Remove display only when count reaches zero
        if (counts[type] == 0 && displays.ContainsKey(type))
        {
            Destroy(displays[type]);
            displays.Remove(type);

            Debug.Log($"[Basket] Display removed for {type}");
        }

        // Spawn the real (full-size) flower
        foreach (var s in slots)
        {
            if (s.type != type) continue;

            GameObject realFlower = Instantiate(
                s.realFlowerPrefab,
                handTransform.position,
                handTransform.rotation
            );

            return realFlower;
        }

        return null;
    }

    // Optional helper
    public int GetCount(FlowerType type)
    {
        return counts.ContainsKey(type) ? counts[type] : 0;
    }
}
