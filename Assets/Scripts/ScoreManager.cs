using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Game End Panel")]
    public GameObject gameEndPanel;
    [Tooltip("Big heading: \"You Win!\" or \"Game Over\"")]
    public TextMeshProUGUI gameEndTitleText;
    public TextMeshProUGUI gameEndScoreText;
    public Button gameEndRetryButton;
    public Button gameEndQuitButton;

    [Header("Stars")]
    public Image[] starImages = new Image[3];
    public Color starOnColor = new Color(1f, 0.85f, 0.2f);
    public Color starOffColor = new Color(1f, 1f, 1f, 0.2f);
    [Tooltip("Time mode: money / goal needed for 2 and 3 stars (reaching the goal = 1 star)")]
    public float timeTwoStarRatio = 1.5f;
    public float timeThreeStarRatio = 2f;
    [Tooltip("Speed mode: reach the goal within these seconds for 3 / 2 stars")]
    public float speedThreeStarSeconds = 90f;
    public float speedTwoStarSeconds = 150f;
    [Tooltip("The Speed star times are for this goal; other goals (Match Rules) scale them")]
    public float speedStarGoal = 100f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (gameEndRetryButton != null) gameEndRetryButton.onClick.AddListener(Retry);
        if (gameEndQuitButton != null) gameEndQuitButton.onClick.AddListener(Menu);
    }

    public void ShowGameEndPanel(bool won, OrderManager.GameMode mode, float money, float time)
    {
        Time.timeScale = 0f;

        if (gameEndPanel != null)
            gameEndPanel.SetActive(true);

        StartCoroutine(EndScreenRoutine(won, mode, money, time));
    }

    private System.Collections.IEnumerator EndScreenRoutine(bool won, OrderManager.GameMode mode, float money, float time)
    {
        string header = "";
        if (gameEndTitleText != null)
            gameEndTitleText.text = won ? "You Win!" : "Game Over";
        else
            header = won ? "You Win!\n" : "Game Over\n";

        foreach (Image star in starImages)
        {
            if (star == null) continue;
            star.color = starOffColor;
            star.transform.localScale = Vector3.one;
        }

        if (gameEndScoreText != null)
        {
            if (mode == OrderManager.GameMode.TIME)
                yield return VisualFx.CountUp(money, 1.2f, v => gameEndScoreText.text = header + "Money Earned: " + OrderUIManager.Money(v < money ? Mathf.Round(v) : money));
            else
                yield return VisualFx.CountUp(time, 1.2f, v => gameEndScoreText.text = header + "Time Reached: " + OrderUIManager.Clock(v));

            if (OrderManager.Instance != null)
                gameEndScoreText.text += "\n<size=60%>" + OrderUIManager.Summary(OrderManager.Instance.GetStats(0)) + "</size>";
        }

        int stars = CountStars(won, mode, money, time);

        if (stars > GetBestStars(mode))
        {
            PlayerPrefs.SetInt(BestStarsKey(mode), stars);
            PlayerPrefs.Save();
        }

        for (int i = 0; i < stars && i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            starImages[i].color = starOnColor;
            yield return VisualFx.Pop(starImages[i].transform, 1.8f, 0.3f);
        }
    }

    public static int GetBestStars(OrderManager.GameMode mode) => PlayerPrefs.GetInt(BestStarsKey(mode), 0);

    private static string BestStarsKey(OrderManager.GameMode mode) => "BestStars_" + mode;

    private int CountStars(bool won, OrderManager.GameMode mode, float money, float time)
    {
        if (!won)
            return 0;

        float quota = OrderManager.Instance != null ? Mathf.Max(0.01f, OrderManager.Instance.moneyQuota) : speedStarGoal;

        if (mode == OrderManager.GameMode.TIME)
        {
            float ratio = money / quota;
            return ratio >= timeThreeStarRatio ? 3 : ratio >= timeTwoStarRatio ? 2 : 1;
        }

        float scale = quota / Mathf.Max(0.01f, speedStarGoal);
        return time <= speedThreeStarSeconds * scale ? 3 : time <= speedTwoStarSeconds * scale ? 2 : 1;
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        GameModeSelector.retryMode = OrderManager.Instance?.GetCurrentMode();
        MainMenuManager.skipMainMenuOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Menu()
    {
        Time.timeScale = 1f;
        MainMenuManager.skipMainMenuOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
