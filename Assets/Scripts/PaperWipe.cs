using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Screen transition: a sheet of order paper slides up over the screen, the switch happens behind it,
// then it slides off the top. Survives scene loads, so it also covers Retry / Menu reloads.
public class PaperWipe : MonoBehaviour
{
    private static PaperWipe instance;
    private RectTransform sheet;
    private bool busy;

    private const float InTime = 0.28f, OutTime = 0.35f;
    private static readonly Color Paper = new Color(0.97f, 0.95f, 0.88f);
    private static readonly Color Ink = new Color(0.12f, 0.1f, 0.15f);

    public static void Play(System.Action midpoint)
    {
        if (instance == null)
            Create();
        if (instance.busy)
        {
            midpoint?.Invoke(); // already wiping: just do it
            return;
        }
        instance.StartCoroutine(instance.Run(midpoint));
    }

    public static void ReloadScene() => Play(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

    private IEnumerator Run(System.Action midpoint)
    {
        busy = true;
        sheet.gameObject.SetActive(true);
        float h = ((RectTransform)sheet.parent).rect.height * 1.1f + 100f; // oversized sheet + torn edge

        for (float t = 0f; t < InTime; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / InTime, 3f);
            Place(Mathf.Lerp(-h, 0f, k), (1f - k) * 4f);
            yield return null;
        }
        Place(0f, 0f);

        midpoint?.Invoke();
        yield return null; // let the new screen (or scene) settle behind the paper
        yield return null;

        for (float t = 0f; t < OutTime; t += Time.unscaledDeltaTime)
        {
            float k = t / OutTime;
            Place(Mathf.Lerp(0f, h, k * k), -k * 4f);
            yield return null;
        }
        sheet.gameObject.SetActive(false);
        busy = false;
    }

    private void Place(float y, float tilt)
    {
        sheet.anchoredPosition = new Vector2(0f, y);
        sheet.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    private static void Create()
    {
        var root = new GameObject("Paper Wipe", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        instance = root.AddComponent<PaperWipe>();

        // Oversized so the tilt never shows a corner; blocks clicks while it's up.
        var sheet = new GameObject("Sheet", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        sheet.SetParent(root.transform, false);
        sheet.anchorMin = new Vector2(-0.1f, -0.05f);
        sheet.anchorMax = new Vector2(1.1f, 1.05f);
        sheet.offsetMin = sheet.offsetMax = Vector2.zero;
        sheet.GetComponent<Image>().color = Paper;
        instance.sheet = sheet;

        // Ruled lines, like the order tickets.
        for (int i = 1; i < 14; i++)
        {
            var line = new GameObject("Line", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            line.SetParent(sheet, false);
            line.anchorMin = new Vector2(0f, i / 14f);
            line.anchorMax = new Vector2(1f, i / 14f);
            line.sizeDelta = new Vector2(0f, 3f);
            var img = line.GetComponent<Image>();
            img.color = new Color(0.45f, 0.6f, 0.9f, 0.5f);
            img.raycastTarget = false;
        }

        // Torn edges top and bottom.
        Sprite teeth = MakeTeethSprite();
        foreach (bool top in new[] { true, false })
        {
            var edge = new GameObject("Torn Edge", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            edge.SetParent(sheet, false);
            edge.anchorMin = new Vector2(0f, top ? 1f : 0f);
            edge.anchorMax = new Vector2(1f, top ? 1f : 0f);
            edge.sizeDelta = new Vector2(0f, 40f);
            edge.localScale = new Vector3(1f, top ? 1f : -1f, 1f);
            edge.pivot = new Vector2(0.5f, 0f);
            var img = edge.GetComponent<Image>();
            img.sprite = teeth;
            img.type = Image.Type.Tiled;
            img.color = Paper;
            img.raycastTarget = false;
        }

        // Title in the middle, in the menu's marker font if it's around.
        var title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        title.rectTransform.SetParent(sheet, false);
        title.rectTransform.sizeDelta = new Vector2(1400f, 300f);
        title.text = "Big Bang Burgers!";
        title.fontSize = 130f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Ink;
        title.raycastTarget = false;
        title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (f.name.StartsWith("PermanentMarker"))
            {
                title.font = f;
                break;
            }

        sheet.gameObject.SetActive(false);
    }

    // A row of triangle teeth, tiled along the sheet's edge.
    private static Sprite MakeTeethSprite()
    {
        const int w = 40, h = 40;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float peak = h * (1f - Mathf.Abs(x - w / 2f + 0.5f) / (w / 2f));
                px[y * w + x] = new Color32(255, 255, 255, (byte)(y < peak ? 255 : 0));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
    }
}
