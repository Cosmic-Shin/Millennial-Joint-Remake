using UnityEngine;
using UnityEngine.UI;

public class FryerCounter : StorageStation
{
    [Header("Cooking")]
    public float cookingTime = 7f;
    public float overcookTime = 10f;
    public GameObject cookingUIPanel;
    public Transform cookingAnchor;

    [Header("Cooking Animation")]
    public Image cookingUIImage;
    public Sprite[] cookingSprites;
    public int animationLoops = 3;

    private bool isCooking = false;
    private float cookingTimer = 0f;
    private bool isOvercooked = false;
    private float overcookTimer = 0f;

    private float animationTimer = 0f;
    private float animationDurationPerLoop;

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

        if (isCooking)
        {
            if (cookingTimer <= 0f)
            {
                FinishCooking();
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
            UpdateCookingAnimation();

            if (cookingTimer <= 0f)
            {
                FinishCooking();
                isOvercooked = true;
                overcookTimer = overcookTime;
            }
        }
        else if (isOvercooked)
        {
            overcookTimer -= Time.deltaTime;

            if (overcookTimer <= 0f)
            {
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

        animationTimer = 0f;
        animationDurationPerLoop = cookingTime / Mathf.Max(1, animationLoops);

        ShowCookingUI(true);
    }

    private void FinishCooking()
    {
        if (storedItem.type == ItemType.FrozenFries)
            storedItem.Set(ItemType.FriesCooked);
        else if (storedItem.type == ItemType.ChickenRaw)
            storedItem.Set(ItemType.ChickenCooked);

        UpdateStoredItemVisual();
        ShowCookingUI(false);
        isCooking = false;
    }

    private void UpdateCookingAnimation()
    {
        if (cookingUIImage == null || cookingSprites == null || cookingSprites.Length == 0)
            return;

        animationTimer += Time.deltaTime;

        float timeInLoop = animationTimer % animationDurationPerLoop;
        float normalizedTime = timeInLoop / animationDurationPerLoop;

        int frameIndex = Mathf.FloorToInt(normalizedTime * cookingSprites.Length);
        frameIndex = Mathf.Clamp(frameIndex, 0, cookingSprites.Length - 1);

        cookingUIImage.sprite = cookingSprites[frameIndex];

        cookingUIImage.color = Color.Lerp(
            Color.white,
            new Color(1f, 0.7f, 0.7f),
            normalizedTime
        );
    }

    private void ShowCookingUI(bool show)
    {
        if (cookingUIPanel != null)
            cookingUIPanel.SetActive(show);

        if (!show && cookingUIImage != null)
            cookingUIImage.color = Color.white;
    }
}