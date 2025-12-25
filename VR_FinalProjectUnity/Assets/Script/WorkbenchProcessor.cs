using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkbenchColorMixer : MonoBehaviour
{
    public enum FlowerMode { None, Red, Yellow, Blue }

    [Header("Powder Prefabs")]
    public GameObject redPowderPrefab;
    public GameObject yellowPowderPrefab;
    public GameObject bluePowderPrefab;

    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("Requirements")]
    public int requiredFlowers = 4;
    public int requiredShovelEnters = 3;

    [Header("UI Popups (auto hide)")]
    public GameObject flowerNotEnoughCanvas;
    public GameObject colorMismatchCanvas;
    public float popupDuration = 2f;

    [Header("Tags")]
    public string shovelTag = "Shovel";
    public string powderTag = "Powder";
    public string redFlowerTag = "FlowerRed";
    public string yellowFlowerTag = "FlowerYellow";
    public string blueFlowerTag = "FlowerBlue";

    [Header("Snap Zone (IMPORTANT)")]
    public FlowerSnapZone flowerSnapZone; // ✅ 拖你的 FlowerSnapZone 物体进来

    [Header("Debug")]
    public bool debugLog = true;

    FlowerMode mode = FlowerMode.None;
    readonly HashSet<Transform> flowersInZone = new HashSet<Transform>();

    bool powderInside = false;
    int stirCount = 0;
    bool shovelInside = false;

    Coroutine popupRoutine;

    void Awake()
    {
        if (flowerNotEnoughCanvas) flowerNotEnoughCanvas.SetActive(false);
        if (colorMismatchCanvas) colorMismatchCanvas.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        Transform root = GetRoot(other);
        if (root == null) return;

        // 粉末进入：锁定
        if (root.CompareTag(powderTag))
        {
            powderInside = true;
            if (debugLog) Debug.Log("[Workbench] Powder inside -> locked");
            return;
        }

        // 铲子进入
        if (root.CompareTag(shovelTag))
        {
            if (shovelInside) return;
            shovelInside = true;

            if (powderInside)
            {
                if (debugLog) Debug.Log("[Workbench] Shovel enter but powder still inside -> ignore");
                return;
            }

            if (mode == FlowerMode.None || flowersInZone.Count < requiredFlowers)
            {
                ShowPopup(flowerNotEnoughCanvas);
                if (debugLog) Debug.Log($"[Workbench] Not enough flowers: {flowersInZone.Count}/{requiredFlowers}");
                return;
            }

            stirCount++;
            if (debugLog) Debug.Log($"[Workbench] Stir {stirCount}/{requiredShovelEnters}");

            if (stirCount >= requiredShovelEnters)
            {
                ProcessToPowder();
            }
            return;
        }

        // 花进入：决定模式/检查颜色
        FlowerMode incoming = GetFlowerModeByTag(root.tag);
        if (incoming == FlowerMode.None) return;

        if (powderInside)
        {
            if (debugLog) Debug.Log("[Workbench] Flower entered but powder still inside -> ignore");
            return;
        }

        if (mode == FlowerMode.None && flowersInZone.Count == 0)
        {
            mode = incoming;
            if (debugLog) Debug.Log("[Workbench] Mode set to: " + mode);
        }

        if (incoming != mode)
        {
            ShowPopup(colorMismatchCanvas);
            if (debugLog) Debug.Log($"[Workbench] Color mismatch! mode={mode}, incoming={incoming}");
            return;
        }

        flowersInZone.Add(root);
        if (debugLog) Debug.Log($"[Workbench] Flowers in zone: {flowersInZone.Count}/{requiredFlowers} mode={mode}");
    }

    void OnTriggerExit(Collider other)
    {
        Transform root = GetRoot(other);
        if (root == null) return;

        if (root.CompareTag(powderTag))
        {
            powderInside = false;
            if (debugLog) Debug.Log("[Workbench] Powder removed -> can start new round when empty");
            TryResetIfEmpty();
            return;
        }

        if (root.CompareTag(shovelTag))
        {
            shovelInside = false;
            return;
        }

        FlowerMode outgoing = GetFlowerModeByTag(root.tag);
        if (outgoing == FlowerMode.None) return;

        bool removed = flowersInZone.Remove(root);
        if (removed && debugLog) Debug.Log($"[Workbench] Flower left, remaining: {flowersInZone.Count}/{requiredFlowers}");

        stirCount = 0;
        TryResetIfEmpty();
    }

    void ProcessToPowder()
    {
        if (powderInside) return;
        if (mode == FlowerMode.None) return;
        if (flowersInZone.Count < requiredFlowers) return;

        GameObject powderPrefab = GetPowderPrefab(mode);
        if (powderPrefab == null)
        {
            Debug.LogError("[Workbench] Powder prefab not assigned for mode: " + mode);
            return;
        }

        // 收集要销毁的4朵花
        List<Transform> toDestroy = new List<Transform>(requiredFlowers);
        foreach (var f in flowersInZone)
        {
            if (f != null) toDestroy.Add(f);
            if (toDestroy.Count >= requiredFlowers) break;
        }
        if (toDestroy.Count < requiredFlowers) return;

        Vector3 pos = spawnPoint ? spawnPoint.position : toDestroy[0].position;
        Quaternion rot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        Instantiate(powderPrefab, pos, rot);

        // ✅ 先通知 FlowerSnapZone 重置slot（避免 Destroy 后 slot 还占用）
        if (flowerSnapZone != null)
            flowerSnapZone.ResetSlotsAfterProcessing(false);

        // Destroy 4 flowers
        for (int i = 0; i < toDestroy.Count; i++)
            if (toDestroy[i] != null) Destroy(toDestroy[i].gameObject);

        flowersInZone.Clear();
        stirCount = 0;

        if (debugLog) Debug.Log($"✅ Processed {mode} -> powder spawned. Workbench locked until powder removed.");
    }

    void TryResetIfEmpty()
    {
        if (flowersInZone.Count == 0 && powderInside == false)
        {
            mode = FlowerMode.None;
            stirCount = 0;
            if (debugLog) Debug.Log("[Workbench] Reset to initial state (empty).");
        }
    }

    void ShowPopup(GameObject canvas)
    {
        if (canvas == null) return;

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupCo(canvas));
    }

    IEnumerator PopupCo(GameObject canvas)
    {
        if (flowerNotEnoughCanvas) flowerNotEnoughCanvas.SetActive(false);
        if (colorMismatchCanvas) colorMismatchCanvas.SetActive(false);

        canvas.SetActive(true);
        yield return new WaitForSeconds(popupDuration);
        if (canvas) canvas.SetActive(false);
        popupRoutine = null;
    }

    FlowerMode GetFlowerModeByTag(string tag)
    {
        if (tag == redFlowerTag) return FlowerMode.Red;
        if (tag == yellowFlowerTag) return FlowerMode.Yellow;
        if (tag == blueFlowerTag) return FlowerMode.Blue;
        return FlowerMode.None;
    }

    GameObject GetPowderPrefab(FlowerMode m)
    {
        switch (m)
        {
            case FlowerMode.Red: return redPowderPrefab;
            case FlowerMode.Yellow: return yellowPowderPrefab;
            case FlowerMode.Blue: return bluePowderPrefab;
        }
        return null;
    }

    Transform GetRoot(Collider col)
    {
        if (col.attachedRigidbody != null) return col.attachedRigidbody.transform;
        return col.transform.root;
    }
}
