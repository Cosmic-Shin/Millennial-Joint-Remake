using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;
    private PlayerControl playerControl;

    private int hashIsRunning = Animator.StringToHash("IsRunning");

    private bool lastIsRunning = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        playerControl = GetComponent<PlayerControl>();
        if (playerControl == null)
            playerControl = GetComponentInParent<PlayerControl>();

        if (animator == null)
            Debug.LogError($"AnimationController: No Animator found on '{gameObject.name}' or its children/parents!");

        if (playerControl == null)
            Debug.LogError($"AnimationController: No PlayerControl found on '{gameObject.name}' or its parents!");

        if (animator == null || playerControl == null)
        {
            enabled = false;
            return;
        }

        if (!System.Linq.Enumerable.Any(animator.parameters, p => p.nameHash == hashIsRunning))
            enabled = false;
    }

    private void Update()
    {
        if (animator == null || playerControl == null)
            return;

        bool isMoving = false;

        bool canMove = playerControl.CanMove;
        bool moveUp = canMove && playerControl.IsMoveUpHeld();
        bool moveDown = canMove && playerControl.IsMoveDownHeld();
        bool moveLeft = canMove && playerControl.IsMoveLeftHeld();
        bool moveRight = canMove && playerControl.IsMoveRightHeld();

        if (moveUp || moveDown || moveLeft || moveRight)
            isMoving = true;

        bool isRunning = isMoving && playerControl.IsRunHeld();

        if (isRunning != lastIsRunning)
        {
            animator.SetBool(hashIsRunning, isRunning);
            lastIsRunning = isRunning;
        }
    }
}
