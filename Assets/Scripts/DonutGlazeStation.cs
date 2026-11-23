using UnityEngine;
using UnityEngine.UI;

// Bring a plate with a fried donut, pick a glaze, wait while it's glazed.
// The flavour the most urgent donut order wants is preselected and shown in green.
public class DonutGlazeStation : BaseStation, IInteractable
{
    private static readonly DonutGlaze[] Glazes = { DonutGlaze.Chocolate, DonutGlaze.Strawberry, DonutGlaze.Galaxy };
    private static readonly OrderItemType[] GlazeOrders = { OrderItemType.ChocolateDonut, OrderItemType.StrawberryDonut, OrderItemType.GalaxyDonut };

    [Header("Smart Suggestion")]
    public Color suggestionTextColor = new Color(0.05f, 0.5f, 0.1f);

    [Header("Glazing")]
    public float glazingTime = 2.5f;
    public GameObject glazingUIPanel;
    public Image glazingUIImage;

    private PlayerControl currentPlayer;
    private bool isSelecting;
    private bool isGlazing;
    private int selectedIndex;
    private int suggestedIndex = -1;
    private Color normalTextColor = Color.black;
    private float glazingTimer;

    private static bool HasPlainDonut(KitchenItemData item) =>
        item.IsPlate && item.plateHasDonut && item.donutGlaze == DonutGlaze.None;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (currentPlayer != null)
            return currentPlayer == player;

        return HasPlainDonut(player.heldItem);
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        if (currentPlayer != null && currentPlayer != player) return;

        if (isGlazing)
        {
            Show(player, $"Glazing... {glazingTimer:F1}s left");
            return;
        }

        if (isSelecting)
        {
            StartGlazing();
            return;
        }

        if (!HasPlainDonut(player.heldItem))
        {
            Show(player, player.heldItem.IsPlate && player.heldItem.plateHasDonut
                ? "Already glazed"
                : "Bring a plate with a fried donut");
            return;
        }

        currentPlayer = player;
        currentPlayer.currentGlazeStation = this;
        StartSelection();
    }

    private void StartSelection()
    {
        isSelecting = true;

        QueuedOrder urgent = OrderManager.Instance != null
            ? OrderManager.Instance.MostUrgentOrder(currentPlayer, dish => System.Array.IndexOf(GlazeOrders, dish) >= 0)
            : null;
        suggestedIndex = urgent != null ? System.Array.IndexOf(GlazeOrders, urgent.item.type) : -1;
        selectedIndex = suggestedIndex >= 0 ? suggestedIndex : StoryMode.NextOnMenu(GlazeOrders, -1, 1);

        if (currentPlayer.emoteSelectionText != null)
            normalTextColor = currentPlayer.emoteSelectionText.color;

        currentPlayer.doMove = false;
        currentPlayer.ResetMenuStep();

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);

        UpdateSelectionText();
    }

    public void HandleGlazeSelectionInput()
    {
        if (!isSelecting || currentPlayer == null)
            return;

        if (currentPlayer.IsDropItemPressed() ||
            Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndUse();
            return;
        }

        int step = currentPlayer.GetMenuStep();
        if (step != 0)
        {
            selectedIndex = StoryMode.NextOnMenu(GlazeOrders, selectedIndex, step);
            UpdateSelectionText();
        }
    }

    private void UpdateSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        currentPlayer.emoteSelectionText.text = "< " + Glazes[selectedIndex] + " >";
        currentPlayer.emoteSelectionText.color = selectedIndex == suggestedIndex ? suggestionTextColor : normalTextColor;
    }

    private void StartGlazing()
    {
        isSelecting = false;
        isGlazing = true;
        HideSelection();

        glazingTimer = glazingTime;
        ShowGlazingUI(true);
        Show(currentPlayer, "Glazing " + Glazes[selectedIndex].ToString().ToLower() + "...");
        AudioManager.Instance?.PlayStartChiliSFX();
    }

    private void Update()
    {
        if (!isGlazing)
            return;

        glazingTimer -= Time.deltaTime * CookRate(currentPlayer);
        SetCookRing(glazingUIImage, 1f - glazingTimer / glazingTime, new Color(1f, 0.55f, 0.85f));

        if (glazingTimer <= 0f)
            FinishGlazing();
    }

    private void FinishGlazing()
    {
        if (currentPlayer != null && HasPlainDonut(currentPlayer.heldItem))
        {
            currentPlayer.heldItem.donutGlaze = Glazes[selectedIndex];
            currentPlayer.RefreshHeldItemDisplay();
            Show(currentPlayer, Glazes[selectedIndex] + " Donut complete!");
            Sparkle();
            AudioManager.Instance?.PlayFinishedChiliSFX();
            AudioManager.Instance?.PlayCounterTopInteractSFX();
        }

        EndUse();
    }

    private void EndUse()
    {
        isSelecting = false;
        isGlazing = false;
        ShowGlazingUI(false);
        HideSelection();

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentGlazeStation = null;
        }

        currentPlayer = null;
    }

    private void HideSelection()
    {
        if (currentPlayer == null)
            return;

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(false);

        if (currentPlayer.emoteSelectionText != null)
        {
            currentPlayer.emoteSelectionText.text = string.Empty;
            currentPlayer.emoteSelectionText.color = normalTextColor;
        }
    }

    private void ShowGlazingUI(bool show)
    {
        if (glazingUIPanel != null)
            glazingUIPanel.SetActive(show);
    }

    private void LateUpdate()
    {
        if (!isSelecting || currentPlayer == null || currentPlayer.emoteSelectionObject == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform textTransform = currentPlayer.emoteSelectionObject.transform;
        textTransform.rotation = Quaternion.LookRotation(cam.transform.position - textTransform.position);
        textTransform.Rotate(0f, 180f, 0f);
    }
}
