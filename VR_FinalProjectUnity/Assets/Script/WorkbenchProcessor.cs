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
    public FlowerSnapZone flowerSnapZone;

    [Header("Debug")]
    public bool debugLog = true;

    // 🔊 SFX
    [Header("SFX")]
    public AudioClip shovelHitSfx;   // O1
    public AudioClip powderSpawnSfx; // W2
    public AudioClip errorSfx;        // ERROR

    AudioSource audioSource;
    bool errorPlaying;

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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D error sound
    }

    void OnTriggerEnter(Collider other)
    {
        Transform root = GetRoot(other);
        if (root == null) return;

        if (root.CompareTag(powderTag))
        {
            powderInside = true;
            if (debugLog) Debug.Log("[Workbench] Powder inside -> locked");
            return;
        }

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

            if (shovelHitSfx != null)
                audioSource.PlayOneShot(shovelHitSfx);

            if (debugLog) Debug.Log($"[Workbench] Stir {stirCount}/{requiredShovelEnters}");

            if (stirCount >= requiredShovelEnters)
                ProcessToPowder();

            return;
        }

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

        flowersInZone.Remove(root);
        stirCount = 0;
        TryResetIfEmpty();
    }

    void ProcessToPowder()
    {
        if (powderInside || mode == FlowerMode.None || flowersInZone.Count < requiredFlowers)
            return;

        GameObject powderPrefab = GetPowderPrefab(mode);
        if (powderPrefab == null) return;

        List<Transform> toDestroy = new List<Transform>();
        foreach (var f in flowersInZone)
        {
            toDestroy.Add(f);
            if (toDestroy.Count >= requiredFlowers) break;
        }

        Vector3 pos = spawnPoint ? spawnPoint.position : toDestroy[0].position;
        Instantiate(powderPrefab, pos, Quaternion.identity);

        if (powderSpawnSfx != null)
            audioSource.PlayOneShot(powderSpawnSfx);

        if (flowerSnapZone != null)
            flowerSnapZone.ResetSlotsAfterProcessing(false);

        foreach (var f in toDestroy)
            if (f != null) Destroy(f.gameObject);

        flowersInZone.Clear();
        stirCount = 0;
    }

    void TryResetIfEmpty()
    {
        if (flowersInZone.Count == 0 && !powderInside)
        {
            mode = FlowerMode.None;
            stirCount = 0;
        }
    }

    void ShowPopup(GameObject canvas)
    {
        if (canvas == null) return;

        if (!errorPlaying && errorSfx != null)
        {
            audioSource.PlayOneShot(errorSfx);
            errorPlaying = true;
        }

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(PopupCo(canvas));
    }

    IEnumerator PopupCo(GameObject canvas)
    {
        if (flowerNotEnoughCanvas) flowerNotEnoughCanvas.SetActive(false);
        if (colorMismatchCanvas) colorMismatchCanvas.SetActive(false);

        canvas.SetActive(true);
        yield return new WaitForSeconds(popupDuration);
        if (canvas) canvas.SetActive(false);

        errorPlaying = false;
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
        if (col.attachedRigidbody != null)
            return col.attachedRigidbody.transform;

        return col.transform.root;
    }
}
