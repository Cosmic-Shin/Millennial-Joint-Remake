using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelFadeIn : MonoBehaviour
{
    public float duration = 0.25f;
    [Tooltip("Show the MenuBackdrop behind this panel so the map doesn't show while it fades in")]
    public bool useBackdrop = true;

    private Vector3 baseScale;
    private bool hasBaseScale;

    private void OnEnable()
    {
        if (!hasBaseScale)
        {
            baseScale = transform.localScale;
            hasBaseScale = true;
        }

        if (useBackdrop)
            MenuBackdrop.PanelOpened();

        StartCoroutine(VisualFx.FadeIn(GetComponent<CanvasGroup>(), transform, baseScale, duration));
    }

    private void OnDisable()
    {
        if (useBackdrop)
            MenuBackdrop.PanelClosed();
    }
}
