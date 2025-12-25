using UnityEngine;

public class KettleAnimEvents : MonoBehaviour
{
    public WorkbenchMixZone zone;

    public void PourFinished()
    {
        if (zone != null) zone.OnPourFinished();
    }
}
