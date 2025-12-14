using UnityEngine;

public class UniquePaperRT : MonoBehaviour
{
    [Header("Assign these from your Paper object")]
    public Camera maskCamera;
    public Camera colorCamera;
    public Renderer paperRenderer; // the plane mesh renderer that uses the paper material

    [Header("Source RTs (the ones currently assigned)")]
    public RenderTexture sourceMask;
    public RenderTexture sourceColor;

    [Header("Shader property names (change if needed)")]
    public string maskProperty = "_MaskRT";
    public string colorProperty = "_ColorRT";

    RenderTexture _maskInstance;
    RenderTexture _colorInstance;

    void Awake()
    {
        // 1) Make sure this canvas has its OWN material instance
        if (paperRenderer != null)
            paperRenderer.material = new Material(paperRenderer.material);

        // 2) Create unique RTs (cloned from the source descriptors)
        _maskInstance = CloneRT(sourceMask, "MaskRT");
        _colorInstance = CloneRT(sourceColor, "ColorRT");

        // 3) Cameras must write into the unique RTs
        if (maskCamera) maskCamera.targetTexture = _maskInstance;
        if (colorCamera) colorCamera.targetTexture = _colorInstance;

        // 4) Material must sample the unique RTs
        var mat = paperRenderer != null ? paperRenderer.material : null;
        if (mat != null)
        {
            bool assigned = false;

            // Try direct property names first
            if (mat.HasProperty(maskProperty)) { mat.SetTexture(maskProperty, _maskInstance); assigned = true; }
            if (mat.HasProperty(colorProperty)) { mat.SetTexture(colorProperty, _colorInstance); assigned = true; }

            // Fallback: replace any texture slot that was using the shared RTs
            if (!assigned)
            {
                foreach (var prop in mat.GetTexturePropertyNames())
                {
                    var t = mat.GetTexture(prop);
                    if (t == sourceMask) mat.SetTexture(prop, _maskInstance);
                    if (t == sourceColor) mat.SetTexture(prop, _colorInstance);
                }
            }
        }
    }

    RenderTexture CloneRT(RenderTexture src, string label)
    {
        if (src == null)
        {
            // Safe default if you forgot to assign
            var rt = new RenderTexture(1024, 1024, 0);
            rt.name = $"{label}_{gameObject.name}_{GetInstanceID()}";
            rt.Create();
            return rt;
        }

        var copy = new RenderTexture(src.descriptor);
        copy.name = $"{label}_{gameObject.name}_{GetInstanceID()}";
        copy.Create();
        return copy;
    }

    void OnDestroy()
    {
        if (_maskInstance) _maskInstance.Release();
        if (_colorInstance) _colorInstance.Release();
    }
}
