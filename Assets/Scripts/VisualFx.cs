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

        // Crumples into a ball, then gets tossed up and away like a binned receipt.
        float side = Random.value < 0.5f ? -1f : 1f;
        const float crumple = 0.25f, toss = 0.75f;
        for (float e = 0f; e < crumple + toss; e += Time.unscaledDeltaTime)
        {
            float c = Mathf.Clamp01(e / crumple);
            float jitter = Mathf.Sin(e * 90f) * 0.08f * (1f - c);
            rect.localScale = new Vector3(Mathf.Lerp(1f, 0.35f, c) + jitter, Mathf.Lerp(1f, 0.4f, c) - jitter, 1f);

            float k = Mathf.Clamp01((e - crumple) / toss);
            rect.anchoredPosition = start + new Vector2(side * 260f * k, 140f * k - 520f * k * k);
            rect.localRotation = Quaternion.Euler(0f, 0f, -side * 540f * k + jitter * 40f);
            img.color = new Color(1f, Mathf.Lerp(0.6f, 0.25f, c), Mathf.Lerp(0.55f, 0.25f, c), 1f - k * k);
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

    // Reused buffers: Tint runs every frame for highlights, lockouts and reversed chefs,
    // and GetComponentsInChildren / sharedMaterials each allocated a fresh array per call.
    private static readonly List<Renderer> rendererBuffer = new List<Renderer>();
    private static readonly List<Material> materialBuffer = new List<Material>();

    public static void Tint(GameObject root, Color color, float amount)
    {
        root.GetComponentsInChildren(rendererBuffer);
        foreach (Renderer r in rendererBuffer)
        {
            if (r is ParticleSystemRenderer) continue;

            r.GetSharedMaterials(materialBuffer);
            if (!originalColors.TryGetValue(r, out Color[] originals))
            {
                originals = new Color[materialBuffer.Count];
                for (int i = 0; i < materialBuffer.Count; i++)
                    originals[i] = materialBuffer[i] != null && materialBuffer[i].HasProperty(BaseColorId) ? materialBuffer[i].GetColor(BaseColorId) : Color.clear;
                originalColors[r] = originals;
            }

            for (int i = 0; i < originals.Length && i < materialBuffer.Count; i++)
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
        root.GetComponentsInChildren(rendererBuffer);
        foreach (Renderer r in rendererBuffer)
        {
            if (r is ParticleSystemRenderer) continue;

            block.Clear();
            r.GetSharedMaterials(materialBuffer);
            for (int i = 0; i < materialBuffer.Count; i++)
                r.SetPropertyBlock(block, i);
        }
    }

    private static readonly Dictionary<GameObject, int> highlightCounts = new Dictionary<GameObject, int>();
    private static readonly Dictionary<GameObject, List<GameObject>> highlightOutlines = new Dictionary<GameObject, List<GameObject>>();

    public static Material HighlightOutlineMaterial;

    public static void Highlight(GameObject target, bool on)
    {
        if (target == null) return;

        highlightCounts.TryGetValue(target, out int count);
        count += on ? 1 : -1;

        if (count <= 0)
        {
            highlightCounts.Remove(target);
            ClearTint(target);
            SetOutline(target, false);
        }
        else
        {
            highlightCounts[target] = count;
            SetOutline(target, true);
        }
    }

    private static void SetOutline(GameObject target, bool on)
    {
        if (highlightOutlines.TryGetValue(target, out List<GameObject> existing))
        {
            foreach (GameObject outline in existing)
                if (outline != null) outline.SetActive(on);
            return;
        }

        if (!on || HighlightOutlineMaterial == null)
            return;

        var outlines = new List<GameObject>();
        foreach (MeshRenderer r in target.GetComponentsInChildren<MeshRenderer>())
        {
            MeshFilter filter = r.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !r.enabled || r.name == "HighlightOutline" || r.name == "StationOutline") continue;

            var go = new GameObject("HighlightOutline");
            go.transform.SetParent(r.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var outline = go.AddComponent<MeshRenderer>();
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;

            var mats = new Material[filter.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = HighlightOutlineMaterial;
            outline.sharedMaterials = mats;
            outlines.Add(go);
        }

        highlightOutlines[target] = outlines;
    }

    public static void PulseHighlight(GameObject target)
    {
        if (target != null && highlightCounts.ContainsKey(target))
            Tint(target, Color.white, 0.25f + 0.15f * Mathf.Sin(Time.time * 6f));
    }

    public static IEnumerator Squash(Transform t, Vector3 baseScale, float amount, float time)
    {
        for (float e = 0f; e < time && t != null; e += Time.deltaTime)
        {
            float k = e / time;
            float s = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * amount;
            t.localScale = Vector3.Scale(baseScale, new Vector3(1f + s * 0.5f, 1f - s, 1f + s * 0.5f));
            yield return null;
        }

        if (t != null)
            t.localScale = baseScale;
    }

    public static Material FxMaterial;
    private static ParticleSystem puffSystem;

    // A thin wisp of steam rising from hot food. Lives on the food, so it goes wherever the food goes.
    public static void AddSteam(Transform food)
    {
        if (FxMaterial == null || food == null)
            return;

        var go = new GameObject("Food Steam");
        go.transform.SetParent(food, false);
        go.transform.localPosition = Vector3.up * 0.15f;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
        main.startColor = new Color(1f, 1f, 1f, 0.35f);
        main.gravityModifier = -0.03f;
        var emission = ps.emission;
        emission.rateOverTime = 4f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.05f;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = FxMaterial;
        ps.Play();
    }

    public static void Puff(Vector3 position, Color color, int count = 8, float size = 0.35f, float speed = 1.2f)
    {
        if (FxMaterial == null)
            return;

        if (puffSystem == null)
        {
            var go = new GameObject("PuffFx");
            puffSystem = go.AddComponent<ParticleSystem>();
            puffSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = puffSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.gravityModifier = -0.05f;
            main.maxParticles = 300;

            var emission = puffSystem.emission;
            emission.rateOverTime = 0f;
            var shape = puffSystem.shape;
            shape.enabled = false;

            var sizeOverLifetime = puffSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f));

            var colorOverLifetime = puffSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            var drag = puffSystem.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 4f;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = FxMaterial;
        }

        var emit = new ParticleSystem.EmitParams { startColor = color };
        for (int i = 0; i < count; i++)
        {
            Vector2 ring = Random.insideUnitCircle.normalized;
            emit.position = position + new Vector3(ring.x, 0f, ring.y) * 0.15f;
            emit.velocity = new Vector3(ring.x, Random.Range(0.2f, 0.6f), ring.y) * speed * Random.Range(0.6f, 1.2f);
            emit.startSize = size * Random.Range(0.7f, 1.3f);
            puffSystem.Emit(emit, 1);
        }
    }
}
