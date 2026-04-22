using UnityEngine;

/// <summary>
/// Handles player character animations based on movement and item state.
/// Transitions between Idle, Run, and CarryItem animations.
/// </summary>
public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private PlayerControl playerControl;

    // Animation parameter hashes (more efficient than string names)
    private int hashIsRunning = Animator.StringToHash("IsRunning");

    private float lastDirX;
    private float lastDirZ;
    private bool lastIsRunning = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerControl = GetComponent<PlayerControl>();

        if (animator == null)
            Debug.LogError("AnimationController: No Animator found on this GameObject!");

        if (playerControl == null)
            Debug.LogError("AnimationController: No PlayerControl found on this GameObject!");
    }

    private void Update()
    {
        if (animator == null || playerControl == null)
            return;

        // Determine movement direction
        bool isMoving = false;
        float dirX = 0f;
        float dirZ = 0f;

        // Check input based on player number (same keys used by PlayerControl)
        bool moveUp = Input.GetKey(playerControl.MoveUp);
        bool moveDown = Input.GetKey(playerControl.MoveDown);
        bool moveLeft = Input.GetKey(playerControl.MoveLeft);
        bool moveRight = Input.GetKey(playerControl.MoveRight);

        if (moveUp || moveDown || moveLeft || moveRight)
        {
            isMoving = true;
            dirX = (moveRight ? 1 : 0) - (moveLeft ? 1 : 0);
            dirZ = (moveUp ? 1 : 0) - (moveDown ? 1 : 0);

            if (dirX != 0 || dirZ != 0)
            {
                lastDirX = dirX;
                lastDirZ = dirZ;
            }
        }

        // Check if running
        bool isRunning = isMoving && Input.GetKey(playerControl.Run);

        // Update animator parameters only if they changed (optimization)
        if (isRunning != lastIsRunning)
        {
            animator.SetBool(hashIsRunning, isRunning);
            lastIsRunning = isRunning;
        }
    }
}
