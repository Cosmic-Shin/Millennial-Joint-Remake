using UnityEngine;

public class ControlSwapManager : MonoBehaviour
{
    public static ControlSwapManager Instance { get; private set; }

    [Header("Control Swap Settings")]
    [SerializeField] private float swapInterval = 30f;

    private PlayerControl player1Control;
    private PlayerControl player2Control;

    private float swapTimer = 0f;
    private bool hasSwapped = false;
    private bool isGamePlaying = false;

    public bool IsSwapped { get; private set; }

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
    }

    private void FindPlayerControls()
    {
        PlayerControl[] allPlayers = FindObjectsByType<PlayerControl>(FindObjectsSortMode.None);

        foreach (PlayerControl player in allPlayers)
        {
            if (player.playerNumber == 1)
                player1Control = player;
            else if (player.playerNumber == 2)
                player2Control = player;
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

        if (OrderManager.Instance.IsRunning)
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

        (player1Control.Bindings, player2Control.Bindings) = (player2Control.Bindings, player1Control.Bindings);
        IsSwapped = !IsSwapped;
        KeybindManager.Instance?.RefreshAllKeybindDisplay();
    }

    public void RestoreOriginalControls()
    {
        if (IsSwapped)
            SwapControls();
    }
}
