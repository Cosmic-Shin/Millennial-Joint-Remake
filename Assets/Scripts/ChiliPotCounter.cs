using UnityEngine;
using UnityEngine.UI;

public class ChiliPotCounter : BaseStation, IInteractable
{
    [Header("Chili Applying")]
    public float chiliTime = 2f;
    public GameObject chiliUIPanel;

    [Header("Chili Animation")]
    public Image chiliUIImage;
    public Sprite[] chiliSprites;
    public int animationLoops = 3;

    private PlayerControl currentPlayer;
    private bool isApplyingChili;

    private float chiliTimer;
    private float animationTimer;
    private float animationDurationPerLoop;

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;

        if (isApplyingChili && currentPlayer == player)
            return true;

        return player.heldItem.IsPlate
               && player.heldItem.plateHasDogBun
               && player.heldItem.plateHasHotdog
               && !player.heldItem.plateHasChili;
    }

    private void Update()
    {
        if (!isApplyingChili)
            return;

        chiliTimer -= Time.deltaTime;
        UpdateChiliAnimation();

        if (chiliTimer <= 0f)
            FinishApplyingChili();
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;

        if (isApplyingChili)
        {
            Show(player, $"Adding chili... {chiliTimer:F1}s left");
            return;
        }

        if (!player.heldItem.IsPlate)
        {
            Show(player, "Hold a plate first");
            return;
        }

        if (!player.heldItem.plateHasDogBun || !player.heldItem.plateHasHotdog)
        {
            Show(player, "Need plate with dog bun and hotdog");
            return;
        }

        if (player.heldItem.plateHasChili)
        {
            Show(player, "Chili already added");
            return;
        }

        currentPlayer = player;

        // IMPORTANT: requires PlayerControl currentChiliPot variable
        currentPlayer.currentChiliPot = this;

        StartApplyingChili();
    }

    private void StartApplyingChili()
    {
        isApplyingChili = true;

        currentPlayer.doMove = false;

        chiliTimer = chiliTime;
        animationTimer = 0f;
        animationDurationPerLoop = chiliTime / Mathf.Max(1, animationLoops);

        ShowChiliUI(true);
        Show(currentPlayer, "Adding chili...");
    }

    private void FinishApplyingChili()
    {
        if (currentPlayer != null)
        {
            currentPlayer.heldItem.plateHasChili = true;
            currentPlayer.RefreshHeldItemDisplay();

            Show(currentPlayer, "Chili Dog complete!");
        }

        EndChiliUse();
    }

    private void EndChiliUse()
    {
        isApplyingChili = false;

        ShowChiliUI(false);

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentChiliPot = null;
        }

        currentPlayer = null;
    }

    private void UpdateChiliAnimation()
    {
        if (chiliUIImage == null || chiliSprites == null || chiliSprites.Length == 0)
            return;

        animationTimer += Time.deltaTime;

        float timeInLoop = animationTimer % animationDurationPerLoop;
        float normalizedTime = timeInLoop / animationDurationPerLoop;

        int frameIndex = Mathf.FloorToInt(normalizedTime * chiliSprites.Length);
        frameIndex = Mathf.Clamp(frameIndex, 0, chiliSprites.Length - 1);

        chiliUIImage.sprite = chiliSprites[frameIndex];
    }

    private void ShowChiliUI(bool show)
    {
        if (chiliUIPanel != null)
            chiliUIPanel.SetActive(show);
    }
}