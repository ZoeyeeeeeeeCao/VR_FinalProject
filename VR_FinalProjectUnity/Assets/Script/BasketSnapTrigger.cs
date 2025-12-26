using UnityEngine;

public class BasketSnapTrigger : MonoBehaviour
{
    public Transform basketSnapPoint;
    public AudioClip puckSound;

    private bool hasSnapped = false;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasSnapped)
            return;

        BasketLogic basket = other.GetComponentInParent<BasketLogic>();
        if (basket == null)
            return;

        basket.SnapToTable(basketSnapPoint);

        // 🔊 PLAY PUCK SOUND
        if (puckSound != null && audioSource != null)
            audioSource.PlayOneShot(puckSound);

        hasSnapped = true;
    }

    void OnTriggerExit(Collider other)
    {
        BasketLogic basket = other.GetComponentInParent<BasketLogic>();
        if (basket == null)
            return;

        hasSnapped = false;
    }
}
