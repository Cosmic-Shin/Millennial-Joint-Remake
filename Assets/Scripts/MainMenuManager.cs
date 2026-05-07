using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject startPanel; // your mode panel
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
        // 🔥 INITIAL STATE
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (startPanel != null) startPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        SetFade(0f);

        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);

        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);

        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
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