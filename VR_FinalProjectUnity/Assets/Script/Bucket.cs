using UnityEngine;
using System.Collections.Generic;

public class Bucket : MonoBehaviour
{
    [HideInInspector]
    public List<GameObject> flowersInBucket = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FlowerRed") || other.CompareTag("FlowerBlue") || other.CompareTag("FlowerYellow"))
        {
            if (!flowersInBucket.Contains(other.gameObject))
                flowersInBucket.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (flowersInBucket.Contains(other.gameObject))
            flowersInBucket.Remove(other.gameObject);
    }
}
