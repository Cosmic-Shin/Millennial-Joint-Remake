using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A handwritten note from the Boss before each chapter, and the trophy finale after the last one.
public class StoryScenePanel : MonoBehaviour
{
    public static StoryScenePanel Instance { get; private set; }

    public GameObject panel;
    public TMP_Text title;
    public TMP_Text body;
    public TMP_Text prompt;
    public Image doodle;
    [Tooltip("Full-screen button: clicking anywhere continues")]
    public Button continueButton;
    [Tooltip("One doodle per chapter, then the finale trophy")]
    public Sprite[] doodles;

    private const float CharactersPerSecond = 55f;

    private static readonly (string title, string note)[] Notes =
    {
        ("Welcome Aboard", "Chefs! Big Bang Burgers opens today and the whole galaxy is hungry.\n\nKeep it simple: soda and fried chicken. Try not to burn my kitchen down.\n\n- The Boss"),
        ("First Review", "\"Decent chicken, for a space station.\" We'll take it!\n\nWe're expanding to the Split Islands. Mind the gap, and get those fries moving.\n\n- The Boss"),
        ("Breakfast Crowd", "Bridge Islands gets a morning rush, and aliens without coffee are a safety hazard.\n\nPour fast. Scoop faster.\n\n- The Boss"),
        ("A Big Dessert Order", "Ring Station wants dessert for its whole crew. All of them. At once.\n\nDon't ask how many stomachs that is.\n\n- The Boss"),
        ("Lunch on the Move", "Commuters only have one stop to eat, so we're cooking between shuttles.\n\nBurgers and donuts. No excuses, no refunds.\n\n- The Boss"),
        ("The Critic Has Landed", "The galaxy's toughest food critic just parked on our asteroid.\n\nEverything we've learned goes on the menu. Make it legendary.\n\n- The Boss"),
        ("Five Galaxies!", "The critic cried. Then ordered seconds. Then gave us five galaxies out of five.\n\nThis Golden Spatula is yours, chefs. You earned it.\n\n- The Boss")
    };

    public static int FinaleScene => Notes.Length - 1;

    private static string SeenKey(int chapter) => "Story_Seen_" + chapter;
    public static bool HasSeen(int chapter) => PlayerPrefs.GetInt(SeenKey(chapter), 0) == 1;

    private void Awake()
    {
        Instance = this;
        if (continueButton != null)
            continueButton.onClick.AddListener(() => clicked = true);
        if (panel != null)
            panel.SetActive(false);
    }

    private bool clicked;

    public void Show(int scene, Action done)
    {
        if (scene < StoryMode.Chapters.Length)
        {
            PlayerPrefs.SetInt(SeenKey(scene), 1);
            PlayerPrefs.Save();
        }

        StartCoroutine(Run(scene, done));
    }

    private bool ContinuePressed(PlayerControl[] players)
    {
        if (clicked)
        {
            clicked = false;
            return true;
        }

        foreach (PlayerControl p in players)
            if (p.IsPrimaryActionPressed())
                return true;
        return false;
    }

    private IEnumerator Run(int scene, Action done)
    {
        PlayerControl[] players = FindObjectsByType<PlayerControl>(FindObjectsSortMode.None);

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        title.text = Notes[scene].title;
        body.text = Notes[scene].note;
        body.maxVisibleCharacters = 0;
        body.ForceMeshUpdate();
        doodle.sprite = doodles[scene];
        prompt.text = "";
        StartCoroutine(VisualFx.Pop(doodle.transform, 1.5f, 0.3f));

        if (scene == FinaleScene)
        {
            AudioManager.Instance?.PlayChapterClearSFX();
            StartCoroutine(VisualFx.Confetti((RectTransform)panel.transform, new Color(0.95f, 0.74f, 0.17f), 110, 5f));
        }
        else
        {
            AudioManager.Instance?.PlayChalkHoverSFX();
        }

        yield return null; // the press that opened this must not also skip it
        clicked = false;

        // First press finishes the writing, second press moves on.
        int total = body.textInfo.characterCount;
        for (float shown = 0f; shown < total; shown += Time.unscaledDeltaTime * CharactersPerSecond)
        {
            ClearUiFocus();
            body.maxVisibleCharacters = Mathf.FloorToInt(shown);
            if (ContinuePressed(players))
                break;
            yield return null;
        }
        body.maxVisibleCharacters = total;
        yield return null;

        prompt.text = PromptText(players);
        StartCoroutine(VisualFx.Pop(prompt.transform, 1.25f, 0.2f));

        while (!ContinuePressed(players))
        {
            ClearUiFocus();
            yield return null;
        }

        AudioManager.Instance?.PlayButtonClickSFX();
        PaperWipe.Play(() =>
        {
            panel.SetActive(false);
            done?.Invoke();
        });
    }

    private static string PromptText(PlayerControl[] players)
    {
        if (players.Length == 0)
            return "Click to continue";

        Array.Sort(players, (a, b) => a.playerNumber.CompareTo(b.playerNumber));
        string first = players[0].UsesJoystick ? "A" : players[0].PrimaryAction.ToString();
        string second = players.Length < 2 ? first : players[1].UsesJoystick ? "A" : players[1].PrimaryAction.ToString();
        return first == second ? $"Press {first} to continue" : $"Press {first} / {second} to continue";
    }

    // Keeps Enter (P2's interact) from also clicking a button underneath.
    private static void ClearUiFocus()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
