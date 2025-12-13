using UnityEngine;

public class MashController : MonoBehaviour
{
    public Bucket bucket;             // Drag the bucket here
    public GameObject powderPrefab;   // Particle effect prefab
    public KeyCode mashKey = KeyCode.M; // Press M to mash

    void Update()
    {
        if (Input.GetKeyDown(mashKey))
            MashFlowers();
    }

    public void MashFlowers()
    {
        if (bucket == null || powderPrefab == null || bucket.flowersInBucket.Count == 0)
            return;

        int red = 0, blue = 0, yellow = 0;

        foreach (GameObject flower in bucket.flowersInBucket)
        {
            if (flower == null) continue;

            if (flower.CompareTag("FlowerRed")) red++;
            else if (flower.CompareTag("FlowerBlue")) blue++;
            else if (flower.CompareTag("FlowerYellow")) yellow++;

            Instantiate(powderPrefab, flower.transform.position, Quaternion.identity);
            Destroy(flower);
        }

        bucket.flowersInBucket.Clear();
        Debug.Log($"Mashed -> Red: {red}, Blue: {blue}, Yellow: {yellow}");
    }
}
