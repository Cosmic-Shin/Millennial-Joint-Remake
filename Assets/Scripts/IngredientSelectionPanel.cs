using UnityEngine;
using UnityEngine.UI;

public class IngredientSelectionPanel : MonoBehaviour
{
    public GameObject panelObject;
    public Button bunButton;
    public Button veggieButton;
    public Button pattyButton;
    public Button friesButton;
    public Button baconButton;
    public Button chickenButton;
    public Button closeButton;

    private PlayerControl currentPlayer;
    private IngredientBox parentBox;

    private void Awake()
    {
        if (bunButton != null)
            bunButton.onClick.AddListener(() => SelectIngredient(ItemType.Bun));

        if (veggieButton != null)
            veggieButton.onClick.AddListener(() => SelectIngredient(ItemType.VeggieRaw));

        if (pattyButton != null)
            pattyButton.onClick.AddListener(() => SelectIngredient(ItemType.PattyRaw));

        if (friesButton != null)
            friesButton.onClick.AddListener(() => SelectIngredient(ItemType.FrozenFries));

        if (baconButton != null)
            baconButton.onClick.AddListener(() => SelectIngredient(ItemType.BaconRaw));

        if (chickenButton != null)
            chickenButton.onClick.AddListener(() => SelectIngredient(ItemType.ChickenRaw));

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void ShowPanel(PlayerControl player, IngredientBox box)
    {
        currentPlayer = player;
        parentBox = box;

        if (panelObject != null)
            panelObject.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelObject != null)
            panelObject.SetActive(false);

        currentPlayer = null;
        parentBox = null;
    }

    private void SelectIngredient(ItemType ingredientType)
    {
        if (currentPlayer == null || parentBox == null) return;

        Debug.Log("IngredientBox.Interact called | player holding before: " + currentPlayer.GetHeldItemDebug());

        currentPlayer.heldItem.Set(ingredientType);
        Debug.Log("IngredientBox.Interact completed | player holding after: " + currentPlayer.GetHeldItemDebug());
        parentBox.Show(currentPlayer, "Picked up " + currentPlayer.heldItem.GetDisplayName());

        ClosePanel();
    }
}
