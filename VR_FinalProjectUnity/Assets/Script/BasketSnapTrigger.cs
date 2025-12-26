using UnityEngine;

public class BasketSnapTrigger : MonoBehaviour
{
    public Transform basketSnapPoint;

    private bool hasSnapped = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasSnapped)
            return;

        BasketLogic basket = other.GetComponentInParent<BasketLogic>();
        if (basket == null)
            return;

        basket.SnapToTable(basketSnapPoint);
        hasSnapped = true;
    }

    void OnTriggerExit(Collider other)
    {
        BasketLogic basket = other.GetComponentInParent<BasketLogic>();
        if (basket == null)
            return;

        // Reset only when basket leaves trigger volume
        hasSnapped = false;
    }
}
