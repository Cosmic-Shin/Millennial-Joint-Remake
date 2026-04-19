using UnityEngine;
using UnityEngine.UI;

public class IngredientSelectionPanel : MonoBehaviour
{
    public GameObject panelObject;
    public Button bunButton;
    public Button breadButton;
    public Button veggieButton;
    public Button pattyButton;
    public Button friesButton;
    public Button chickenButton;
    public Button hamButton;
    public Button cheeseButton;
    public Button closeButton;

    private PlayerControl currentPlayer;
    private IngredientBox parentBox;

    private void Awake()
    {
        if (bunButton != null)
            bunButton.onClick.AddListener(() => SelectIngredient(ItemType.Bun));

        if (breadButton != null)
            breadButton.onClick.AddListener(() => SelectIngredient(ItemType.Bread));

        if (veggieButton != null)
            veggieButton.onClick.AddListener(() => SelectIngredient(ItemType.VeggieRaw));

        if (pattyButton != null)
            pattyButton.onClick.AddListener(() => SelectIngredient(ItemType.PattyRaw));

        if (friesButton != null)
            friesButton.onClick.AddListener(() => SelectIngredient(ItemType.FrozenFries));

        if (chickenButton != null)
            chickenButton.onClick.AddListener(() => SelectIngredient(ItemType.ChickenRaw));

        if (hamButton != null)
            hamButton.onClick.AddListener(() => SelectIngredient(ItemType.HamRaw));

        if (cheeseButton != null)
            cheeseButton.onClick.AddListener(() => SelectIngredient(ItemType.Cheese));

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void ShowPanel(PlayerControl player, IngredientBox box)
    {
        currentPlayer = player;
        parentBox = box;

        if (currentPlayer != null)
            currentPlayer.doMove = false;

        if (panelObject != null)
            panelObject.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelObject != null)
            panelObject.SetActive(false);

        if (currentPlayer != null)
            currentPlayer.doMove = true;

        currentPlayer = null;
        parentBox = null;
    }

    private void SelectIngredient(ItemType ingredientType)
    {
        if (currentPlayer == null || parentBox == null) return;

        Debug.Log("IngredientBox.Interact called | player holding before: " + currentPlayer.GetHeldItemDebug());

        currentPlayer.heldItem.Set(ingredientType);
        currentPlayer.RefreshHeldItemDisplay();
        Debug.Log("IngredientBox.Interact completed | player holding after: " + currentPlayer.GetHeldItemDebug());
        parentBox.Show(currentPlayer, "Picked up " + currentPlayer.heldItem.GetDisplayName());

        ClosePanel();
    }
}
