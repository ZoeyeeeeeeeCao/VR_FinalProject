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
        basket.CollectFlower(other.gameObject);
    }
}
