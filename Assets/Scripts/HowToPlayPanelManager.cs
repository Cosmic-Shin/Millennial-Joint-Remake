using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanelManager : MonoBehaviour
{
    [Header("Pages (cycled with the arrows)")]
    [SerializeField] private GameObject player1Panel;
    [SerializeField] private GameObject player2Panel;
    [SerializeField] private GameObject recipePanel;

    [Header("Navigation Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Tooltip("Keybind reset; only shown on the keybind pages")]
    [SerializeField] private GameObject resetButton;

    [Header("Recipe Page (sprites shown slots.Length at a time)")]
    [SerializeField] private Image[] recipeSlots;
    [SerializeField] private Sprite[] recipeSprites;
    [SerializeField] private Button recipePrevButton;
    [SerializeField] private Button recipeNextButton;
    [SerializeField] private TMPro.TMP_Text recipePageText;

    private readonly List<GameObject> pages = new List<GameObject>();
    private int currentPage = 0;
    private int recipePage = 0;

    private void Start()
    {
        foreach (GameObject page in new[] { player1Panel, player2Panel, recipePanel })
        {
            if (page != null)
                pages.Add(page);
        }

        if (leftButton != null)
            leftButton.onClick.AddListener(() => ShowPage(currentPage - 1));

        if (rightButton != null)
            rightButton.onClick.AddListener(() => ShowPage(currentPage + 1));

        if (recipePrevButton != null)
            recipePrevButton.onClick.AddListener(() => ShowRecipePage(recipePage - 1));

        if (recipeNextButton != null)
            recipeNextButton.onClick.AddListener(() => ShowRecipePage(recipePage + 1));

        ShowPage(0);
        ShowRecipePage(0);
    }

    private void ShowRecipePage(int index)
    {
        if (recipeSlots == null || recipeSlots.Length == 0 || recipeSprites == null || recipeSprites.Length == 0)
            return;

        int pageCount = Mathf.CeilToInt(recipeSprites.Length / (float)recipeSlots.Length);
        recipePage = (index % pageCount + pageCount) % pageCount;

        for (int i = 0; i < recipeSlots.Length; i++)
        {
            int spriteIndex = recipePage * recipeSlots.Length + i;
            bool hasSprite = spriteIndex < recipeSprites.Length && recipeSprites[spriteIndex] != null;

            recipeSlots[i].gameObject.SetActive(hasSprite);
            if (hasSprite)
                recipeSlots[i].sprite = recipeSprites[spriteIndex];
        }

        if (recipePageText != null)
            recipePageText.text = $"{recipePage + 1} / {pageCount}";
    }

    private void ShowPage(int index)
    {
        if (pages.Count == 0)
            return;

        currentPage = (index % pages.Count + pages.Count) % pages.Count;

        for (int i = 0; i < pages.Count; i++)
            pages[i].SetActive(i == currentPage);

        if (resetButton != null)
            resetButton.SetActive(pages[currentPage] != recipePanel);
    }
}
