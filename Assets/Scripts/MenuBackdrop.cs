using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MenuBackdrop : MonoBehaviour
{
    public float holdAfterClose = 0.1f;

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
        image.enabled = openPanels > 0 || Time.unscaledTime - lastClosedTime < holdAfterClose;
    }
}
