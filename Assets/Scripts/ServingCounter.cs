using UnityEngine;

public class ServingCounter : BaseStation, IInteractable
{
    public int totalServed;

    [Header("Price Popup")]
    public GameObject pricePopupPrefab;

    [Header("Popup Settings")]
    public float popupYOffset = 1.5f;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        bool hasCompletePlate = player.heldItem.IsPlate &&
            (player.heldItem.IsCompleteBurger ||
             player.heldItem.IsCompleteSandwich ||
             player.heldItem.IsCompleteFriedChicken ||
             player.heldItem.IsCompleteFries ||
             player.heldItem.IsCompleteChiliDog);

        bool hasCompleteDrink = player.heldItem.IsCompleteDrink;
        
        // Check for direct ice cream items (not in cup)
        bool hasIceCream = player.heldItem.type == ItemType.StrawberryIceCream ||
                          player.heldItem.type == ItemType.BubblegumIceCream ||
                          player.heldItem.type == ItemType.MangoIceCream;

        return hasCompletePlate || hasCompleteDrink || hasIceCream;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        float earned = 0f;

        if (OrderManager.Instance != null)
            earned = OrderManager.Instance.TryServeItem(player, player.heldItem);

        if (earned <= 0f)
        {
            Show(player, player.heldItem.GetDisplayName() + " is not part of the current order");
            return;
        }

        string servedName = player.heldItem.GetDisplayName();

        player.heldItem.Clear();
        player.RefreshHeldItemDisplay();

        totalServed++;

        if (OrderManager.Instance != null &&
            OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS)
        {
            Show(player, "Player " + player.playerNumber + " served " + servedName + "!");
        }
        else
        {
            Show(player, servedName + " served! Total served: " + totalServed);
        }

        SpawnPricePopup(earned);

        AudioManager.Instance?.PlayServeFoodSFX();
    }

    private void SpawnPricePopup(float earned)
    {
        if (pricePopupPrefab == null)
            return;

        Vector3 popupPos = transform.position + Vector3.up * popupYOffset;
        GameObject popup = Instantiate(pricePopupPrefab, popupPos, Quaternion.identity);

        WorldTextFade fade = popup.GetComponent<WorldTextFade>();

        if (fade != null)
            fade.Play("$" + Mathf.RoundToInt(earned), Color.green);
        else
            Debug.LogWarning("Price popup prefab needs WorldTextFade script.");
    }
}