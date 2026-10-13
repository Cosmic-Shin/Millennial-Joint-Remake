using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class VisualFx
{
    public static IEnumerator Pop(Transform t, float from, float time, Vector3? baseScale = null)
    {
        Vector3 target = baseScale ?? Vector3.one;

        for (float e = 0f; e < time && t != null; e += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - e / time, 3f);
            t.localScale = target * Mathf.Lerp(from, 1f, k);
            yield return null;
        }

        if (t != null)
            t.localScale = target;
    }

    public static IEnumerator Shake(RectTransform r, float amount, float time)
    {
        Vector2 start = r.anchoredPosition;

        for (float e = 0f; e < time && r != null; e += Time.unscaledDeltaTime)
        {
            r.anchoredPosition = start + Random.insideUnitCircle * amount * (1f - e / time);
            yield return null;
        }

        if (r != null)
            r.anchoredPosition = start;
    }

    public static IEnumerator FadeIn(CanvasGroup group, Transform scaleTarget, Vector3 baseScale, float time)
    {
        for (float e = 0f; e < time && group != null; e += Time.unscaledDeltaTime)
        {
            float k = e / time;
            group.alpha = k;
            scaleTarget.localScale = baseScale * Mathf.Lerp(0.96f, 1f, k);
            yield return null;
        }

        if (group != null)
        {
            group.alpha = 1f;
            scaleTarget.localScale = baseScale;
        }
    }

    public static IEnumerator CountUp(float target, float time, System.Action<float> set)
    {
        for (float e = 0f; e < time; e += Time.unscaledDeltaTime)
        {
            set(target * (1f - Mathf.Pow(1f - e / time, 2f)));
            yield return null;
        }

        set(target);
    }

    public static IEnumerator FloatText(RectTransform parent, Vector2 position, string text, TMP_FontAsset font, Color color, float size = 60f)
    {
        var go = new GameObject("FloatText", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400f, 100f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        for (float e = 0f; e < 1f; e += Time.unscaledDeltaTime)
        {
            rect.anchoredPosition = position + Vector2.up * 80f * e;
            tmp.alpha = 1f - e * e;
            yield return null;
        }

        Object.Destroy(go);
    }

    public static IEnumerator DropGhost(Image source, Sprite sprite)
    {
        var go = new GameObject("ExpiredOrder", typeof(RectTransform));
        go.transform.SetParent(source.transform.parent, false);
        var rect = (RectTransform)go.transform;
        var src = (RectTransform)source.transform;
        rect.anchorMin = src.anchorMin;
        rect.anchorMax = src.anchorMax;
        rect.pivot = src.pivot;
        rect.sizeDelta = src.sizeDelta;
        Vector2 start = src.anchoredPosition;

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = source.preserveAspect;
        img.raycastTarget = false;

        for (float e = 0f; e < 0.7f; e += Time.unscaledDeltaTime)
        {
            float k = e / 0.7f;
            rect.anchoredPosition = start + Vector2.down * 160f * k * k;
            rect.localRotation = Quaternion.Euler(0f, 0f, -25f * k);
            img.color = new Color(1f, 0.25f, 0.25f, 1f - k);
            yield return null;
        }

        Object.Destroy(go);
    }

    public static IEnumerator FlashImage(Image image, Color flash, float time)
    {
        for (float e = 0f; e < time && image != null; e += Time.unscaledDeltaTime)
        {
            float k = e / time;
            image.color = Color.Lerp(flash, Color.white, k);
            image.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }

        if (image != null)
        {
            image.color = Color.white;
            image.transform.localScale = Vector3.one;
        }
    }

    public static IEnumerator Confetti(RectTransform parent, Color mainColor, int count = 70, float time = 3f)
    {
        Color[] palette = { mainColor, Color.white, Color.yellow, mainColor * 0.7f + Color.white * 0.3f };
        var pieces = new List<(RectTransform rect, Image img, Vector2 velocity, float spin)>();

        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Rect area = canvas != null ? ((RectTransform)canvas.rootCanvas.transform).rect : parent.rect;
        float halfWidth = area.width * 0.5f;
        float top = area.height * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Confetti", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(Random.Range(14f, 26f), Random.Range(8f, 16f));
            rect.anchoredPosition = new Vector2(Random.Range(-halfWidth, halfWidth), top + Random.Range(0f, 400f));
            var img = go.AddComponent<Image>();
            img.color = palette[Random.Range(0, palette.Length)];
            img.raycastTarget = false;
            pieces.Add((rect, img, new Vector2(Random.Range(-60f, 60f), -Random.Range(250f, 450f)), Random.Range(-360f, 360f)));
        }

        for (float e = 0f; e < time; e += Time.unscaledDeltaTime)
        {
            float dt = Time.unscaledDeltaTime;
            foreach (var p in pieces)
            {
                if (p.rect == null) continue;
                p.rect.anchoredPosition += p.velocity * dt + Vector2.right * Mathf.Sin((e + p.spin) * 3f) * 40f * dt;
                p.rect.Rotate(0f, 0f, p.spin * dt);
                if (e > time - 0.6f)
                    p.img.color = new Color(p.img.color.r, p.img.color.g, p.img.color.b, (time - e) / 0.6f);
            }
            yield return null;
        }

        foreach (var p in pieces)
            if (p.rect != null) Object.Destroy(p.rect.gameObject);
    }

    private static readonly Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public static void Tint(GameObject root, Color color, float amount)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;

            Material[] mats = r.sharedMaterials;
            if (!originalColors.TryGetValue(r, out Color[] originals))
            {
                originals = new Color[mats.Length];
                for (int i = 0; i < mats.Length; i++)
                    originals[i] = mats[i] != null && mats[i].HasProperty(BaseColorId) ? mats[i].GetColor(BaseColorId) : Color.clear;
                originalColors[r] = originals;
            }

            for (int i = 0; i < mats.Length && i < originals.Length; i++)
            {
                if (originals[i] == Color.clear) continue;
                block.Clear();
                block.SetColor(BaseColorId, Color.Lerp(originals[i], color, amount));
                r.SetPropertyBlock(block, i);
            }
        }
    }

    public static void ClearTint(GameObject root)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;

            block.Clear();
            for (int i = 0; i < r.sharedMaterials.Length; i++)
                r.SetPropertyBlock(block, i);
        }
    }

    private static readonly Dictionary<GameObject, int> highlightCounts = new Dictionary<GameObject, int>();

    public static void Highlight(GameObject target, bool on)
    {
        if (target == null) return;

        highlightCounts.TryGetValue(target, out int count);
        count += on ? 1 : -1;

        if (count <= 0)
        {
            highlightCounts.Remove(target);
            ClearTint(target);
        }
        else
        {
            highlightCounts[target] = count;
        }
    }

    public static void PulseHighlight(GameObject target)
    {
        if (target != null && highlightCounts.ContainsKey(target))
            Tint(target, Color.white, 0.25f + 0.15f * Mathf.Sin(Time.time * 6f));
    }
}
