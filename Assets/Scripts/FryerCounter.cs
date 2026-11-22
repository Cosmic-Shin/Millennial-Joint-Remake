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

    [Tooltip("Anchor for the timer ring (its own sprite stays hidden)")]
    public Image cookingUIImage;

    private bool isCooking = false;
    private PlayerControl cookingPlayer;
    private float cookingTimer = 0f;
    private bool isOvercooked = false;
    private float overcookTimer = 0f;

    // What each raw item turns into in the fryer.
    private static readonly System.Collections.Generic.Dictionary<ItemType, ItemType> Fries =
        new System.Collections.Generic.Dictionary<ItemType, ItemType>
        {
            { ItemType.FrozenFries, ItemType.FriesCooked },
            { ItemType.ChickenRaw, ItemType.ChickenCooked },
            { ItemType.ShrimpRaw, ItemType.ShrimpCooked },
            { ItemType.DonutRaw, ItemType.DonutCooked },
        };

    public override bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (storedItem.IsEmpty)
            return Fries.ContainsKey(player.heldItem.type);

        return player.heldItem.IsEmpty;
    }

    public override void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (isCooking)
        {
            if (cookingTimer <= 0f)
            {
                FinishCooking();
                Show(player, storedItem.GetDisplayName() + " cooked!");
            }
            else
            {
                Show(player, $"Cooking... {cookingTimer:F1}s left");
            }

            return;
        }

        if (storedItem.IsEmpty)
        {
            if (!Fries.ContainsKey(player.heldItem.type))
            {
                Show(player, "Place fries, chicken, shrimp or donut dough in the fryer");
                return;
            }

            cookingPlayer = player;
            storedItem.Set(player.heldItem.type);
            player.heldItem.Clear();
            UpdateStoredItemVisual();
            StartCooking();
            Show(player, "Placed " + storedItem.GetDisplayName().ToLower() + " in fryer - cooking started");
            return;
        }

        if (player.heldItem.IsEmpty)
        {
            player.heldItem.Set(storedItem.type);
            storedItem.Clear();
            UpdateStoredItemVisual();
            isOvercooked = false;
            overcookTimer = 0f;
            ShowCookingUI(false);
            Show(player, "Picked up " + player.heldItem.GetDisplayName().ToLower());
            AudioManager.Instance?.PlayCounterTopInteractSFX();
        }
    }

    private void Update()
    {
        if (isCooking)
        {
            SetBurnSizzle(true, 0f);
            cookingTimer -= Time.deltaTime * CookRate(cookingPlayer);
            SetCookRing(cookingUIImage, 1f - cookingTimer / cookingTime, new Color(0.45f, 1f, 0.4f));

            if (cookingTimer <= 0f)
            {
                FinishCooking();
                isOvercooked = true;
                overcookTimer = overcookTime;
            }
        }
        else if (isOvercooked && BurnProof(cookingPlayer))
        {
            // Insta Cook: hold the burn timer full and keep the warning off until the boost ends.
            overcookTimer = overcookTime;
            ShowCookingUI(false);
            ThrobFood(storedItemVisualizer, 0f);
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
                    CameraMovement.Instance.Shake(0.3f, 0.35f);
                BurnPoof();
            }
        }
    }

    private void FlashBurnWarning()
    {
        ShowCookingUI(true);
        ThrobFood(storedItemVisualizer, 1f - overcookTimer / overcookTime);
        SetCookRing(cookingUIImage, overcookTimer / overcookTime,
            Color.Lerp(new Color(1f, 0.3f, 0.2f), Color.white, Mathf.PingPong(Time.time * 4f, 1f)));

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

        ShowCookingUI(true);
        SetSteam(true);
        AudioManager.Instance?.PlayStartFryingSFX();
    }

    private void FinishCooking()
    {
        if (Fries.TryGetValue(storedItem.type, out ItemType cooked))
            storedItem.Set(cooked);

        UpdateStoredItemVisual();
        ShowCookingUI(false);
        isCooking = false;
        SetSteam(false);
        Sparkle();
        Bounce(0.2f);
        AudioManager.Instance?.PlayFinishedFryingSFX();
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
    }
}