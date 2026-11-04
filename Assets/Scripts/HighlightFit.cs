using TMPro;
using UnityEngine;

// Keeps a highlighter strip sized to its text's glyphs (labels like "Keyboard"/"Joystick" change width at runtime).
// Visual only: ChalkButton still animates the strip's fillAmount.
public class HighlightFit : MonoBehaviour
{
    public TMP_Text target;
    [Tooltip("Extra width on each side, in canvas units")]
    public float padding = 4f;

    private void LateUpdate() => Fit();

    public void Fit()
    {
        if (target == null || string.IsNullOrWhiteSpace(target.GetParsedText()))
            return;

        var rt = (RectTransform)transform;
        Transform parent = rt.parent;
        Bounds b = target.textBounds;
        Vector3 min = parent.InverseTransformPoint(target.transform.TransformPoint(b.min));
        Vector3 max = parent.InverseTransformPoint(target.transform.TransformPoint(b.max));
        float pad = padding * target.canvas.rootCanvas.transform.lossyScale.x / parent.lossyScale.x;

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        Vector3 center = (min + max) * 0.5f;
        rt.localPosition = new Vector3(center.x, center.y, rt.localPosition.z);
        Vector3 s = rt.localScale;
        rt.sizeDelta = new Vector2((Mathf.Abs(max.x - min.x) + 2f * pad) / s.x, Mathf.Abs(max.y - min.y) / s.y);
    }
}
