using UnityEngine;

[System.Serializable]
public struct FlowerSlot
{
    public FlowerType type;
    public Transform slot;
    public GameObject displayPrefab;
    public GameObject realFlowerPrefab; // full-size, grabbable
}
