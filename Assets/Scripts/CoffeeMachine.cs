using UnityEngine;
using UnityEngine.UI;

public class CoffeeMachine : BaseStation, IInteractable
{
    [Header("Pouring")]
    public float pouringTime = 3f;
    public GameObject pouringUIPanel;

    [Header("Pouring Animation")]
    public Image pouringUIImage;
    public Sprite[] pouringSprites;
    public int animationLoops = 3;

    private bool isPouring = false;
    private float pouringTimer = 0f;
    private PlayerControl currentPlayer;

    private float animationTimer = 0f;
    private float animationDurationPerLoop;

    private void Update()
    {
        if (isPouring)
        {
            pouringTimer -= Time.deltaTime;
            UpdatePouringAnimation();

            if (pouringTimer <= 0f)
            {
                if (currentPlayer != null)
                {
                    currentPlayer.heldItem.Set(ItemType.Cup);
                    currentPlayer.heldItem.cupHasCoffee = true;
                    currentPlayer.RefreshHeldItemDisplay();
                    currentPlayer.doMove = true;
                    Show(currentPlayer, "Grabbed coffee!");
                }

                ShowPouringUI(false);
                isPouring = false;
                currentPlayer = null;
            }
        }
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty && !isPouring;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (isPouring)
        {
            Show(player, $"Pouring coffee... {pouringTimer:F1}s left");
            return;
        }

        if (!player.heldItem.IsEmpty)
        {
            Show(player, "Hands must be empty to grab coffee");
            return;
        }

        currentPlayer = player;
        currentPlayer.doMove = false;

        isPouring = true;
        pouringTimer = pouringTime;

        animationTimer = 0f;
        animationDurationPerLoop = pouringTime / Mathf.Max(1, animationLoops);

        ShowPouringUI(true);
        Show(player, "Pouring coffee...");
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
}