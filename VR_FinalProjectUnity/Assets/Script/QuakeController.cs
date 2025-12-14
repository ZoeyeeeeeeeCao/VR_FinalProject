using UnityEngine;

public class QuakeController : MonoBehaviour
{
    [Header("Mode Groups")]
    public GameObject level2Group;
    public GameObject level3Group;

    [Header("Painted Canvases")]
    public CanvasLockController[] canvases; // 1..3 (from PaintSession)
    public Transform[] wallHooks;           // 1..3

    [Header("Drop Force")]
    public float dropImpulse = 0.15f;

    // Called at quake start
    public void BeginEarthquake()
    {
        if (level2Group) level2Group.SetActive(false);

        // Keep canvases visible & move them to the walls for the cinematic moment
        for (int i = 0; i < canvases.Length && i < wallHooks.Length; i++)
        {
            var c = canvases[i];
            if (!c) continue;

            c.LockTo(wallHooks[i]); // snap to wall
            var rb = c.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    // Called at the “drop” moment
    public void DropCanvases()
    {
        foreach (var c in canvases)
        {
            if (!c) continue;

            // unparent so physics can act
            c.transform.SetParent(null, true);

            var rb = c.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(Random.insideUnitSphere * dropImpulse, ForceMode.Impulse);
            }

            // optional: keep grabbing disabled until Level 3 starts
            if (c.grab) c.grab.enabled = false;
        }
    }

    // Called at the end
    public void EndEarthquake()
    {
        if (level3Group) level3Group.SetActive(true);

        // Now enable grabbing for canvases so player can pick them up
        foreach (var c in canvases)
            if (c && c.grab) c.grab.enabled = true;
    }
}
