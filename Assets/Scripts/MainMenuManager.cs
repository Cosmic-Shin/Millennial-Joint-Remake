using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    // ADD THIS
    public static bool skipMainMenuOnLoad = false;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject startPanel;
    public GameObject pausePanel;

    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;
    public Button backButton;

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
        // =========================
        // NORMAL START
        // =========================
        if (!skipMainMenuOnLoad)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (startPanel != null) startPanel.SetActive(false);
        }
        // =========================
        // RETRY START
        // =========================
        else
        {
            skipMainMenuOnLoad = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (startPanel != null) startPanel.SetActive(false);

            StartCoroutine(ShowModeSelectorNextFrame());
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);

        SetFade(0f);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    private IEnumerator ShowModeSelectorNextFrame()
    {
        yield return null;

        if (GameModeSelector.Instance != null)
        {
            GameModeSelector.Instance.ShowModeSelector();
        }
    }

    // =========================
    // PLAY
    // =========================
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

    // =========================
    // BACK
    // =========================
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

    // =========================
    // SHOW MAIN MENU
    // =========================
    public void ShowMainMenu()
    {
        StartCoroutine(FadeAndShowMainMenu());
    }

    private IEnumerator FadeAndShowMainMenu()
    {
        yield return StartCoroutine(Fade(1f, fadeInSpeed));

        if (GameModeSelector.Instance != null && GameModeSelector.Instance.modePanel != null)
            GameModeSelector.Instance.modePanel.SetActive(false);

        if (OrderUIManager.Instance != null)
            OrderUIManager.Instance.HideNormalGameplayUI();

        if (VersusUIManager.Instance != null)
            VersusUIManager.Instance.HideVersusUI();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (startPanel != null) startPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        AudioManager.Instance?.PlayMenuBGM();

        yield return StartCoroutine(Fade(0f, fadeOutSpeed));
    }

    // =========================
    // HIDE MAIN MENU
    // =========================
    public void HideMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (startPanel != null)
            startPanel.SetActive(false);
    }

    // =========================
    // FADE SYSTEM
    // =========================
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

    // =========================
    // QUIT
    // =========================
    public void Quit()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}