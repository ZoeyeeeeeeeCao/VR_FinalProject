using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class FlowerBasketPlacement : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
        {
            // Flower placed into basket
            Debug.Log("Flower placed into basket");
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
        {
            // Flower removed from basket
            Debug.Log("Flower removed from basket");
        }
    }
}
