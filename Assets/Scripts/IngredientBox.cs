using UnityEngine;
using UnityEngine.UI;

public class IngredientBox : BaseStation, IInteractable
{
    [Header("Panel")]
    public GameObject selectionPanel;

    [Header("Buttons")]
    public Button bunButton;
    public Button breadButton;
    public Button veggieButton;
    public Button pattyButton;
    public Button friesButton;
    public Button chickenButton;
    public Button hamButton;      // Ham & Cheese combined
    public Button closeButton;

    private PlayerControl currentPlayer;

    private void Awake()
    {
        if (bunButton     != null) bunButton    .onClick.AddListener(() => SelectIngredient(ItemType.Bun));
        if (breadButton   != null) breadButton  .onClick.AddListener(() => SelectIngredient(ItemType.Bread));
        if (veggieButton  != null) veggieButton .onClick.AddListener(() => SelectIngredient(ItemType.VeggieRaw));
        if (pattyButton   != null) pattyButton  .onClick.AddListener(() => SelectIngredient(ItemType.PattyRaw));
        if (friesButton   != null) friesButton  .onClick.AddListener(() => SelectIngredient(ItemType.FrozenFries));
        if (chickenButton != null) chickenButton.onClick.AddListener(() => SelectIngredient(ItemType.ChickenRaw));
        if (hamButton     != null) hamButton    .onClick.AddListener(() => SelectIngredient(ItemType.HamRaw));
        if (closeButton   != null) closeButton  .onClick.AddListener(ClosePanel);

        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        currentPlayer = player;
        currentPlayer.doMove = false;

        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }

    private void SelectIngredient(ItemType ingredientType)
    {
        if (currentPlayer == null) return;

        currentPlayer.heldItem.Set(ingredientType);
        currentPlayer.RefreshHeldItemDisplay();
        Show(currentPlayer, "Picked up " + currentPlayer.heldItem.GetDisplayName());

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