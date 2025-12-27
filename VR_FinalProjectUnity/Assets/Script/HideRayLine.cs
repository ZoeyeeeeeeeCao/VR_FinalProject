using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HideRayLine : MonoBehaviour
{
    public Behaviour lineVisual; // drag "XR Interactor Line Visual" here

    public void HideLine()
    {
        if (lineVisual) lineVisual.enabled = false;
    }
}
