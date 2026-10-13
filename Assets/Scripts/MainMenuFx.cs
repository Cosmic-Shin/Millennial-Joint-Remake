using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuFx : MonoBehaviour
{
    [Header("Intro")]
    [Tooltip("Image set to Filled / Horizontal / Left")]
    public Image title;
    public float titleWriteTime = 1.1f;
    public RectTransform[] doodles;
    [Tooltip("Faded in one after another")]
    public CanvasGroup[] buttons;
    public float buttonStagger = 0.12f;

    [Header("Idle")]
    public float titleBobAmount = 8f;
    public float titleTiltDegrees = 1.2f;

    [Header("Chalk Dust")]
    public RectTransform dustArea;
    public Sprite dustSprite;
    public int dustCount = 26;

    [Header("Joystick Hint")]
    public GameObject joystickHint;

    private static bool introPlayed;

    private Vector2 titleBasePos;
    private bool hasBase;
    private float nextJoystickCheck;
    private readonly List<(RectTransform rect, Image img, Vector2 velocity, float phase)> dust =
        new List<(RectTransform, Image, Vector2, float)>();

    private void OnEnable()
    {
        if (!hasBase && title != null)
        {
            titleBasePos = title.rectTransform.anchoredPosition;
            hasBase = true;
        }

        if (dust.Count == 0)
            SpawnDust();

        StartCoroutine(Intro(!introPlayed));
        introPlayed = true;

        foreach (RectTransform doodle in doodles)
            if (doodle != null) StartCoroutine(WiggleLoop(doodle));
    }

    private IEnumerator Intro(bool full)
    {
        foreach (CanvasGroup b in buttons)
            if (b != null) b.alpha = 0f;

        if (title != null)
            title.fillAmount = full ? 0f : 1f;

        foreach (RectTransform doodle in doodles)
            if (doodle != null) doodle.localScale = full ? Vector3.zero : Vector3.one;

        if (full)
        {
            for (float t = 0f; t < titleWriteTime && title != null; t += Time.unscaledDeltaTime)
            {
                title.fillAmount = Mathf.SmoothStep(0f, 1f, t / titleWriteTime);
                yield return null;
            }

            if (title != null)
                title.fillAmount = 1f;

            foreach (RectTransform doodle in doodles)
                if (doodle != null) StartCoroutine(VisualFx.Pop(doodle, 0.01f, 0.35f));
        }

        float fade = full ? 0.3f : 0.15f;
        float stagger = full ? buttonStagger : buttonStagger * 0.4f;

        foreach (CanvasGroup b in buttons)
        {
            if (b == null) continue;
            StartCoroutine(FadeButton(b, fade));
            yield return new WaitForSecondsRealtime(stagger);
        }
    }

    private static IEnumerator FadeButton(CanvasGroup group, float time)
    {
        for (float t = 0f; t < time && group != null; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / time;
            yield return null;
        }

        if (group != null)
            group.alpha = 1f;
    }

    private IEnumerator WiggleLoop(RectTransform doodle)
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(2.5f, 4.5f));

            float[] angles = { -10f, 8f, -5f, 3f, 0f };
            float from = 0f;
            foreach (float to in angles)
            {
                for (float t = 0f; t < 0.08f; t += Time.unscaledDeltaTime)
                {
                    doodle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, t / 0.08f));
                    yield return null;
                }
                from = to;
            }

            doodle.localRotation = Quaternion.identity;
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        if (title != null && hasBase)
        {
            title.rectTransform.anchoredPosition = titleBasePos + Vector2.up * Mathf.Sin(time * 1.2f) * titleBobAmount;
            title.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.8f) * titleTiltDegrees);
        }

        UpdateDust(time);

        if (joystickHint != null && time >= nextJoystickCheck)
        {
            nextJoystickCheck = time + 0.5f;
            joystickHint.SetActive(JoystickConnected());
        }
    }

    private void SpawnDust()
    {
        if (dustArea == null || dustSprite == null)
            return;

        Rect area = dustArea.rect;
        for (int i = 0; i < dustCount; i++)
        {
            var go = new GameObject("ChalkDust", typeof(RectTransform));
            go.transform.SetParent(dustArea, false);
            var rect = (RectTransform)go.transform;
            float size = Random.Range(6f, 16f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));

            var img = go.AddComponent<Image>();
            img.sprite = dustSprite;
            img.raycastTarget = false;

            dust.Add((rect, img, new Vector2(Random.Range(-12f, 12f), Random.Range(6f, 20f)), Random.Range(0f, 10f)));
        }
    }

    private void UpdateDust(float time)
    {
        if (dust.Count == 0)
            return;

        Rect area = dustArea.rect;
        float dt = Time.unscaledDeltaTime;

        foreach (var d in dust)
        {
            Vector2 p = d.rect.anchoredPosition + (d.velocity + Vector2.right * Mathf.Sin(time + d.phase) * 8f) * dt;

            if (p.y > area.yMax) p.y = area.yMin;
            if (p.x > area.xMax) p.x = area.xMin;
            if (p.x < area.xMin) p.x = area.xMax;

            d.rect.anchoredPosition = p;
            d.img.color = new Color(1f, 1f, 1f, 0.12f + 0.12f * Mathf.Sin(time * 1.7f + d.phase));
        }
    }

    private static bool JoystickConnected()
    {
        foreach (string name in Input.GetJoystickNames())
            if (!string.IsNullOrEmpty(name)) return true;
        return false;
    }
}
