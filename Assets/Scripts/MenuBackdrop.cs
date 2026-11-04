using UnityEngine;
using UnityEngine.UI;

// Hides the map behind open menu panels: shows the space backdrop camera (and/or a flat image) while any panel is open.
public class MenuBackdrop : MonoBehaviour
{
    public float holdAfterClose = 0.1f;
    [Tooltip("Camera that renders the animated space backdrop over the map while menus are open")]
    public Camera backdropCamera;

    private static int openPanels;
    private static float lastClosedTime = float.NegativeInfinity;

    private Image image;

    public static void PanelOpened() => openPanels++;

    public static void PanelClosed()
    {
        openPanels = Mathf.Max(0, openPanels - 1);
        lastClosedTime = Time.unscaledTime;
    }

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        bool show = openPanels > 0 || Time.unscaledTime - lastClosedTime < holdAfterClose;
        if (image != null)
            image.enabled = show;
        if (backdropCamera != null)
            backdropCamera.enabled = show;
    }
}
