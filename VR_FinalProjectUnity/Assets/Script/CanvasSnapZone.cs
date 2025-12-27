using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CanvasSnapZone : MonoBehaviour
{
    [Header("Snap target")]
    public Transform attachPoint;

    [Header("Filter")]
    public string requiredTag = "Default";   // set to "Default" or your own tag

    [Header("Behavior")]
    public bool snapWhenReleasedInside = true; // if held on enter, snap after release (via OnTriggerStay)
    public bool lockAfterSnap = true;          // freeze RB + disable grab
    public float snapDistance = 0.25f;         // must be this close to snap point
    public float snapCooldown = 0.25f;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        TrySnap(other, "Enter");
    }

    void OnTriggerStay(Collider other)
    {
        if (snapWhenReleasedInside)
            TrySnap(other, "Stay");
    }

    void TrySnap(Collider other, string phase)
    {
        if (!attachPoint) return;

        // Find the XRGrabInteractable on this collider or its parents
        var grab = other.GetComponentInParent<XRGrabInteractable>();
        if (!grab) return;

        // ? Tag check on the interactable root (NOT the child collider)
        if (!string.IsNullOrEmpty(requiredTag) && !grab.CompareTag(requiredTag))
        {
            // Debug once in a while so it doesn't spam too hard
            // (Comment this out if noisy)
            Debug.Log($"[SnapZone] {phase}: Found {grab.name} but tag is '{grab.tag}', need '{requiredTag}'.");
            return;
        }

        // If it's being held, don't snap on enter; allow stay to snap after release
        if (grab.isSelected)
        {
            // Helpful debug
            // Debug.Log($"[SnapZone] {phase}: {grab.name} is selected (held), waiting for release...");
            return;
        }

        // Distance gate (prevents snapping from far away inside big trigger)
        float d = Vector3.Distance(grab.transform.position, attachPoint.position);
        if (d > snapDistance) return;

        // Cooldown gate (prevents jittery multi-snap)
        var cd = grab.GetComponent<SnapCooldown>();
        if (!cd) cd = grab.gameObject.AddComponent<SnapCooldown>();
        if (!cd.CanSnap(snapCooldown)) return;

        DoSnap(grab);
        Debug.Log($"[SnapZone] Snapped {grab.name} to {attachPoint.name} (distance {d:F3}).");
    }

    void DoSnap(XRGrabInteractable grab)
    {
        var rb = grab.GetComponent<Rigidbody>();

        // Detach from parent
        grab.transform.SetParent(null, true);

        Debug.Log("SNAP!!");

        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Force snap
            rb.MovePosition(attachPoint.position);
            rb.MoveRotation(attachPoint.rotation);

            if (lockAfterSnap)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        else
        {
            // No rigidbody case
            grab.transform.SetPositionAndRotation(attachPoint.position, attachPoint.rotation);
        }

        if (lockAfterSnap)
        {
            // Disable grabbing after snap
            grab.enabled = false;
        }
    }

    private class SnapCooldown : MonoBehaviour
    {
        float last = -999f;
        public bool CanSnap(float cd) { if (Time.time - last < cd) return false; last = Time.time; return true; }
    }
}
