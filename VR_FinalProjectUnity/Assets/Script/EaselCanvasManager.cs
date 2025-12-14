using UnityEngine;

public class EaselCanvasManager : MonoBehaviour
{
    [Header("Easel")]
    public Transform canvasSlot;              // where the canvas snaps

    [Header("Menu")]
    public VRMenuToggle menuToggle;           // your existing script (optional)

    [Header("Limits")]
    public int totalCanvasesAllowed = 3;      // total canvases in the session

    private int canvasesUsed = 0;             // counts how many have been placed on easel
    private bool waitingForReplacement = false;

    private CanvasLockController currentCanvas;

    void Start()
    {
        // If a canvas starts already on the easel, register it
        var existing = canvasSlot.GetComponentInChildren<CanvasLockController>();
        if (existing != null)
        {
            currentCanvas = existing;
            currentCanvas.LockTo(canvasSlot);
            canvasesUsed = 1;
        }
    }

    // UI Button: YES
    public void ConfirmDoneYes()
    {
        if (currentCanvas != null)
        {
            currentCanvas.Unlock();
            waitingForReplacement = true;
        }

        if (menuToggle != null) menuToggle.SetOpen(false);
    }

    // UI Button: NO
    public void ConfirmDoneNo()
    {
        if (menuToggle != null) menuToggle.SetOpen(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!waitingForReplacement) return;

        var newCanvas = other.GetComponentInParent<CanvasLockController>();
        if (newCanvas == null) return;

        // Prevent re-locking the same canvas you just unlocked
        if (newCanvas == currentCanvas) return;

        // Enforce total count
        if (canvasesUsed >= totalCanvasesAllowed) return;

        // Lock the new one to easel
        currentCanvas = newCanvas;
        currentCanvas.LockTo(canvasSlot);

        canvasesUsed++;
        waitingForReplacement = false;
    }
}
