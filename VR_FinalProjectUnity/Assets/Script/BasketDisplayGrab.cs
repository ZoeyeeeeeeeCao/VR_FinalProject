using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BasketDisplayGrab : MonoBehaviour
{
    public FlowerType type;

    private BasketLogic basket;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private bool isTaking = false;

    void Awake()
    {
        // Find BasketLogic from parent hierarchy at runtime
        basket = GetComponentInParent<BasketLogic>();

        if (basket == null)
        {
            Debug.LogError("[BasketDisplayGrab] BasketLogic not found in parent.");
            return;
        }

        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab == null)
        {
            Debug.LogError("[BasketDisplayGrab] XRGrabInteractable missing on display prefab.");
            return;
        }

        grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrabbed);
    }

    void OnMouseDown()
    {
        Debug.Log("[BasketDisplayGrab] MouseDown detected");
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // Prevent multiple removals from one grab
        if (isTaking)
            return;

        isTaking = true;

        Debug.Log("[BasketDisplayGrab] Grab detected");

        Transform handTransform = args.interactorObject.transform;

        GameObject realFlower = basket.TakeFlower(type, handTransform);
        if (realFlower == null)
        {
            isTaking = false;
            return;
        }

        var realGrab = realFlower.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (realGrab != null)
        {
            realGrab.interactionManager.SelectEnter(
                args.interactorObject,
                realGrab
            );
        }

        // Allow next take after a short delay
        Invoke(nameof(ResetTake), 0.1f);
    }

    void ResetTake()
    {
        isTaking = false;
    }
}
