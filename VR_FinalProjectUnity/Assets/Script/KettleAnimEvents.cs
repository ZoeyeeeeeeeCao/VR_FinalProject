using UnityEngine;

public class KettleAnimEvents : MonoBehaviour
{
    public WorkbenchMixZone zone;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip waterPourSfx; // W1

    // 🔊 Called by animation event WHEN WATER STARTS
    public void PlayWaterSfx()
    {
        if (audioSource != null && waterPourSfx != null)
            audioSource.PlayOneShot(waterPourSfx);
    }

    // 🔔 Called by animation event AT END
    public void PourFinished()
    {
        if (zone != null)
            zone.OnPourFinished();
    }
}
