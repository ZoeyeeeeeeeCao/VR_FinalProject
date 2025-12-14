using UnityEngine;

public class ParticleByAnimation : MonoBehaviour
{
    public ParticleSystem ps;

    public void PlayParticles()
    {
        if (ps == null) return;
        ps.Play();
    }

    public void StopParticles()
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
