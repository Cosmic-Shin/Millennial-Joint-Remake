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
    public Button retryButton;
    public Button quitButton;

    [Header("Sub Panel Buttons")]
    public Button howToPlayCloseButton;

    [Header("Volume Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("Display Settings")]
    public Button fullscreenButton;
    public Button resolutionButton;

    private bool isPaused = false;
    private Vector2Int[] resolutions;
    private int resolutionIndex;

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

        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitToMainMenu);

        if (howToPlayCloseButton != null)
            howToPlayCloseButton.onClick.AddListener(BackToPausePanel);

        SetupVolumeSliders();
        SetupDisplaySettings();
    }

    private void SetupDisplaySettings()
    {
        resolutions = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Distinct(
            System.Linq.Enumerable.Select(Screen.resolutions, r => new Vector2Int(r.width, r.height))));

        resolutionIndex = System.Array.IndexOf(resolutions, new Vector2Int(Screen.width, Screen.height));
        if (resolutionIndex < 0)
            resolutionIndex = resolutions.Length - 1;

        if (fullscreenButton != null)
            fullscreenButton.onClick.AddListener(() => ApplyDisplay(!Screen.fullScreen, resolutionIndex));

        if (resolutionButton != null)
            resolutionButton.onClick.AddListener(() => ApplyDisplay(Screen.fullScreen, (resolutionIndex + 1) % Mathf.Max(1, resolutions.Length)));

        UpdateDisplayLabels(Screen.fullScreen);
    }

    private void ApplyDisplay(bool fullscreen, int index)
    {
        resolutionIndex = index;

        if (resolutions.Length > 0)
        {
            Vector2Int res = resolutions[index];
            Screen.SetResolution(res.x, res.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }

        UpdateDisplayLabels(fullscreen);
    }

    private void UpdateDisplayLabels(bool fullscreen)
    {
        TMPro.TMP_Text fullscreenText = fullscreenButton != null ? fullscreenButton.GetComponentInChildren<TMPro.TMP_Text>() : null;
        TMPro.TMP_Text resolutionText = resolutionButton != null ? resolutionButton.GetComponentInChildren<TMPro.TMP_Text>() : null;

        if (fullscreenText != null)
            fullscreenText.text = fullscreen ? "Fullscreen: On" : "Fullscreen: Off";

        if (resolutionText != null && resolutions.Length > 0)
            resolutionText.text = $"{resolutions[resolutionIndex].x} x {resolutions[resolutionIndex].y}";
    }

    private bool openedFromMainMenu;

    private GameObject[] GameOnlyButtons => new[]
    {
        howToPlayButton != null ? howToPlayButton.gameObject : null,
        retryButton != null ? retryButton.gameObject : null,
        quitButton != null ? quitButton.gameObject : null
    };

    public void OpenFromMainMenu(GameObject panel)
    {
        if (panel == null)
            return;

        openedFromMainMenu = true;
        MainMenuManager.Instance?.mainMenuPanel.SetActive(false);

        foreach (GameObject button in GameOnlyButtons)
            SetPanel(button, panel != pausePanel);

        SetPanel(panel, true);
    }

    private void ReturnToMainMenu()
    {
        openedFromMainMenu = false;

        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);

        foreach (GameObject button in GameOnlyButtons)
            SetPanel(button, true);

        MainMenuManager.Instance?.mainMenuPanel.SetActive(true);
    }

    private void Update()
    {
        if (KeybindManager.Instance != null && KeybindManager.Instance.IsCapturingInput)
            return;

        if (openedFromMainMenu)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1))
                ReturnToMainMenu();
            return;
        }

        if (OrderManager.Instance != null && OrderManager.Instance.state != OrderManager.GameState.Playing)
            return;

        if (isPaused && howToPlayPanel != null && howToPlayPanel.activeSelf
            && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)))
        {
            BackToPausePanel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)
            || (isPaused && Input.GetKeyDown(KeyCode.JoystickButton1)))
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
    }

    public void Resume()
    {
        if (openedFromMainMenu)
        {
            ReturnToMainMenu();
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;

        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);
    }

    public void ForceResume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);
    }

    public void OpenHowToPlay()
    {
        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, true);
    }

    public void BackToPausePanel()
    {
        if (openedFromMainMenu)
        {
            ReturnToMainMenu();
            return;
        }

        SetPanel(pausePanel, true);
        SetPanel(howToPlayPanel, false);
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);

        isPaused = false;

        GameModeSelector.retryMode = OrderManager.Instance?.GetCurrentMode();
        MainMenuManager.skipMainMenuOnLoad = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;

        SetPanel(pausePanel, false);
        SetPanel(howToPlayPanel, false);
        isPaused = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
}