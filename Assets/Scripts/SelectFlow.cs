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
    public float previewSpinSpeed = 35f;
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

    [Header("Back buttons (mouse)")]
    public Button[] backButtons;

    [Header("Style")]
    public Color[] playerColors = { new Color(1f, 0.38f, 0.32f), new Color(0.38f, 0.62f, 1f) };
    public Color highlightColor = new Color(1f, 0.85f, 0.25f);
    public Color cardColor = new Color(1f, 1f, 1f, 0.15f);

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

        // Keep UI selection empty so Enter (P2's interact) doesn't click a button.
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            EventSystem.current.SetSelectedGameObject(null);

        foreach (Transform pivot in previewPivots)
            pivot.Rotate(0f, previewSpinSpeed * Time.unscaledDeltaTime, 0f, Space.World);

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
                ? $"<color=#{Hex(highlightColor)}>READY!</color>  ({KeyName(p, false)} to cancel)"
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
                mapCards[c].sprite = map != null ? map.preview : null;
                mapCards[c].color = c == mapIndex ? highlightColor : map != null && map.preview != null ? Color.white : cardColor;
                mapCardLabels[c].text = map != null ? map.displayName : "Random";
            }
            string p1 = KeyName(players[0], true), p2 = players.Length > 1 ? KeyName(players[1], true) : p1;
            mapStatusText.text = $"Either player: move to browse, {p1} / {p2} to start";
        }
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
                line = r == RandomizeRow ? "Randomize" : r == ResetRow ? "Reset" : "Ready";
            }

            sb.AppendLine(r == row[i] && !ready[i] ? $"<color=#{Hex(highlightColor)}>> {line}</color>" : line);
        }
        return sb.ToString();
    }
}
