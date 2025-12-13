using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class BasketLogic : MonoBehaviour
{
    public Transform[] placementPoints;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    private int index = 0;
    private HashSet<GameObject> stored = new HashSet<GameObject>();

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    void OnEnable()
    {
        socket.selectEntered.AddListener(OnSocketEntered);
    }

    void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnSocketEntered);
    }

    void OnSocketEntered(SelectEnterEventArgs args)
    {
        Debug.Log("[BASKET] Socket triggered by: " + args.interactableObject.transform.name);
        
        if (index >= placementPoints.Length)
            return;

        GameObject flower = args.interactableObject.transform.gameObject;

        if (stored.Contains(flower))
            return;

        stored.Add(flower);

        // Force XR to release the flower
        socket.interactionManager.SelectExit(socket, args.interactableObject);

        // Disable interaction
        var grab = flower.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        var rb = flower.GetComponent<Rigidbody>();

        if (grab) grab.enabled = false;
        if (rb) rb.isKinematic = true;

        // Place flower inside basket (OUTSIDE trigger)
        flower.transform.SetParent(transform);
        flower.transform.position = placementPoints[index].position;
        flower.transform.rotation = placementPoints[index].rotation;

        index++;
    }
}
