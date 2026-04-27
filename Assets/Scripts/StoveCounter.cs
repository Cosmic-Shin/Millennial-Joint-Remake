using UnityEngine;

/// <summary>
/// Stove now only cooks PattyRaw → PattyCooked.
/// Ham no longer needs cooking (goes directly to sandwich from IngredientBox).
/// Chicken is handled separately by a FryerCounter if used.
/// </summary>
public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    [Header("Cooking")]
    public float cookingTime = 5f; // Adjustable cooking time
    public float overcookTime = 10f; // Time before cooked item is destroyed
    public GameObject cookingUIPanel; // UI panel to show during cooking
    public Transform cookingAnchor; // Where the item is placed for cooking

    private bool isCooking = false;
    private float cookingTimer = 0f;
    private bool isOvercooked = false;
    private float overcookTimer = 0f;

    // Valid interactions:
    //   - Stove empty + holding PattyRaw → place it
    //   - Stove has PattyRaw + empty hands → check cooking status
    //   - Stove has PattyCooked + empty hands → pick it up
    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.PattyRaw;

        if (storedItem.type == ItemType.PattyRaw || isCooking)
            return player.heldItem.IsEmpty; // check cooking status

        if (storedItem.type == ItemType.PattyCooked)
            return player.heldItem.IsEmpty; // pick up

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        // If cooking, check if done
        if (isCooking)
        {
            if (cookingTimer <= 0f)
            {
                // Cooking finished
                storedItem.Set(ItemType.PattyCooked);
                UpdateStoredItemVisual();
                ShowCookingUI(false);
                isCooking = false;
                Show(player, "Patty cooked!");
            }
            else
            {
                Show(player, $"Cooking... {cookingTimer:F1}s left");
            }
            return;
        }

        // Place raw patty
        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.PattyRaw)
            {
                storedItem.Set(ItemType.PattyRaw);
                player.heldItem.Clear();
                UpdateStoredItemVisual();
                StartCooking();
                Show(player, "Placed raw patty on stove - cooking started");
                return;
            }

            Show(player, "Place a raw patty on the stove");
            return;
        }

        // Pick up cooked patty
        if (storedItem.type == ItemType.PattyCooked)
        {
            player.heldItem.Set(ItemType.PattyCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            isOvercooked = false;
            overcookTimer = 0f;
            Show(player, "Picked up cooked patty");
            return;
        }

        Show(player, "Cannot use stove right now");
    }

    private void Update()
    {
        if (isCooking)
        {
            cookingTimer -= Time.deltaTime;
            if (cookingTimer <= 0f)
            {
                // Cooking finished
                storedItem.Set(ItemType.PattyCooked);
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
                Debug.Log("StoveCounter: Cooked patty overcooked and destroyed!");
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

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}