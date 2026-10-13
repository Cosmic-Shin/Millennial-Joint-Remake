using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    public static bool skipMainMenuOnLoad = false;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject startPanel;
    public GameObject pausePanel;

    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;
    public Button backButton;
    public Button howToPlayButton;
    public Button settingsButton;

    [Header("Fade")]
    public Image blackFadeImage;
    public float fadeInSpeed = 8f;
    public float fadeOutSpeed = 3f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!skipMainMenuOnLoad)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (startPanel != null) startPanel.SetActive(false);
        }
        else
        {
            skipMainMenuOnLoad = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (startPanel != null) startPanel.SetActive(false);

            StartCoroutine(ShowModeSelectorNextFrame());
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);

        AudioManager.Instance?.PlayMenuBGM();

        SetFade(0f);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(() => PauseManager.Instance?.OpenFromMainMenu(PauseManager.Instance.howToPlayPanel));

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => PauseManager.Instance?.OpenFromMainMenu(PauseManager.Instance.pausePanel));
    }

    private IEnumerator ShowModeSelectorNextFrame()
    {
        yield return null;

        if (GameModeSelector.Instance != null)
        {
            GameModeSelector.Instance.ShowModeSelector();
        }
    }

    public void OnPlay()
    {
        StartCoroutine(FadeToStartPanel());
    }

    private IEnumerator FadeToStartPanel()
    {
        yield return StartCoroutine(Fade(1f, fadeInSpeed));

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (startPanel != null) startPanel.SetActive(true);

        yield return StartCoroutine(Fade(0f, fadeOutSpeed));
    }

    public void OnBack()
    {
        StartCoroutine(FadeToMainMenu());
    }

    private IEnumerator FadeToMainMenu()
    {
        yield return StartCoroutine(Fade(1f, fadeInSpeed));

        if (startPanel != null) startPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        yield return StartCoroutine(Fade(0f, fadeOutSpeed));
    }

    private IEnumerator Fade(float targetAlpha, float speed)
    {
        float start = blackFadeImage.color.a;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * speed;

            float a = Mathf.Lerp(start, targetAlpha, t);
            SetFade(a);

            yield return null;
        }

        SetFade(targetAlpha);
    }

    private void SetFade(float alpha)
    {
        if (blackFadeImage == null) return;

        Color c = blackFadeImage.color;
        c.a = alpha;
        blackFadeImage.color = c;
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}