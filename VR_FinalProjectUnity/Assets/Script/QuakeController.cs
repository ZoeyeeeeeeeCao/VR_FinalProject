using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class QuakeController : MonoBehaviour
{
    public GameObject level2Group;
    public GameObject level3Group;

    public Rigidbody[] apartmentCanvasRBs;
    public XRGrabInteractable[] apartmentCanvasGrabs;

    public float dropImpulse = 0.15f;

    public void Begin()
    {
        if (level2Group) level2Group.SetActive(false);

        // keep them ungrabbable during cinematic
        foreach (var g in apartmentCanvasGrabs)
            if (g) g.enabled = false;

        Debug.Log("QUAKE Begin called");
    }

    public void Drop()
    {
        foreach (var rb in apartmentCanvasRBs)
        {
            if (!rb) continue;
            rb.transform.SetParent(null, true);
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(Random.insideUnitSphere * dropImpulse, ForceMode.Impulse);
        }

        Debug.Log("QUAKE DROP called");
    }

    public void End()
    {
        if (level3Group) level3Group.SetActive(true);

        // now the player can pick them up and re-socket
        foreach (var g in apartmentCanvasGrabs)
            if (g) g.enabled = true;

        Debug.Log("QUAKE END called");
    }
}
