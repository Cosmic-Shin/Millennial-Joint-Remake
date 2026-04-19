using System.Collections;
using UnityEngine;

public class BaseStation : MonoBehaviour
{
    [Header("UI Feedback")]
    public InteractionPanel interactionPanel;

    [Header("Panel Animation")]
    public float animationTime = 0.5f;

    public void OpenPanel(bool show)
    {
        if (interactionPanel == null) return;
        if (interactionPanel.panelCanvas == null) return;

        LeanTween.cancel(interactionPanel.panelCanvas.gameObject);

        if (show)
        {
            interactionPanel.panelCanvas.gameObject.SetActive(true);
            interactionPanel.panelCanvas.transform.localScale = Vector3.zero;
            interactionPanel.panelCanvas.transform
                .LeanScale(Vector3.one, animationTime)
                .setEaseOutQuint();
        }
        else
        {
            interactionPanel.panelCanvas.transform
                .LeanScale(Vector3.zero, animationTime)
                .setEaseOutQuint()
                .setOnComplete(() =>
                {
                    interactionPanel.panelCanvas.gameObject.SetActive(false);
                });
        }
    }

    public void Show(PlayerControl player, string message)
    {
        string fullMessage = message;
        if (player != null)
            fullMessage += "\n" + player.GetHeldItemDebug();

        Debug.Log("[" + gameObject.name + "] " + fullMessage);

        if (interactionPanel != null)
            interactionPanel.ShowMessage(fullMessage);
    }
}