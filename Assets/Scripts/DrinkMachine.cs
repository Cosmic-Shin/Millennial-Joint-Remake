using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interact while holding an empty cup → opens panel to choose Soda, Ice Tea, or Orange Juice.
/// The selection panel is just a plain GameObject — no separate script needed.
/// </summary>
public class DrinkMachine : BaseStation, IInteractable
{
    [Header("Panel")]
    public GameObject selectionPanel;

    [Header("Buttons")]
    public Button sodaButton;
    public Button iceTeaButton;
    public Button orangeJuiceButton;
    public Button closeButton;

    private PlayerControl currentPlayer;

    private void Awake()
    {
        if (sodaButton        != null) sodaButton       .onClick.AddListener(() => SelectDrink("soda"));
        if (iceTeaButton      != null) iceTeaButton     .onClick.AddListener(() => SelectDrink("icetea"));
        if (orangeJuiceButton != null) orangeJuiceButton.onClick.AddListener(() => SelectDrink("oj"));
        if (closeButton       != null) closeButton      .onClick.AddListener(ClosePanel);

        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsCup
               && !player.heldItem.cupHasSoda
               && !player.heldItem.cupHasIceTea
               && !player.heldItem.cupHasOrangeJuice;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (!player.heldItem.IsCup)
        {
            Show(player, "Hold an empty cup first");
            return;
        }

        if (player.heldItem.cupHasSoda || player.heldItem.cupHasIceTea || player.heldItem.cupHasOrangeJuice)
        {
            Show(player, "Cup is already filled");
            return;
        }

        currentPlayer = player;
        currentPlayer.doMove = false;

        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }

    private void SelectDrink(string drinkKey)
    {
        if (currentPlayer == null) return;

        switch (drinkKey)
        {
            case "soda":
                currentPlayer.heldItem.cupHasSoda        = true;
                currentPlayer.heldItem.cupHasIceTea      = false;
                currentPlayer.heldItem.cupHasOrangeJuice = false;
                Show(currentPlayer, "Filled cup with Soda");
                break;

            case "icetea":
                currentPlayer.heldItem.cupHasIceTea      = true;
                currentPlayer.heldItem.cupHasSoda        = false;
                currentPlayer.heldItem.cupHasOrangeJuice = false;
                Show(currentPlayer, "Filled cup with Ice Tea");
                break;

            case "oj":
                currentPlayer.heldItem.cupHasOrangeJuice = true;
                currentPlayer.heldItem.cupHasSoda        = false;
                currentPlayer.heldItem.cupHasIceTea      = false;
                Show(currentPlayer, "Filled cup with Orange Juice");
                break;
        }

        currentPlayer.RefreshHeldItemDisplay();
        ClosePanel();
    }

    private void ClosePanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.doMove = true;

        currentPlayer = null;
    }
}