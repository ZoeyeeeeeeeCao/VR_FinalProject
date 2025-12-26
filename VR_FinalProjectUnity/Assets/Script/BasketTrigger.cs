using UnityEngine;

public class BasketTrigger : MonoBehaviour
{
    private BasketLogic basket;

    void Awake()
    {
        basket = GetComponentInParent<BasketLogic>();
    }

    void OnTriggerEnter(Collider other)
    {
        //  Do NOT collect while basket is locked/snapped
        if (basket.IsLockedToTable)
            return;

        basket.CollectFlower(other.gameObject);
    }
}
