using UnityEngine;
using UnityEngine.UI;

public class StoveCounter : BaseStation, IInteractable
{
    public KitchenItemData storedItem = new KitchenItemData();
    public KitchenItemVisualizer storedItemVisualizer;

    [Header("Cooking")]
    public float cookingTime = 5f;
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

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        // 🔥 Accept BOTH Patty and Hotdog
        if (storedItem.IsEmpty)
            return player.heldItem.type == ItemType.PattyRaw ||
                   player.heldItem.type == ItemType.HotDogRaw;

        if (storedItem.type == ItemType.PattyRaw ||
            storedItem.type == ItemType.HotDogRaw ||
            isCooking)
            return player.heldItem.IsEmpty;

        if (storedItem.type == ItemType.PattyCooked ||
            storedItem.type == ItemType.HotDogCooked)
            return player.heldItem.IsEmpty;

        return false;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        // 🔥 If cooking → check status
        if (isCooking)
        {
            if (cookingTimer <= 0f)
            {
                FinishCooking();
                Show(player, "Done!");
            }
            else
            {
                Show(player, $"Cooking... {cookingTimer:F1}s left");
            }
            return;
        }

        // 🔥 Place item (Patty OR Hotdog)
        if (storedItem.IsEmpty)
        {
            if (player.heldItem.type == ItemType.PattyRaw ||
                player.heldItem.type == ItemType.HotDogRaw)
            {
                storedItem.Set(player.heldItem.type);
                player.heldItem.Clear();

                UpdateStoredItemVisual();
                StartCooking();

                Show(player, "Cooking started");
                return;
            }

            Show(player, "Place raw patty or raw hotdog");
            return;
        }

        // 🔥 Pick up cooked item
        if (storedItem.type == ItemType.PattyCooked ||
            storedItem.type == ItemType.HotDogCooked)
        {
            player.heldItem.Set(storedItem.type);
            storedItem.Clear();

            UpdateStoredItemVisual();

            isOvercooked = false;
            overcookTimer = 0f;

            Show(player, "Picked up cooked item");
            return;
        }

        Show(player, "Cannot use stove right now");
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

                Debug.Log("StoveCounter: Item overcooked and destroyed!");
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
        // 🔥 Convert correctly depending on item
        if (storedItem.type == ItemType.PattyRaw)
            storedItem.Set(ItemType.PattyCooked);
        else if (storedItem.type == ItemType.HotDogRaw)
            storedItem.Set(ItemType.HotDogCooked);

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

        // heat effect
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

    private void UpdateStoredItemVisual()
    {
        if (storedItemVisualizer != null)
            storedItemVisualizer.Refresh(storedItem);
    }
}