using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class FlowerBasketPlacement : MonoBehaviour
{
    public Vector3 basketScale = Vector3.one * 0.5f;
    public float scaleTime = 0.25f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private Rigidbody rb;
    private Vector3 originalScale;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        originalScale = transform.localScale;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        StopAllCoroutines();
        transform.localScale = originalScale;
        rb.isKinematic = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
        {
            rb.isKinematic = true;
            StartCoroutine(ScaleTo(basketScale));
        }
    }

    IEnumerator ScaleTo(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / scaleTime;
            transform.localScale = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.localScale = target;
    }
}
