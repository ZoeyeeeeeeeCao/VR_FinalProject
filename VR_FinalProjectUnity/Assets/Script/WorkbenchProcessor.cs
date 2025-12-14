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

    [Header("Debug")]
    public bool debugLog = true;

    // 当前轮模式
    FlowerMode mode = FlowerMode.None;

    // 当前在 Zone 内的同色花（root）
    readonly HashSet<Transform> flowersInZone = new HashSet<Transform>();

    // 追踪粉末是否还在台上（粉末在 -> 锁定不允许开始新轮）
    bool powderInside = false;

    // 搅拌计数
    int stirCount = 0;

    // 防止 shovel 在 zone 内抖动导致多次 Enter
    bool shovelInside = false;

    Coroutine popupRoutine;

    void Awake()
    {
        if (flowerNotEnoughCanvas) flowerNotEnoughCanvas.SetActive(false);
        if (colorMismatchCanvas) colorMismatchCanvas.SetActive(false);
    }

    // ---------- Trigger ----------
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

        // 铲子进入：要么弹“花不够”，要么计搅拌
        if (root.CompareTag(shovelTag))
        {
            if (shovelInside) return;
            shovelInside = true;

            if (powderInside)
            {
                // 粉末还在，不允许继续加工（你也可以选择弹提示，但你没要求）
                if (debugLog) Debug.Log("[Workbench] Shovel enter but powder still inside -> ignore");
                return;
            }

            if (mode == FlowerMode.None || flowersInZone.Count < requiredFlowers)
            {
                // 花不够或还没开始模式
                ShowPopup(flowerNotEnoughCanvas);
                if (debugLog) Debug.Log($"[Workbench] Not enough flowers: {flowersInZone.Count}/{requiredFlowers}");
                return;
            }

            // 花够了，计搅拌
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
        if (incoming == FlowerMode.None) return; // 不是我们关心的花

        if (powderInside)
        {
            // 粉末还在台上：本轮没结束，不接受新花（也可以弹 mismatch，但你需求是“粉末拿走后才能开始新轮”）
            if (debugLog) Debug.Log("[Workbench] Flower entered but powder still inside -> ignore");
            return;
        }

        // 如果工作台是空的（mode None + 当前没有花），第一个花决定模式
        if (mode == FlowerMode.None && flowersInZone.Count == 0)
        {
            mode = incoming;
            if (debugLog) Debug.Log("[Workbench] Mode set to: " + mode);
        }

        // 颜色不匹配：弹提示，不计入
        if (incoming != mode)
        {
            ShowPopup(colorMismatchCanvas);
            if (debugLog) Debug.Log($"[Workbench] Color mismatch! mode={mode}, incoming={incoming}");
            return;
        }

        // 同色：加入集合
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
            // 粉末拿走后，如果台上也没有花了，则完全重置
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

        // 花离开
        bool removed = flowersInZone.Remove(root);
        if (removed && debugLog) Debug.Log($"[Workbench] Flower left, remaining: {flowersInZone.Count}/{requiredFlowers}");

        // 你说：放了1朵又拿走 -> 回到初始
        // 所以：只要台上花变少，就把搅拌清零（避免“搅过的进度”被带走）
        stirCount = 0;

        TryResetIfEmpty();
    }

    // ---------- Core ----------
    void ProcessToPowder()
    {
        if (powderInside) return;
        if (mode == FlowerMode.None) return;
        if (flowersInZone.Count < requiredFlowers) return;

        // 选对应粉末
        GameObject powderPrefab = GetPowderPrefab(mode);
        if (powderPrefab == null)
        {
            Debug.LogError("[Workbench] Powder prefab not assigned for mode: " + mode);
            return;
        }

        // 销毁 4 朵（如果你可能放 >4，只销毁4朵）
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

        for (int i = 0; i < toDestroy.Count; i++)
            if (toDestroy[i] != null) Destroy(toDestroy[i].gameObject);

        flowersInZone.Clear();
        stirCount = 0;

        // 注意：生成出来的粉末要有 Tag=Powder + Collider + Rigidbody(kine) 这样才能被 zone 识别为 powderInside
        if (debugLog) Debug.Log($"✅ Processed {mode} -> powder spawned. Workbench locked until powder removed.");
    }

    void TryResetIfEmpty()
    {
        // 只有当：没有花 && 没有粉末，才算“工作台空” -> 允许新模式
        if (flowersInZone.Count == 0 && powderInside == false)
        {
            mode = FlowerMode.None;
            stirCount = 0;
            if (debugLog) Debug.Log("[Workbench] Reset to initial state (empty).");
        }
    }

    // ---------- Helpers ----------
    void ShowPopup(GameObject canvas)
    {
        if (canvas == null) return;

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupCo(canvas));
    }

    IEnumerator PopupCo(GameObject canvas)
    {
        // 先关掉两个，避免重叠
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
