using UnityEngine;
using UnityEngine.UI;

public class DrinkMachine : BaseStation, IInteractable
{
    [Header("Drink Selection")]
    private string[] drinkSelectionLabels = new string[]
    {
        "Soda",
        "Ice Tea",
        "Orange Juice"
    };

    [Header("Pouring")]
    public float pouringTime = 3f;
    public GameObject pouringUIPanel;

    [Header("Pouring Animation")]
    public Image pouringUIImage;
    public Sprite[] pouringSprites;
    public int animationLoops = 3;

    private PlayerControl currentPlayer;
    private bool isSelectingDrink;
    private bool isPouring;
    private int selectedDrinkIndex;

    private float pouringTimer;
    private float animationTimer;
    private float animationDurationPerLoop;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if ((isSelectingDrink || isPouring) && currentPlayer == player)
            return true;

        return player.heldItem.IsCup
               && !player.heldItem.cupHasSoda
               && !player.heldItem.cupHasIceTea
               && !player.heldItem.cupHasOrangeJuice;
    }

    private void Update()
    {
        if (!isPouring)
            return;

        pouringTimer -= Time.deltaTime;
        UpdatePouringAnimation();

        if (pouringTimer <= 0f)
        {
            FinishPouring();
        }
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (isPouring)
        {
            Show(player, $"Pouring drink... {pouringTimer:F1}s left");
            return;
        }

        if (!isSelectingDrink)
        {
            if (!player.heldItem.IsCup)
            {
                Show(player, "Hold an empty cup first");
                return;
            }

            if (player.heldItem.cupHasSoda || player.heldItem.cupHasIceTea || player.heldItem.cupHasOrangeJuice)
            {
                Show(player, "Cup is already filled");
                return;
            }

            currentPlayer = player;
            currentPlayer.currentDrinkMachine = this;
            StartDrinkSelection();
        }
        else
        {
            StartPouring();
        }
    }

    private void StartDrinkSelection()
    {
        isSelectingDrink = true;
        selectedDrinkIndex = 0;

        currentPlayer.doMove = false;

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(true);

        UpdateDrinkSelectionText();
    }

    private void StartPouring()
    {
        isSelectingDrink = false;
        isPouring = true;

        if (currentPlayer.emoteSelectionObject != null)
            currentPlayer.emoteSelectionObject.SetActive(false);

        if (currentPlayer.emoteSelectionText != null)
            currentPlayer.emoteSelectionText.text = string.Empty;

        pouringTimer = pouringTime;
        animationTimer = 0f;
        animationDurationPerLoop = pouringTime / Mathf.Max(1, animationLoops);

        ShowPouringUI(true);
        Show(currentPlayer, "Pouring drink...");
    }

    private void FinishPouring()
    {
        if (currentPlayer != null)
        {
            currentPlayer.heldItem.cupHasSoda = false;
            currentPlayer.heldItem.cupHasIceTea = false;
            currentPlayer.heldItem.cupHasOrangeJuice = false;

            switch (selectedDrinkIndex)
            {
                case 0:
                    currentPlayer.heldItem.cupHasSoda = true;
                    Show(currentPlayer, "Filled cup with Soda");
                    break;

                case 1:
                    currentPlayer.heldItem.cupHasIceTea = true;
                    Show(currentPlayer, "Filled cup with Ice Tea");
                    break;

                case 2:
                    currentPlayer.heldItem.cupHasOrangeJuice = true;
                    Show(currentPlayer, "Filled cup with Orange Juice");
                    break;
            }

            currentPlayer.RefreshHeldItemDisplay();
        }

        EndDrinkMachineUse();
    }

    private void EndDrinkMachineUse()
    {
        isSelectingDrink = false;
        isPouring = false;

        ShowPouringUI(false);

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentDrinkMachine = null;

            if (currentPlayer.emoteSelectionObject != null)
                currentPlayer.emoteSelectionObject.SetActive(false);

            if (currentPlayer.emoteSelectionText != null)
                currentPlayer.emoteSelectionText.text = string.Empty;
        }

        currentPlayer = null;
    }

    private void UpdateDrinkSelectionText()
    {
        if (currentPlayer == null || currentPlayer.emoteSelectionText == null)
            return;

        currentPlayer.emoteSelectionText.text = drinkSelectionLabels[selectedDrinkIndex];
    }

    public void HandleDrinkSelectionInput()
    {
        if (!isSelectingDrink || currentPlayer == null)
            return;

        if (Vector3.Distance(transform.position, currentPlayer.transform.position) > 3f)
        {
            EndDrinkMachineUse();
            return;
        }

        if (Input.GetKeyDown(currentPlayer.MoveLeft))
        {
            selectedDrinkIndex = (selectedDrinkIndex - 1 + drinkSelectionLabels.Length) % drinkSelectionLabels.Length;
            UpdateDrinkSelectionText();
        }
        else if (Input.GetKeyDown(currentPlayer.MoveRight))
        {
            selectedDrinkIndex = (selectedDrinkIndex + 1) % drinkSelectionLabels.Length;
            UpdateDrinkSelectionText();
        }
    }

    private void UpdatePouringAnimation()
    {
        if (pouringUIImage == null || pouringSprites == null || pouringSprites.Length == 0)
            return;

        animationTimer += Time.deltaTime;

        float timeInLoop = animationTimer % animationDurationPerLoop;
        float normalizedTime = timeInLoop / animationDurationPerLoop;

        int frameIndex = Mathf.FloorToInt(normalizedTime * pouringSprites.Length);
        frameIndex = Mathf.Clamp(frameIndex, 0, pouringSprites.Length - 1);

        pouringUIImage.sprite = pouringSprites[frameIndex];
    }

    private void ShowPouringUI(bool show)
    {
        if (pouringUIPanel != null)
            pouringUIPanel.SetActive(show);
    }

    private void LateUpdate()
    {
        if (!isSelectingDrink || currentPlayer == null)
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