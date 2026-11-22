using UnityEngine;
using UnityEngine.UI;

public class ChiliPotCounter : BaseStation, IInteractable
{
    [Header("Chili Applying")]
    public float chiliTime = 2f;
    public GameObject chiliUIPanel;

    [Tooltip("Anchor for the timer ring (its own sprite stays hidden)")]
    public Image chiliUIImage;

    private PlayerControl currentPlayer;
    private bool isApplyingChili;

    private float chiliTimer;

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

        chiliTimer -= Time.deltaTime * CookRate(currentPlayer);
        SetCookRing(chiliUIImage, 1f - chiliTimer / chiliTime, new Color(1f, 0.45f, 0.25f));

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

        currentPlayer.currentChiliPot = this;

        StartApplyingChili();
    }

    private void StartApplyingChili()
    {
        isApplyingChili = true;

        currentPlayer.doMove = false;

        chiliTimer = chiliTime;

        ShowChiliUI(true);
        SetSteam(true);
        Show(currentPlayer, "Adding chili...");
        AudioManager.Instance?.PlayStartChiliSFX();
    }

    private void FinishApplyingChili()
    {
        if (currentPlayer != null)
        {
            currentPlayer.heldItem.plateHasChili = true;
            currentPlayer.RefreshHeldItemDisplay();

            Show(currentPlayer, "Chili Dog complete!");
            Sparkle();
            AudioManager.Instance?.PlayFinishedChiliSFX();
            AudioManager.Instance?.PlayCounterTopInteractSFX();
        }

        EndChiliUse();
    }

    private void EndChiliUse()
    {
        isApplyingChili = false;

        ShowChiliUI(false);
        SetSteam(false);

        if (currentPlayer != null)
        {
            currentPlayer.doMove = true;
            currentPlayer.currentChiliPot = null;
        }

        currentPlayer = null;
    }

    private void ShowChiliUI(bool show)
    {
        if (chiliUIPanel != null)
            chiliUIPanel.SetActive(show);
    }
}