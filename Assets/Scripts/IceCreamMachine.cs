using UnityEngine;
using UnityEngine.UI;

public class IceCreamMachine : BaseStation, IInteractable
{
    [Header("Ice Cream Selection")]
    private string[] iceCreamSelectionLabels = new string[]
    {
        "Strawberry",
        "Bubblegum",
        "Mango"
    };

    private static readonly OrderItemType[] iceCreamOrders = { OrderItemType.StrawberryIceCream, OrderItemType.BubblegumIceCream, OrderItemType.MangoIceCream };

    [Header("Smart Suggestion")]
    public Color suggestionTextColor = new Color(0.05f, 0.5f, 0.1f);
    private int suggestedIceCreamIndex = -1;
    private Color normalTextColor = Color.black;

    [Header("Scooping")]
    public float scoopingTime = 3f;
    public GameObject scoopingUIPanel;

    [Tooltip("Anchor for the timer ring (its own sprite stays hidden)")]
    public Image scoopingUIImage;

    private PlayerControl currentPlayer;
    private bool isSelectingIceCream;
    private bool isScooping;
    private int selectedIceCreamIndex;

    private float scoopingTimer;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (currentPlayer != null && currentPlayer != player)
            return false;

        if ((isSelectingIceCream || isScooping) && currentPlayer == player)
            return true;

        return player.heldItem.IsEmpty && !isScooping;
    }

    private void Update()
    {
        if (!isScooping)
            return;

        scoopingTimer -= Time.deltaTime * CookRate(currentPlayer);
        SetCookRing(scoopingUIImage, 1f - scoopingTimer / scoopingTime, new Color(1f, 0.6f, 0.85f));

        if (scoopingTimer <= 0f)
        {
            FinishScooping();
        }
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        if (currentPlayer != null && currentPlayer != player) return;

        if (isScooping)
        {
            Show(player, $"Scooping ice cream... {scoopingTimer:F1}s left");
            return;
        }

        if (!isSelectingIceCream)
        {
            if (!player.heldItem.IsEmpty)
            {
                Show(player, "Hands must be empty to get ice cream");
                return;
            }

            if (!System.Array.Exists(iceCreamOrders, StoryMode.OnMenu))
            {
                Show(player, "Not on today's menu");
                player.PlayNope();
                return;
            }

            currentPlayer = player;
            currentPlayer.currentIceCreamMachine = this;
            StartIceCreamSelection();
        }
        else
        {
            StartScooping();
        }
    }

    private void StartIceCreamSelection()
    {
        isSelectingIceCream = true;

        QueuedOrder urgent = OrderManager.Instance != null
            ? OrderManager.Instance.MostUrgentOrder(currentPlayer, dish => System.Array.IndexOf(iceCreamOrders, dish) >= 0)
            : null;
        suggestedIceCreamIndex = urgent != null ? System.Array.IndexOf(iceCreamOrders, urgent.item.type) : -1;
        selectedIceCreamIndex = suggestedIceCreamIndex >= 0 ? suggestedIceCreamIndex : StoryMode.NextOnMenu(iceCreamOrders, -1, 1);

        if (currentPlayer.emoteSelectionText != null)
            normalTextColor = currentPlayer.emoteSelectionText.color;

        currentPlayer.doMove = false;
        currentPlayer.ResetMenuStep();

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);

        UpdateIceCreamSelectionText();
    }

    private void StartScooping()
    {
        isSelectingIceCream = false;
        isScooping = true;

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(false);

        if (currentPlayer.emoteSelectionText != null)
            currentPlayer.emoteSelectionText.text = string.Empty;

        scoopingTimer = scoopingTime;

        ShowScoopingUI(true);
        Show(currentPlayer, "Scooping ice cream...");
        AudioManager.Instance?.PlayStartDrinkCoffeeSFX();
    }

    private void FinishScooping()
    {
        if (currentPlayer != null)
        {
            switch (selectedIceCreamIndex)
            {
                case 0:
                    currentPlayer.heldItem.Set(ItemType.StrawberryIceCream);
                    Show(currentPlayer, "Got Strawberry Ice Cream!");
                    break;

                case 1:
                    currentPlayer.heldItem.Set(ItemType.BubblegumIceCream);
                    Show(currentPlayer, "Got Bubblegum Ice Cream!");
                    break;

                case 2:
                    currentPlayer.heldItem.Set(ItemType.MangoIceCream);
                    Show(currentPlayer, "Got Mango Ice Cream!");
                    break;
            }

            currentPlayer.RefreshHeldItemDisplay();
            AudioManager.Instance?.PlayFinishedDrinkCoffeeSFX();
            AudioManager.Instance?.PlayGetPlateAndCupSFX();
        }

        EndIceCreamMachineUse();
    }

    private void EndIceCreamMachineUse()
    {
        isSelectingIceCream = false;
        isScooping = false;

        ShowScoopingUI(false);

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentIceCreamMachine = null;

            if (currentPlayer.emoteSelectionObject != null)
                currentPlayer.emoteSelectionObject.SetActive(false);

            if (currentPlayer.emoteSelectionText != null)
            {
                currentPlayer.emoteSelectionText.text = string.Empty;
                currentPlayer.emoteSelectionText.color = normalTextColor;
            }
        }

        currentPlayer = null;
    }

    private void UpdateIceCreamSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        currentPlayer.emoteSelectionText.text = iceCreamSelectionLabels[selectedIceCreamIndex];
        currentPlayer.emoteSelectionText.color = selectedIceCreamIndex == suggestedIceCreamIndex ? suggestionTextColor : normalTextColor;
    }

    public void HandleIceCreamSelectionInput()
    {
        if (!isSelectingIceCream || currentPlayer == null)
            return;

        if (currentPlayer.IsDropItemPressed() ||
            Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndIceCreamMachineUse();
            return;
        }

        int step = currentPlayer.GetMenuStep();
        if (step != 0)
        {
            selectedIceCreamIndex = StoryMode.NextOnMenu(iceCreamOrders, selectedIceCreamIndex, step);
            UpdateIceCreamSelectionText();
        }
    }

    private void ShowScoopingUI(bool show)
    {
        if (scoopingUIPanel != null)
            scoopingUIPanel.SetActive(show);
    }

    private void LateUpdate()
    {
        if (!isSelectingIceCream || currentPlayer == null)
            return;

        if (currentPlayer.emoteSelectionObject == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        Transform textTransform = currentPlayer.emoteSelectionObject.transform;

        Vector3 direction = cam.transform.position - textTransform.position;
        textTransform.rotation = Quaternion.LookRotation(direction);
        textTransform.Rotate(0f, 180f, 0f);
    }
}