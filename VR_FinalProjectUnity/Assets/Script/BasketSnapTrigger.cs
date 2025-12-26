using UnityEngine;

public class BasketSnapTrigger : MonoBehaviour
{
    public Transform basketSnapPoint;

    // 🔊 ADD THESE TWO LINES
    public AudioClip puckSound;
    private AudioSource audioSource;

    private bool hasSnapped = false;

    void Awake()
    {
        // 🔊 ADD THIS (does not affect logic)
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

        // 🔊 PLAY SOUND (ONE LINE)
        if (audioSource != null && puckSound != null)
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
