using UnityEngine;

public class BasketTrigger : MonoBehaviour
{
    public BasketLogic basket;

    private void OnTriggerEnter(Collider other)
    {
        basket.CollectFlower(other.gameObject);
    }
}
