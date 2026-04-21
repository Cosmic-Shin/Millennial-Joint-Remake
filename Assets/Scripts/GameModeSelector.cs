using UnityEngine;
using UnityEngine.UI;

public class GameModeSelector : MonoBehaviour
{
    public static GameModeSelector Instance { get; private set; }

    public GameObject modePanel;
    public Button timeModeBut;
    public Button speedModeBut;

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
        if (timeModeBut != null)
            timeModeBut.onClick.AddListener(() => SelectMode(OrderManager.GameMode.TIME));

        if (speedModeBut != null)
            speedModeBut.onClick.AddListener(() => SelectMode(OrderManager.GameMode.SPEED));

        if (modePanel != null)
            modePanel.SetActive(true);

        // Start game paused
        PauseManager.Instance?.Pause();
    }

    public void SelectMode(OrderManager.GameMode mode)
    {
        if (modePanel != null)
            modePanel.SetActive(false);

        OrderManager.Instance?.SetGameMode(mode);
        PauseManager.Instance?.Resume();
    }

    public void ShowModeSelector()
    {
        if (modePanel != null)
            modePanel.SetActive(true);

        PauseManager.Instance?.Pause();
    }
}
