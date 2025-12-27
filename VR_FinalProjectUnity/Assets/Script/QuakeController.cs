using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class QuakeController : MonoBehaviour
{
    public GameObject level2Group;
    public GameObject level3Group;

    public Rigidbody[] apartmentCanvasRBs;
    public XRGrabInteractable[] apartmentCanvasGrabs;

    public float dropImpulse = 0.15f;
    public float reKinematicDelay = 5f;

    public float endDelay = 5f;   // ?? delay before End logic runs

    public void Begin()
    {
        if (level2Group) level2Group.SetActive(false);

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

            StartCoroutine(SetKinematicLater(rb, reKinematicDelay));
        }

        Debug.Log("QUAKE DROP called");
    }

    private IEnumerator SetKinematicLater(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!rb) yield break;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // ?? End is now delayed
    public void End()
    {
        StartCoroutine(EndDelayed());
    }

    private IEnumerator EndDelayed()
    {
        Debug.Log("QUAKE End called ¡ª waiting " + endDelay + " seconds...");
        yield return new WaitForSeconds(endDelay);

        if (level3Group) level3Group.SetActive(true);

        foreach (var g in apartmentCanvasGrabs)
            if (g) g.enabled = true;

        Debug.Log("QUAKE END logic executed");
    }
}
