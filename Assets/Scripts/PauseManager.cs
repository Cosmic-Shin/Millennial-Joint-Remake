using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public GameObject pausePanel;
    public Button closeButton;

    private bool isPaused = false;

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
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Resume);
    }

    private void Update()
    {
        // Only allow ESC pause/resume during actual gameplay
        if (OrderManager.Instance != null && OrderManager.Instance.state == OrderManager.GameState.Waiting)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Debug.Log("Game Resumed");
    }

    public bool IsPaused() => isPaused;
}
