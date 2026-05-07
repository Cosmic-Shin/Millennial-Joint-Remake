using UnityEngine;

public class ControlSwapManager : MonoBehaviour
{
    public static ControlSwapManager Instance { get; private set; }

    [Header("Control Swap Settings")]
    [SerializeField] private float swapInterval = 30f;

    private PlayerControl player1Control;
    private PlayerControl player2Control;

    private KeyCode player1EmoteSelect;
    private KeyCode player2EmoteSelect;

    private float swapTimer = 0f;
    private bool hasSwapped = false;
    private bool isGamePlaying = false;

    private KeyCode player1MoveUp;
    private KeyCode player1MoveLeft;
    private KeyCode player1MoveDown;
    private KeyCode player1MoveRight;
    private KeyCode player1PrimaryAction;
    private KeyCode player1Run;
    private KeyCode player1DropItem;

    private KeyCode player2MoveUp;
    private KeyCode player2MoveLeft;
    private KeyCode player2MoveDown;
    private KeyCode player2MoveRight;
    private KeyCode player2PrimaryAction;
    private KeyCode player2Run;
    private KeyCode player2DropItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        FindPlayerControls();
        StoreOriginalKeyBindings();
    }

    private void FindPlayerControls()
    {
        PlayerControl[] allPlayers = FindObjectsOfType<PlayerControl>();

        foreach (PlayerControl player in allPlayers)
        {
            if (player.playerNumber == 1)
                player1Control = player;
            else if (player.playerNumber == 2)
                player2Control = player;
        }
    }

    private void StoreOriginalKeyBindings()
    {
        if (player1Control != null)
        {
            player1MoveUp = player1Control.MoveUp;
            player1MoveLeft = player1Control.MoveLeft;
            player1MoveDown = player1Control.MoveDown;
            player1MoveRight = player1Control.MoveRight;
            player1PrimaryAction = player1Control.PrimaryAction;
            player1Run = player1Control.Run;
            player1DropItem = player1Control.DropItem;
            player1EmoteSelect = player1Control.p1SelectKey;
        }

        if (player2Control != null)
        {
            player2MoveUp = player2Control.MoveUp;
            player2MoveLeft = player2Control.MoveLeft;
            player2MoveDown = player2Control.MoveDown;
            player2MoveRight = player2Control.MoveRight;
            player2PrimaryAction = player2Control.PrimaryAction;
            player2Run = player2Control.Run;
            player2DropItem = player2Control.DropItem;
            player2EmoteSelect = player2Control.p2SelectKey;
        }
    }

    private void Update()
    {
        if (OrderManager.Instance == null)
            return;

        if (OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS)
        {
            if (isGamePlaying)
            {
                isGamePlaying = false;
                swapTimer = 0f;
                hasSwapped = false;
                RestoreOriginalControls();
            }

            return;
        }

        if (OrderManager.Instance.state == OrderManager.GameState.Playing)
        {
            if (!isGamePlaying)
            {
                isGamePlaying = true;
                swapTimer = 0f;
                hasSwapped = false;
            }

            swapTimer += Time.deltaTime;

            int swapCount = (int)(swapTimer / swapInterval);

            if (swapCount > 0 && !hasSwapped)
            {
                hasSwapped = true;
                SwapControls();
                Debug.Log($"Controls swapped! Swap #{swapCount}");
            }

            if (swapTimer % swapInterval < Time.deltaTime)
                hasSwapped = false;
        }
        else
        {
            if (isGamePlaying)
            {
                isGamePlaying = false;
                swapTimer = 0f;
                hasSwapped = false;
                RestoreOriginalControls();
            }
        }
    }

    private void SwapControls()
    {
        if (player1Control == null || player2Control == null)
            return;

        (player1Control.MoveUp, player2Control.MoveUp) = (player2Control.MoveUp, player1Control.MoveUp);
        (player1Control.MoveLeft, player2Control.MoveLeft) = (player2Control.MoveLeft, player1Control.MoveLeft);
        (player1Control.MoveDown, player2Control.MoveDown) = (player2Control.MoveDown, player1Control.MoveDown);
        (player1Control.MoveRight, player2Control.MoveRight) = (player2Control.MoveRight, player1Control.MoveRight);
        (player1Control.PrimaryAction, player2Control.PrimaryAction) = (player2Control.PrimaryAction, player1Control.PrimaryAction);
        (player1Control.Run, player2Control.Run) = (player2Control.Run, player1Control.Run);
        (player1Control.DropItem, player2Control.DropItem) = (player2Control.DropItem, player1Control.DropItem);

        (player1Control.p1SelectKey, player2Control.p2SelectKey) =
            (player2Control.p2SelectKey, player1Control.p1SelectKey);
    }

    private void RestoreOriginalControls()
    {
        if (player1Control != null)
        {
            player1Control.MoveUp = player1MoveUp;
            player1Control.MoveLeft = player1MoveLeft;
            player1Control.MoveDown = player1MoveDown;
            player1Control.MoveRight = player1MoveRight;
            player1Control.PrimaryAction = player1PrimaryAction;
            player1Control.Run = player1Run;
            player1Control.DropItem = player1DropItem;
            player1Control.p1SelectKey = player1EmoteSelect;
        }

        if (player2Control != null)
        {
            player2Control.MoveUp = player2MoveUp;
            player2Control.MoveLeft = player2MoveLeft;
            player2Control.MoveDown = player2MoveDown;
            player2Control.MoveRight = player2MoveRight;
            player2Control.PrimaryAction = player2PrimaryAction;
            player2Control.Run = player2Run;
            player2Control.DropItem = player2DropItem;
            player2Control.p2SelectKey = player2EmoteSelect;
        }
    }
}