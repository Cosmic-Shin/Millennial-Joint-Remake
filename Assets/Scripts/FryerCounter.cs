using UnityEngine;

public class FryerCounter : StorageStation
{
    [Header("Cooking")]
    public float cookingTime = 7f; // Adjustable cooking time (different from stove)
    public float overcookTime = 10f; // Time before cooked item is destroyed
    public GameObject cookingUIPanel; // UI panel to show during cooking
    public Transform cookingAnchor; // Where the item is placed for cooking

    private bool isCooking = false;
    private float cookingTimer = 0f;
    private bool isOvercooked = false;
    private float overcookTimer = 0f;
    // Valid interactions:
    //   - Fryer empty + holding FrozenFries or ChickenRaw  → place item in fryer
    //   - Fryer has FrozenFries or ChickenRaw + empty hands  → check cooking status
    //   - Fryer has FriesCooked or ChickenCooked + empty hands  → pick up cooked item
    public override bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.FrozenFries || player.heldItem.type == ItemType.ChickenRaw;

        if (storedItem.type == ItemType.FrozenFries || storedItem.type == ItemType.ChickenRaw || isCooking)
            return player.heldItem.IsEmpty;

        if (storedItem.type == ItemType.FriesCooked || storedItem.type == ItemType.ChickenCooked)
            return player.heldItem.IsEmpty;

        return false;
    }

    public override void Interact(PlayerControl player)
    {
        if (player == null) return;

        // If cooking, check if done
        if (isCooking)
        {
            if (cookingTimer <= 0f)
            {
                // Cooking finished
                if (storedItem.type == ItemType.FrozenFries)
                    storedItem.Set(ItemType.FriesCooked);
                else if (storedItem.type == ItemType.ChickenRaw)
                    storedItem.Set(ItemType.ChickenCooked);
                UpdateStoredItemVisual();
                ShowCookingUI(false);
                isCooking = false;
                Show(player, storedItem.type == ItemType.FriesCooked ? "Fries cooked!" : "Chicken cooked!");
            }
            else
            {
                Show(player, $"Cooking... {cookingTimer:F1}s left");
            }
            return;
        }

        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.ChickenRaw)
            {
                storedItem.Set(ItemType.ChickenRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                StartCooking();
                Show(player, "Placed raw chicken in fryer - cooking started");
                return;
            }

            if (player.heldItem.type == ItemType.FrozenFries)
            {
                storedItem.Set(ItemType.FrozenFries);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                StartCooking();
                Show(player, "Placed frozen fries in fryer - cooking started");
                return;
            }

            Show(player, "Place frozen fries or raw chicken in fryer");
            return;
        }

        if (storedItem.type == ItemType.FriesCooked)
        {
            player.heldItem.Set(ItemType.FriesCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            isOvercooked = false;
            overcookTimer = 0f;
            Show(player, "Picked up cooked fries");
            return;
        }

        if (storedItem.type == ItemType.ChickenCooked)
        {
            player.heldItem.Set(ItemType.ChickenCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            isOvercooked = false;
            overcookTimer = 0f;
            Show(player, "Picked up cooked chicken");
            return;
        }
    }

    private void Update()
    {
        if (isCooking)
        {
            cookingTimer -= Time.deltaTime;
            if (cookingTimer <= 0f)
            {
                // Cooking finished
                if (storedItem.type == ItemType.FrozenFries)
                    storedItem.Set(ItemType.FriesCooked);
                else if (storedItem.type == ItemType.ChickenRaw)
                    storedItem.Set(ItemType.ChickenCooked);
                UpdateStoredItemVisual();
                ShowCookingUI(false);
                isCooking = false;
                isOvercooked = true;
                overcookTimer = overcookTime;
            }
        }
        else if (isOvercooked)
        {
            overcookTimer -= Time.deltaTime;
            if (overcookTimer <= 0f)
            {
                // Item overcooked - destroy it
                storedItem.Clear();
                UpdateStoredItemVisual();
                isOvercooked = false;
                Debug.Log("FryerCounter: Cooked item overcooked and destroyed!");
            }
        }
    }

    private void StartCooking()
    {
        isCooking = true;
        cookingTimer = cookingTime;
        ShowCookingUI(true);
    }

    private void ShowCookingUI(bool show)
    {
        if (cookingUIPanel != null)
            cookingUIPanel.SetActive(show);
    }
}