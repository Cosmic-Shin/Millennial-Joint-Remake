using UnityEngine;
using UnityEngine.UI;

public class GameModeSelector : MonoBehaviour
{
    public static GameModeSelector Instance { get; private set; }

    public GameObject modePanel;
    public Button timeModeBut;
    public Button speedModeBut;
    public Button versusModeBut;

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

        if (versusModeBut != null)
            versusModeBut.onClick.AddListener(() => SelectMode(OrderManager.GameMode.VERSUS));
    }

    public void SelectMode(OrderManager.GameMode mode)
    {
        if (modePanel != null)
            modePanel.SetActive(false);

        Time.timeScale = 1f;

        OrderManager.Instance?.SetGameMode(mode);
        
        // Make sure pause is fully reset
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ForceResume();
        }
        
        AudioManager.Instance?.PlayGameplayBGM();
    }

    public void ShowModeSelector()
    {
        if (modePanel != null)
            modePanel.SetActive(true);

        OrderUIManager.Instance?.HideStatus();
        OrderUIManager.Instance?.ClearOrderImages();
        VersusUIManager.Instance?.HideVersusUI();

        // DON'T call Pause() here - just show the selector
        // Time scale should already be 1 from Retry
    }
}