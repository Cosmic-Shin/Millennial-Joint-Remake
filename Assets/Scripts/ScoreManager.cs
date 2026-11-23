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
    [Tooltip("Story Mode: shown after a win when another chapter follows")]
    public Button gameEndNextButton;
    public Sprite newChapterStamp;

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
        if (gameEndNextButton != null)
        {
            gameEndNextButton.onClick.AddListener(NextChapter);
            gameEndNextButton.gameObject.SetActive(false);
        }
    }

    public void ShowGameEndPanel(bool won, OrderManager.GameMode mode, float money, float time)
    {
        Time.timeScale = 0f;

        // Saved before the animation, so pressing Retry or Menu during it can't lose the result.
        int stars = CountStars(won, mode, money, time);
        bool openedChapter = StoryMode.RecordResult(stars);

        string map = MapSelector.Instance?.Current?.name;
        if (!StoryMode.IsActive && stars > GetBestStars(mode, map))
        {
            PlayerPrefs.SetInt(BestStarsKey(mode, map), stars);
            PlayerPrefs.Save();
        }

        OrderUIManager.Instance?.HideNormalGameplayUI(); // tickets and timer otherwise poke out over the end sheet

        if (gameEndPanel != null)
            gameEndPanel.SetActive(true);

        StartCoroutine(EndScreenRoutine(won, mode, money, time, stars, openedChapter));
    }

    private string StoryHint(bool won, int stars, float money)
    {
        float goal = OrderManager.Instance != null ? OrderManager.Instance.moneyQuota : 0f;
        if (!won)
            return OrderUIManager.Money(Mathf.Ceil(goal - money)) + " short of the goal";
        return stars >= 3 ? "" : "Next star at " + OrderUIManager.Money(Mathf.Ceil(goal * (stars == 1 ? StoryMode.TwoStarRatio : StoryMode.ThreeStarRatio)));
    }

    private System.Collections.IEnumerator EndScreenRoutine(bool won, OrderManager.GameMode mode, float money, float time, int stars, bool openedChapter)
    {
        string header = "";
        string winTitle = !StoryMode.IsActive ? "You Win!" : StoryMode.HasNext ? "Chapter Cleared!" : "Story Complete!";
        string loseTitle = StoryMode.IsActive ? "Goal Missed" : "Game Over";
        if (gameEndTitleText != null)
            gameEndTitleText.text = won ? winTitle : loseTitle;
        else
            header = (won ? winTitle : loseTitle) + "\n";

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
                gameEndScoreText.text += $"\n<size={(StoryMode.IsActive ? 50 : 60)}%>" + OrderUIManager.Summary(OrderManager.Instance.GetStats(0)) + "</size>";

            if (StoryMode.IsActive && StoryHint(won, stars, money).Length > 0)
                gameEndScoreText.text += "\n<size=50%><color=#3B5BDB>" + StoryHint(won, stars, money) + "</color></size>";
        }

        if (StoryMode.IsActive && won)
        {
            AudioManager.Instance?.PlayChapterClearSFX();
            StartCoroutine(VisualFx.Confetti((RectTransform)gameEndPanel.transform, starOnColor));
        }

        for (int i = 0; i < stars && i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            starImages[i].color = starOnColor;
            if (StoryMode.IsActive)
                AudioManager.Instance?.PlayStoryStarSFX(i);
            yield return VisualFx.Pop(starImages[i].transform, 1.8f, 0.3f);
        }

        if (!StoryMode.IsActive || !won)
            yield break;

        if (!StoryMode.HasNext)
        {
            // Last chapter: the same button opens the finale.
            if (gameEndNextButton != null && StoryScenePanel.Instance != null)
            {
                gameEndNextButton.GetComponentInChildren<TMP_Text>(true).text = "Finale";
                gameEndNextButton.gameObject.SetActive(true);
                gameEndNextButton.Select();
            }
            yield break;
        }

        if (openedChapter)
        {
            AudioManager.Instance?.PlayChapterUnlockSFX();
            if (gameEndScoreText != null)
            {
                int next = StoryMode.CurrentIndex + 1;
                string meals = string.Join(", ", System.Array.ConvertAll(StoryMode.NewMeals(next), m => new OrderItem(m).GetDisplayName()));
                gameEndScoreText.text += $"\n<size=50%><color=#D6336C>Unlocked: {StoryMode.Chapters[next].title}" + (meals.Length > 0 ? $"  |  New: {meals}" : "") + "</color></size>";
            }
        }

        if (gameEndNextButton != null)
        {
            gameEndNextButton.gameObject.SetActive(true);
            gameEndNextButton.Select(); // so a gamepad lands on Next

            if (openedChapter && newChapterStamp != null)
            {
                var stamp = new GameObject("New Stamp", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                stamp.rectTransform.SetParent(gameEndNextButton.transform, false);
                stamp.rectTransform.anchoredPosition = new Vector2(150f, 42f);
                stamp.rectTransform.sizeDelta = new Vector2(120f, 60f);
                stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                stamp.sprite = newChapterStamp;
                stamp.raycastTarget = false;
                yield return VisualFx.Pop(stamp.transform, 3.2f, 0.22f);
            }
        }
    }

    private void NextChapter()
    {
        if (!StoryMode.HasNext)
        {
            if (StoryScenePanel.Instance != null)
                StoryScenePanel.Instance.Show(StoryScenePanel.FinaleScene, Menu);
            return;
        }

        if (SelectFlow.Instance == null)
            return;

        StoryMode.CurrentIndex++;
        GameSession.Map = System.Array.Find(SelectFlow.Instance.maps, m => m.layoutName == StoryMode.Current.layoutName);
        Retry();
    }

    public static int GetBestStars(OrderManager.GameMode mode, string map) => PlayerPrefs.GetInt(BestStarsKey(mode, map), 0);

    private static string BestStarsKey(OrderManager.GameMode mode, string map) => "BestStars_" + mode + "_" + map;

    private int CountStars(bool won, OrderManager.GameMode mode, float money, float time)
    {
        if (!won)
            return 0;

        float quota = OrderManager.Instance != null ? Mathf.Max(0.01f, OrderManager.Instance.moneyQuota) : speedStarGoal;

        if (mode == OrderManager.GameMode.TIME)
        {
            float ratio = money / quota;
            float three = StoryMode.IsActive ? StoryMode.ThreeStarRatio : timeThreeStarRatio;
            float two = StoryMode.IsActive ? StoryMode.TwoStarRatio : timeTwoStarRatio;
            return ratio >= three ? 3 : ratio >= two ? 2 : 1;
        }

        float scale = quota / Mathf.Max(0.01f, speedStarGoal);
        return time <= speedThreeStarSeconds * scale ? 3 : time <= speedTwoStarSeconds * scale ? 2 : 1;
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        GameModeSelector.retryMode = OrderManager.Instance?.GetCurrentMode();
        MainMenuManager.skipMainMenuOnLoad = true;
        PaperWipe.ReloadScene();
    }

    private void Menu()
    {
        Time.timeScale = 1f;
        MainMenuManager.skipMainMenuOnLoad = true;
        PaperWipe.ReloadScene();
    }
}
