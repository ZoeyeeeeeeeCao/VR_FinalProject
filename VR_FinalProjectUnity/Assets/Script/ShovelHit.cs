using UnityEngine;

public class ShovelHit : MonoBehaviour
{
    [Header("Mashing Settings")]
    public float mashPower = 1f;           // How much one hit adds
    public float mashThreshold = 3f;       // Hits needed to mash flower
    public GameObject powderPrefab;        // Powder effect prefab
    public Transform mashSpawnPoint;       // Where powder spawns

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Flower"))
        {
            Flower flower = collision.gameObject.GetComponent<Flower>();
            if (flower != null)
            {
                flower.AddMash(mashPower);

                if (flower.mashAmount >= mashThreshold)
                {
                    MashFlower(flower);
                }
            }
        }
    }

    void MashFlower(Flower flower)
    {
        // Spawn powder effect
        if (powderPrefab != null && mashSpawnPoint != null)
            Instantiate(powderPrefab, mashSpawnPoint.position, Quaternion.identity);

        // Destroy flower
        Destroy(flower.gameObject);
    }
}
