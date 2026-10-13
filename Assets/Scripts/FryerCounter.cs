using UnityEngine;
using UnityEngine.UI;

public class FryerCounter : StorageStation
{
    [Header("Cooking")]
    public float cookingTime = 7f;
    public float overcookTime = 10f;
    public GameObject cookingUIPanel;
    public Transform cookingAnchor;

    [Tooltip("Smoke that thickens while cooked food is about to burn")]
    public ParticleSystem burnSmoke;
    [Tooltip("Flickering light switched on while cooking")]
    public GameObject fireGlow;

    [Header("Cooking Animation")]
    public Image cookingUIImage;
    public Sprite[] cookingSprites;
    public int animationLoops = 3;

    private bool isCooking = false;
    private PlayerControl cookingPlayer;
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
            cookingPlayer = player;

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
            ShowCookingUI(false);
            Show(player, "Picked up cooked fries");
            AudioManager.Instance?.PlayCounterTopInteractSFX();
            return;
        }

        if (storedItem.type == ItemType.ChickenCooked)
        {
            player.heldItem.Set(ItemType.ChickenCooked);
            storedItem.Clear();
            UpdateStoredItemVisual();
            isOvercooked = false;
            overcookTimer = 0f;
            ShowCookingUI(false);
            Show(player, "Picked up cooked chicken");
            AudioManager.Instance?.PlayCounterTopInteractSFX();
            return;
        }
    }

    private void Update()
    {
        if (isCooking)
        {
            SetBurnSizzle(true, 0f);
            cookingTimer -= Time.deltaTime * CookRate(cookingPlayer);
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
            FlashBurnWarning();

            if (overcookTimer <= 0f)
            {
                storedItem.Clear();
                UpdateStoredItemVisual();
                isOvercooked = false;
                ShowCookingUI(false);

                if (CameraMovement.Instance != null)
                    CameraMovement.Instance.Shake(0.2f, 0.3f);
            }
        }
    }

    private void FlashBurnWarning()
    {
        ShowCookingUI(true);

        if (cookingUIImage != null)
            cookingUIImage.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 4f, 1f));

        if (burnSmoke != null)
        {
            if (!burnSmoke.isPlaying)
                burnSmoke.Play();

            var emission = burnSmoke.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(4f, 30f, 1f - overcookTimer / overcookTime);
        }

        SetBurnSizzle(true, 1f - overcookTimer / overcookTime);
    }

    private void StartCooking()
    {
        isCooking = true;
        cookingTimer = cookingTime;

        animationTimer = 0f;
        animationDurationPerLoop = cookingTime / Mathf.Max(1, animationLoops);

        ShowCookingUI(true);
        SetSteam(true);
        AudioManager.Instance?.PlayStartFryingSFX();
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
        SetSteam(false);
        Sparkle();
        AudioManager.Instance?.PlayFinishedFryingSFX();
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
        if (fireGlow != null)
            fireGlow.SetActive(show);

        if (cookingUIPanel != null)
            cookingUIPanel.SetActive(show);

        if (!show && burnSmoke != null)
            burnSmoke.Stop();

        if (!show)
            SetBurnSizzle(false);

        if (!show && cookingUIImage != null)
            cookingUIImage.color = Color.white;
    }
}