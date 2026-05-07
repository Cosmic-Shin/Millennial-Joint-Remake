using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Game End Panel")]
    public GameObject gameEndPanel;
    public TextMeshProUGUI gameEndScoreText;
    public TMP_InputField nameInputField;
    public Button enterNameButton;
    public Button gameEndRetryButton;
    public Button gameEndQuitButton;

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TextMeshProUGUI[] nameTexts = new TextMeshProUGUI[5];
    public TextMeshProUGUI[] scoreTexts = new TextMeshProUGUI[5];
    public Button scoreCloseButton;
    public Button modeButton;
    public TextMeshProUGUI modeButtonText;

    private OrderManager.GameMode currentResultMode;
    private float currentResultValue;
    private bool showingTimeMode = true;

    [Header("Name Settings")]
    public int maxNameLength = 8;

    [TextArea]
    public string[] bannedWords = { "badword", "shit", "fuck" };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (scorePanel != null) scorePanel.SetActive(false);

        if (enterNameButton != null) enterNameButton.onClick.AddListener(SaveCurrentScore);
        if (gameEndRetryButton != null) gameEndRetryButton.onClick.AddListener(Retry);
        if (gameEndQuitButton != null) gameEndQuitButton.onClick.AddListener(Quit);

        if (scoreCloseButton != null) scoreCloseButton.onClick.AddListener(CloseScorePanel);
        if (modeButton != null) modeButton.onClick.AddListener(ToggleScoreMode);

        if (nameInputField != null)
    {
        nameInputField.characterLimit = maxNameLength;
    }

        ShowTimeScores();
    }

    public void ShowGameEndPanel(bool won, OrderManager.GameMode mode, float money, float time)
    {
        Time.timeScale = 0f;

        currentResultMode = mode;
        currentResultValue = mode == OrderManager.GameMode.TIME ? money : time;

        if (gameEndPanel != null)
            gameEndPanel.SetActive(true);

        if (nameInputField != null)
            nameInputField.text = "";

        if (gameEndScoreText != null)
        {
            if (mode == OrderManager.GameMode.TIME)
                gameEndScoreText.text = (won ? "You Win!\n" : "Game Over\n") + "Money Earned: $" + money.ToString("0.00");
            else
                gameEndScoreText.text = (won ? "You Win!\n" : "Game Over\n") + "Time Reached: " + Mathf.CeilToInt(time) + "s";
        }
    }

    public void SaveCurrentScore()
    {
        if (nameInputField == null)
            return;

        string playerName = nameInputField.text;

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Player";

        playerName = FilterBadWords(playerName);

        SaveScore(currentResultMode, playerName, currentResultValue);

        // 🔥 LOCK INPUT AFTER SUBMIT
        nameInputField.interactable = false;
        nameInputField.readOnly = true;

        if (enterNameButton != null)
            enterNameButton.interactable = false;
    }

    private string FilterBadWords(string input)
    {
        string lowerInput = input.ToLower();

        foreach (string word in bannedWords)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            string lowerWord = word.ToLower();

            if (lowerInput.Contains(lowerWord))
            {
                input = input.Replace(word, "****", System.StringComparison.OrdinalIgnoreCase);
            }
        }      

        return input;
    }

    private void SaveScore(OrderManager.GameMode mode, string playerName, float value)
    {
        string prefix = mode == OrderManager.GameMode.TIME ? "Time" : "Speed";

        string[] names = new string[6];
        float[] scores = new float[6];

        for (int i = 0; i < 5; i++)
        {
            names[i] = PlayerPrefs.GetString(prefix + "_Name_" + i, "---");
            scores[i] = PlayerPrefs.GetFloat(prefix + "_Score_" + i, mode == OrderManager.GameMode.TIME ? 0f : 999999f);
        }

        names[5] = playerName;
        scores[5] = value;

        for (int i = 0; i < scores.Length - 1; i++)
        {
            for (int j = i + 1; j < scores.Length; j++)
            {
                bool swap = mode == OrderManager.GameMode.TIME
                    ? scores[j] > scores[i]
                    : scores[j] < scores[i];

                if (swap)
                {
                    float tempScore = scores[i];
                    scores[i] = scores[j];
                    scores[j] = tempScore;

                    string tempName = names[i];
                    names[i] = names[j];
                    names[j] = tempName;
                }
            }
        }

        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.SetString(prefix + "_Name_" + i, names[i]);
            PlayerPrefs.SetFloat(prefix + "_Score_" + i, scores[i]);
        }

        PlayerPrefs.Save();
    }

    public void OpenScorePanel()
    {
        if (scorePanel != null)
            scorePanel.SetActive(true);

        showingTimeMode = true;
        ShowTimeScores();
    }

    public void CloseScorePanel()
    {
        if (scorePanel != null)
            scorePanel.SetActive(false);
    }

    public void ToggleScoreMode()
    {
        showingTimeMode = !showingTimeMode;

        if (showingTimeMode)
            ShowTimeScores();
        else
            ShowSpeedScores();
    }

    private void ShowTimeScores()
    {
        showingTimeMode = true;

        if (modeButtonText != null)
            modeButtonText.text = "Time Mode";

        LoadScores(OrderManager.GameMode.TIME);
    }

    private void ShowSpeedScores()
    {
        showingTimeMode = false;

        if (modeButtonText != null)
            modeButtonText.text = "Speed Mode";

        LoadScores(OrderManager.GameMode.SPEED);
    }

    private void LoadScores(OrderManager.GameMode mode)
    {
        string prefix = mode == OrderManager.GameMode.TIME ? "Time" : "Speed";

        for (int i = 0; i < 5; i++)
        {
            string playerName = PlayerPrefs.GetString(prefix + "_Name_" + i, "---");
            float score = PlayerPrefs.GetFloat(prefix + "_Score_" + i, mode == OrderManager.GameMode.TIME ? 0f : 999999f);

            if (nameTexts[i] != null)
                nameTexts[i].text = playerName;

            if (scoreTexts[i] != null)
            {
                if (mode == OrderManager.GameMode.TIME)
                    scoreTexts[i].text = "$" + score.ToString("0.00");
                else
                    scoreTexts[i].text = score >= 999999f ? "---" : Mathf.CeilToInt(score) + "s";
            }
        }
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Quit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}