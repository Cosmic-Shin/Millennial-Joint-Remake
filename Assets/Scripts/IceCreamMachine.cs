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

    [Header("Scooping")]
    public float scoopingTime = 3f;
    public GameObject scoopingUIPanel;

    [Header("Scooping Animation")]
    public Image scoopingUIImage;
    public Sprite[] scoopingSprites;
    public int animationLoops = 3;

    private PlayerControl currentPlayer;
    private bool isSelectingIceCream;
    private bool isScooping;
    private int selectedIceCreamIndex;

    private float scoopingTimer;
    private float animationTimer;
    private float animationDurationPerLoop;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if ((isSelectingIceCream || isScooping) && currentPlayer == player)
            return true;

        // Player just needs empty hands - no cup required
        return player.heldItem.IsEmpty && !isScooping;
    }

    private void Update()
    {
        if (!isScooping)
            return;

        float cookingSpeedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.GetCookingTimeMultiplier() : 1f;
        scoopingTimer -= Time.deltaTime * (1f / cookingSpeedMultiplier);
        UpdateScoopingAnimation();

        if (scoopingTimer <= 0f)
        {
            FinishScooping();
        }
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

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
        selectedIceCreamIndex = 0;

        currentPlayer.doMove = false;

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

        float cookingSpeedMultiplier = PowerUpManager.Instance != null ? PowerUpManager.Instance.GetCookingTimeMultiplier() : 1f;
        scoopingTimer = scoopingTime * cookingSpeedMultiplier;
        animationTimer = 0f;
        animationDurationPerLoop = scoopingTime / Mathf.Max(1, animationLoops);

        ShowScoopingUI(true);
        Show(currentPlayer, "Scooping ice cream...");
        AudioManager.Instance?.PlayStartDrinkCoffeeSFX();
    }

    private void FinishScooping()
    {
        if (currentPlayer != null)
        {
            // Create ice cream item directly - no cup needed
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
                currentPlayer.emoteSelectionText.text = string.Empty;
        }

        currentPlayer = null;
    }

    private void UpdateIceCreamSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        currentPlayer.emoteSelectionText.text = iceCreamSelectionLabels[selectedIceCreamIndex];
    }

    public void HandleIceCreamSelectionInput()
    {
        if (!isSelectingIceCream || currentPlayer == null)
            return;

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndIceCreamMachineUse();
            return;
        }

        if (Input.GetKeyDown(currentPlayer.MoveLeft))
        {
            selectedIceCreamIndex = (selectedIceCreamIndex - 1 + iceCreamSelectionLabels.Length) % iceCreamSelectionLabels.Length;
            UpdateIceCreamSelectionText();
        }
        else if (Input.GetKeyDown(currentPlayer.MoveRight))
        {
            selectedIceCreamIndex = (selectedIceCreamIndex + 1) % iceCreamSelectionLabels.Length;
            UpdateIceCreamSelectionText();
        }
    }

    private void UpdateScoopingAnimation()
    {
        if (scoopingUIImage == null || scoopingSprites == null || scoopingSprites.Length == 0)
            return;

        animationTimer += Time.deltaTime;

        float timeInLoop = animationTimer % animationDurationPerLoop;
        float normalizedTime = timeInLoop / animationDurationPerLoop;

        int frameIndex = Mathf.FloorToInt(normalizedTime * scoopingSprites.Length);
        frameIndex = Mathf.Clamp(frameIndex, 0, scoopingSprites.Length - 1);

        scoopingUIImage.sprite = scoopingSprites[frameIndex];
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