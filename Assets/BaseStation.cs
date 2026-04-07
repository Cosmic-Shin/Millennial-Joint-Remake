using UnityEngine;

public class BaseStation : MonoBehaviour
{
    [Header("UI Feedback")]
    public InteractionPanel interactionPanel;

    protected void Show(PlayerControl player, string message)
    {
        string fullMessage = message;

        if (player != null)
            fullMessage += "\n" + player.GetHeldItemDebug();

        Debug.Log("[" + gameObject.name + "] " + fullMessage);

        if (interactionPanel != null)
            interactionPanel.ShowMessage(fullMessage);
    }
}