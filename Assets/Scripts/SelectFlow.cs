using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Character -> Cosmetics -> Map select, run after the control panel's Start.
// Each player drives their own column with their own controls (move = browse, interact = ready/use, drop = back).
public class SelectFlow : MonoBehaviour
{
    public static SelectFlow Instance { get; private set; }

    [Header("Data")]
    public CharacterDefinition[] characters;
    public CosmeticDefinition[] cosmetics;
    public MapDefinition[] maps;

    [Header("Panels")]
    public GameObject characterPanel;
    public GameObject cosmeticPanel;
    public GameObject mapPanel;

    [Header("3D Preview")]
    public Camera previewCamera;
    public Transform[] previewPivots = new Transform[2];
    [Tooltip("Preview chef X offset on the character screen / cosmetics screen")]
    public float characterPivotX = 1.4f;
    public float cosmeticPivotX = 0.55f;

    [Header("Character Select")]
    public TMP_Text[] chefNameTexts = new TMP_Text[2];
    public TMP_Text[] characterStatusTexts = new TMP_Text[2];

    [Header("Cosmetics")]
    public TMP_Text[] cosmeticListTexts = new TMP_Text[2];
    public TMP_Text[] cosmeticStatusTexts = new TMP_Text[2];

    [Header("Map Select")]
    public Image[] mapCards;
    public TMP_Text[] mapCardLabels;
    public TMP_Text mapStatusText;
    public int mapColumns = 5;
    [Tooltip("Picture on the Random card (a shadowed map)")]
    public Sprite randomPreview;

    [Header("Back buttons (mouse)")]
    public Button[] backButtons;

    [Header("Style")]
    public Color[] playerColors = { new Color(1f, 0.38f, 0.32f), new Color(0.38f, 0.62f, 1f) };
    public Color highlightColor = new Color(1f, 0.85f, 0.25f);
    public Color cardColor = new Color(1f, 1f, 1f, 0.15f);
    public Color dimmedPictureColor = new Color(0.6f, 0.6f, 0.6f);
    public Color bestStarOnColor = new Color(0.91f, 0.643f, 0f);
    public Color bestStarOffColor = new Color(0.78f, 0.76f, 0.706f);

    private enum Step { None, Character, Cosmetics, Map }

    private static readonly CosmeticSlot[] Slots = (CosmeticSlot[])Enum.GetValues(typeof(CosmeticSlot));
    private int RandomizeRow => Slots.Length;
    private int ResetRow => Slots.Length + 1;
    private int ReadyRow => Slots.Length + 2;

    private Step step;
    private PlayerControl[] players;
    private readonly bool[] ready = new bool[2];
    private readonly int[] row = new int[2];
    private readonly int[] lastVertical = new int[2];
    private readonly Animator[] previews = new Animator[2];
    private int mapIndex;
    private Action onDone, onBack;
    private int openedFrame;

    private void Awake()
    {
        Instance = this;
        foreach (Button b in backButtons)
            if (b != null)
                b.onClick.AddListener(Back);
        HideAll();
    }

    public void Open(Action done, Action back)
    {
        openedFrame = Time.frameCount;
        onDone = done;
        onBack = back;

        players = FindObjectsByType<PlayerControl>(FindObjectsSortMode.None)
            .OrderBy(p => p.playerNumber).Take(2).ToArray();

        for (int i = 0; i < 2; i++)
            if (GameSession.Players[i].character == null)
                GameSession.Players[i].character = characters[i % characters.Length];

        mapIndex = GameSession.Map != null ? Mathf.Max(0, Array.IndexOf(maps, GameSession.Map)) : maps.Length;
        previewCamera.enabled = true;
        RebuildPreview(0);
        RebuildPreview(1);
        Show(Step.Character);
    }

    private void Show(Step next)
    {
        step = next;
        ready[0] = ready[1] = false;
        row[0] = row[1] = 0;
        characterPanel.SetActive(step == Step.Character);
        cosmeticPanel.SetActive(step == Step.Cosmetics);
        mapPanel.SetActive(step == Step.Map);
        previewCamera.enabled = step == Step.Character || step == Step.Cosmetics;
        float x = step == Step.Cosmetics ? cosmeticPivotX : characterPivotX;
        for (int i = 0; i < previewPivots.Length; i++)
            previewPivots[i].localPosition = new Vector3(i == 0 ? -x : x, 0f, 0f);
        foreach (PlayerControl p in players)
        {
            p.ResetMenuStep();
            lastVertical[p.playerNumber - 1] = Vertical(p);
        }
        Refresh();
    }

