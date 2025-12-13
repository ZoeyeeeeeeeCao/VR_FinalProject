using UnityEngine;

public class ShovelMash : MonoBehaviour
{
    [Header("References")]
    public Bucket bucket;              // Drag the bucket object here
    public GameObject powderPrefab;    // Powder particle prefab

    [Header("Controls")]
    public KeyCode mashKey = KeyCode.M; // For testing with keyboard

    void Update()
    {
        // For testing on laptop
        if (Input.GetKeyDown(mashKey))
        {
            MashFlowers();
        }
    }

    public void MashFlowers()
    {
        if (bucket == null || powderPrefab == null)
        {
            Debug.LogWarning("Bucket or PowderPrefab not assigned!");
            return;
        }

        if (bucket.flowersInBucket.Count == 0)
        {
            Debug.Log("No flowers in the bucket to mash.");
            return;
        }

        int red = 0, blue = 0, yellow = 0;

        foreach (GameObject flower in bucket.flowersInBucket)
        {
            if (flower == null) continue;

            // Count flower types
            if (flower.CompareTag("FlowerRed")) red++;
            else if (flower.CompareTag("FlowerBlue")) blue++;
            else if (flower.CompareTag("FlowerYellow")) yellow++;

            // Spawn powder at flower position
            Instantiate(powderPrefab, flower.transform.position, Quaternion.identity);

            // Remove flower from scene
            Destroy(flower);
        }

        // Clear the bucket list
        bucket.flowersInBucket.Clear();

        // Debug info
        Debug.Log($"Mashed Flowers -> Red: {red}, Blue: {blue}, Yellow: {yellow}");
    }
}
