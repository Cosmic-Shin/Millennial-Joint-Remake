using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject howToPlayPanel;

    [Header("Pause Panel Buttons")]
    public Button closeButton;
    public Button howToPlayButton;
    public Button scoreButton;
    public Button retryButton;
    public Button quitButton;

    [Header("Sub Panel Buttons")]
    public Button howToPlayCloseButton;

    [Header("Volume Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;

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
        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Resume);

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OpenHowToPlay);

        if (scoreButton != null)
            scoreButton.onClick.AddListener(OpenScore);

        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        if (howToPlayCloseButton != null)
            howToPlayCloseButton.onClick.AddListener(BackToPausePanel);

        SetupVolumeSliders();
    }

    private void Update()
    {
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

        SetPanel(pausePanel, true);
        SetPanel(howToPlayPanel, false);

        if (ScoreManager.Instance != null && ScoreManager.Instance.scorePanel != null)
            ScoreManager.Instance.scorePanel.SetActive(false);

        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);

        if (ScoreManager.Instance != null && ScoreManager.Instance.scorePanel != null)
            ScoreManager.Instance.scorePanel.SetActive(false);

        Debug.Log("Game Resumed");
    }

    public void OpenHowToPlay()
    {
        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, true);

        if (ScoreManager.Instance != null && ScoreManager.Instance.scorePanel != null)
            ScoreManager.Instance.scorePanel.SetActive(false);
    }

    public void OpenScore()
    {
        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OpenScorePanel();
    }

    public void BackToPausePanel()
    {
        SetPanel(pausePanel, true);
        SetPanel(howToPlayPanel, false);

        if (ScoreManager.Instance != null && ScoreManager.Instance.scorePanel != null)
            ScoreManager.Instance.scorePanel.SetActive(false);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
        Debug.Log("Quit Game");

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetupVolumeSliders()
    {
        if (AudioManager.Instance == null)
            return;

        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance.GetMusicVolume();
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance.GetSFXVolume();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    public void SetBGMVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}