    private void HideAll()
    {
        step = Step.None;
        characterPanel.SetActive(false);
        cosmeticPanel.SetActive(false);
        mapPanel.SetActive(false);
        if (previewCamera != null)
            previewCamera.enabled = false;
    }

    private void Update()
    {
        if (step == Step.None || Time.frameCount == openedFrame)
            return;

        AnimateMapCards();

        // Keep UI selection empty so Enter (P2's interact) doesn't click a button.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Back();
            return;
        }

        if (step == Step.Map)
        {
            foreach (PlayerControl p in players)
                MapInput(p);
        }
        else
        {
            foreach (PlayerControl p in players)
                PlayerInput(p, p.playerNumber - 1);

            if (ready[0] && ready[1])
                Show(step == Step.Character ? Step.Cosmetics : Step.Map);
        }

        if (step != Step.None)
            Refresh();
    }

    private static int Vertical(PlayerControl p) => p.IsMoveUpHeld() ? -1 : p.IsMoveDownHeld() ? 1 : 0;

    private int VerticalPressed(PlayerControl p, int i)
    {
        int v = Vertical(p);
        int pressed = v != lastVertical[i] ? v : 0;
        lastVertical[i] = v;
        return pressed;
    }

    private void PlayerInput(PlayerControl p, int i)
    {
        int horizontal = p.GetMenuStep();
        int vertical = VerticalPressed(p, i);

        if (p.IsDropItemPressed())
        {
            if (ready[i])
                ready[i] = false;
            else
                Back();
            return;
        }

        if (ready[i])
            return;

        if (step == Step.Character)
        {
            if (horizontal != 0)
                CycleCharacter(i, horizontal);
            if (p.IsPrimaryActionPressed())
                ready[i] = true;
            return;
        }

        if (vertical != 0)
            row[i] = (row[i] + vertical + ReadyRow + 1) % (ReadyRow + 1);

        if (horizontal != 0 && row[i] < Slots.Length)
            CycleCosmetic(i, Slots[row[i]], horizontal);

        if (p.IsPrimaryActionPressed())
        {
            if (row[i] == RandomizeRow) Randomize(i);
            else if (row[i] == ResetRow) ResetCosmetics(i);
            else ready[i] = true;
        }
    }

    private void MapInput(PlayerControl p)
    {
        int count = maps.Length + 1;
        int horizontal = p.GetMenuStep();
        int vertical = VerticalPressed(p, p.playerNumber - 1);
        if (horizontal != 0)
            mapIndex = (mapIndex + horizontal + count) % count;
        if (vertical != 0)
            mapIndex = Mathf.Clamp(mapIndex + vertical * mapColumns, 0, count - 1);

        if (p.IsDropItemPressed())
            Back();
        else if (p.IsPrimaryActionPressed())
            Finish();
    }

    // Mouse click on a map card.
    public void PickMap(int index)
    {
        mapIndex = index;
        Finish();
    }

    public void Back()
    {
        switch (step)
        {
            case Step.Character:
                HideAll();
                onBack?.Invoke();
                break;
            case Step.Cosmetics: Show(Step.Character); break;
            case Step.Map: Show(Step.Cosmetics); break;
        }
    }

    private void Finish()
    {
        GameSession.Map = mapIndex < maps.Length ? maps[mapIndex] : null;
        MapSelector.Instance?.Choose(GameSession.Map);

        foreach (PlayerControl p in players)
            p.GetComponent<ChefVisual>()?.ApplyFromSession();

        HideAll();
        onDone?.Invoke();
    }

    private void CycleCharacter(int i, int dir)
    {
        GameSession.PlayerChoice choice = GameSession.Players[i];
        int index = Array.IndexOf(characters, choice.character);
        choice.character = characters[(index + dir + characters.Length) % characters.Length];
        RebuildPreview(i);
    }

    private List<CosmeticDefinition> Options(CosmeticSlot slot)
    {
        var list = new List<CosmeticDefinition> { null };
        list.AddRange(cosmetics.Where(c => c.slot == slot));
        return list;
    }

    private void CycleCosmetic(int i, CosmeticSlot slot, int dir)
    {
        var worn = GameSession.Players[i].cosmetics;
        List<CosmeticDefinition> options = Options(slot);
        worn.TryGetValue(slot, out CosmeticDefinition current);
        int index = (options.IndexOf(current) + dir + options.Count) % options.Count;
        Equip(i, slot, options[index]);
        ApplyPreview(i);
    }

    // Equipping an item removes anything it conflicts with.
    private static void Equip(int i, CosmeticSlot slot, CosmeticDefinition item)
    {
        var worn = GameSession.Players[i].cosmetics;
        if (item == null)
        {
            worn.Remove(slot);
            return;
        }

        foreach (var pair in worn.ToList())
            if (pair.Key != slot && item.ConflictsWith(pair.Value))
                worn.Remove(pair.Key);
        worn[slot] = item;
    }

    private void Randomize(int i)
    {
        GameSession.Players[i].cosmetics.Clear();
        foreach (CosmeticSlot slot in Slots)
        {
            List<CosmeticDefinition> options = Options(slot);
            Equip(i, slot, options[UnityEngine.Random.Range(0, options.Count)]);
        }
        ApplyPreview(i);
    }

    private void ResetCosmetics(int i)
    {
        GameSession.Players[i].cosmetics.Clear();
        ApplyPreview(i);
    }

    private void RebuildPreview(int i)
    {
        if (previews[i] != null)
            Destroy(previews[i].gameObject);

        GameSession.PlayerChoice choice = GameSession.Players[i];
        previews[i] = ChefVisual.Spawn(choice.character, choice.cosmetics.Values, previewPivots[i]);
        previews[i].updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    private void ApplyPreview(int i)
    {
        if (previews[i] != null)
            previews[i].GetComponent<CosmeticApplier>().Apply(GameSession.Players[i].cosmetics.Values);
    }

    private string KeyName(PlayerControl p, bool interact)
    {
        if (p.UsesJoystick)
            return interact ? "A" : "X";
        return (interact ? p.PrimaryAction : p.DropItem).ToString();
    }

    private string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    private void Refresh()
    {
        foreach (PlayerControl p in players)
        {
            int i = p.playerNumber - 1;
            string status = ready[i]
                ? $"<color=#2F9E44>READY!</color>  ({KeyName(p, false)} to cancel)"
                : $"{KeyName(p, true)} = Ready   {KeyName(p, false)} = Back";

            if (step == Step.Character)
            {
                chefNameTexts[i].text = $"< {GameSession.Players[i].character.displayName} >";
                characterStatusTexts[i].text = status;
            }
            else if (step == Step.Cosmetics)
            {
                cosmeticListTexts[i].text = CosmeticList(i);
                cosmeticStatusTexts[i].text = status;
            }
        }

        if (step == Step.Map)
        {
            for (int c = 0; c < mapCards.Length; c++)
            {
                bool exists = c <= maps.Length;
                mapCards[c].gameObject.SetActive(exists);
                if (!exists)
                    continue;

                MapDefinition map = c < maps.Length ? maps[c] : null;
                mapCards[c].sprite = map != null ? map.preview : randomPreview;
                bool picture = mapCards[c].sprite != null;
                Transform question = mapCards[c].transform.Find("Question");
                if (question != null)
                    question.gameObject.SetActive(map == null);
                bool selected = c == mapIndex;
                // A yellow tint would muddy the picture, so picture cards dim when not selected instead.
                mapCards[c].color = picture ? (selected ? Color.white : dimmedPictureColor) : selected ? highlightColor : cardColor;
                mapCardLabels[c].text = map != null ? map.displayName : "Random";
                ShowBestStars(mapCards[c].transform.Find("Best Stars"), map);
            }
            string p1 = KeyName(players[0], true), p2 = players.Length > 1 ? KeyName(players[1], true) : p1;
            mapStatusText.text = $"Either player: move to browse, {p1} / {p2} to start";
        }
    }

    [Tooltip("Glowing border colour around the selected map card")]
    public Color mapGlowColor = new Color(1f, 0.6f, 0.05f);

    // The selected card grows, breathes and wears a thick glowing border, so it's obvious which map is picked.
    private void AnimateMapCards()
    {
        if (step != Step.Map || mapCards == null)
            return;

        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);

        for (int c = 0; c < mapCards.Length; c++)
        {
            if (mapCards[c] == null || !mapCards[c].gameObject.activeSelf)
                continue;

            bool selected = c == mapIndex;
            Transform t = mapCards[c].transform;
            float target = selected ? 1.15f + 0.02f * pulse : 0.95f;
            t.localScale = Vector3.Lerp(t.localScale, Vector3.one * target, k);

            Image glow = MapGlow(c);
            Color col = mapGlowColor;
            col.a = Mathf.Lerp(glow.color.a, selected ? 0.85f + 0.15f * pulse : 0f, k);
            glow.color = col;

            // Glow sits behind the card (sibling just before it) and tracks its rect and scale.
            RectTransform g = glow.rectTransform, card = (RectTransform)t;
            g.anchorMin = card.anchorMin; g.anchorMax = card.anchorMax; g.pivot = card.pivot;
            g.anchoredPosition = card.anchoredPosition;
            g.sizeDelta = card.sizeDelta + Vector2.one * 130f;
            g.localScale = t.localScale;
            if (g.GetSiblingIndex() != t.GetSiblingIndex() - 1)
                g.SetSiblingIndex(t.GetSiblingIndex());
        }
    }

    private Image[] mapGlows;
    private static Sprite glowSprite;

    private Image MapGlow(int c)
    {
        if (mapGlows == null || mapGlows.Length != mapCards.Length)
            mapGlows = new Image[mapCards.Length];
        if (mapGlows[c] != null)
            return mapGlows[c];

        if (glowSprite == null)
        {
            // Soft-edged rounded rect, 9-sliced so it stretches to any card size.
            const int n = 64, edge = 28;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0f, edge - Mathf.Min(x, n - 1 - x)), dy = Mathf.Max(0f, edge - Mathf.Min(y, n - 1 - y));
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / edge);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Sqrt(a) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(edge, edge, edge, edge));
        }

        var go = new GameObject("Map Glow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        go.transform.SetParent(mapCards[c].transform.parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = glowSprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.35f; // wider soft edge
        img.raycastTarget = false;
        img.color = new Color(mapGlowColor.r, mapGlowColor.g, mapGlowColor.b, 0f);
        return mapGlows[c] = img;
    }

    // Best rating on this map for the chosen mode; Versus and Random have none.
    private void ShowBestStars(Transform stars, MapDefinition map)
    {
        if (stars == null)
            return;
        OrderManager.GameMode mode = GameModeSelector.Instance != null ? GameModeSelector.Instance.PendingMode : OrderManager.GameMode.TIME;
        bool show = map != null && mode != OrderManager.GameMode.VERSUS;
        stars.gameObject.SetActive(show);
        if (!show)
            return;
        int earned = ScoreManager.GetBestStars(mode, map.layoutName);
        for (int i = 0; i < stars.childCount; i++)
            if (stars.GetChild(i).TryGetComponent(out Image star))
                star.color = i < earned ? bestStarOnColor : bestStarOffColor;
    }

    private string CosmeticList(int i)
    {
        var worn = GameSession.Players[i].cosmetics;
        var sb = new System.Text.StringBuilder();
        for (int r = 0; r <= ReadyRow; r++)
        {
            string line;
            if (r < Slots.Length)
            {
                worn.TryGetValue(Slots[r], out CosmeticDefinition item);
                line = $"{Slots[r]}:  < {(item != null ? item.displayName : "None")} >";
            }
            else
            {
                line = r == RandomizeRow ? "Randomize" : r == ResetRow ? "<color=#D6336C>Reset</color>" : "<color=#2F9E44>Ready</color>";
            }

            sb.AppendLine(r == row[i] && !ready[i] ? $"<mark=#{Hex(highlightColor)}99>> {line}</mark>" : line);
        }
        return sb.ToString();
    }
}
