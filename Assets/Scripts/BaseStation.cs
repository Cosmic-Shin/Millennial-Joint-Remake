using UnityEngine;

public class BaseStation : MonoBehaviour
{
    [Header("UI Feedback")]
    public InteractionPanel interactionPanel;

    [Header("Panel Animation")]
    public float animationTime = 0.5f;

    [Header("Cooking FX")]
    [Tooltip("Soft particle material for the cooking steam and the done sparkle (none = no effects)")]
    public Material fxMaterial;
    public Vector3 fxOffset = new Vector3(0f, 1.1f, 0f);

    private AudioSource burnAudio;
    private ParticleSystem steam;
    private ParticleSystem sparkle;

    protected void SetSteam(bool on)
    {
        if (fxMaterial == null)
            return;

        if (steam == null)
        {
            steam = CreateFx("Steam");
            var main = steam.main;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new Color(1f, 1f, 1f, 0.6f);
            main.gravityModifier = -0.05f;

            var emission = steam.emission;
            emission.rateOverTime = 10f;

            var shape = steam.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.2f;

            var size = steam.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

            var color = steam.colorOverLifetime;
            color.enabled = true;
            color.color = Fade(Color.white, 0.2f);
        }

        if (on && !steam.isPlaying)
            steam.Play();
        else if (!on && steam.isPlaying)
            steam.Stop();
    }

    protected void Sparkle()
    {
        if (fxMaterial == null)
            return;

        if (sparkle == null)
        {
            sparkle = CreateFx("Sparkle");
            var main = sparkle.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.2f, 1.3f, 0.2f), new Color(2.2f, 1.8f, 0.4f));
            main.gravityModifier = 0.6f;

            var emission = sparkle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = sparkle.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f;

            var size = sparkle.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        }

        sparkle.Play();
    }

    private ParticleSystem CreateFx(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = fxOffset;
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = fxMaterial;
        return ps;
    }

    private static Gradient Fade(Color color, float fadeInEnd)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeInEnd), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    protected void SetBurnSizzle(bool on, float intensity = 0f)
    {
        AudioClip clip = AudioManager.Instance != null ? AudioManager.Instance.BurnSizzleClip : null;

        if (!on || clip == null || Time.timeScale == 0f)
        {
            if (burnAudio != null && burnAudio.isPlaying)
                burnAudio.Stop();
            return;
        }

        if (burnAudio == null)
        {
            burnAudio = gameObject.AddComponent<AudioSource>();
            burnAudio.loop = true;
            burnAudio.playOnAwake = false;
            burnAudio.spatialBlend = 0f;
        }

        burnAudio.clip = clip;
        burnAudio.volume = AudioManager.Instance.GetSFXVolume() * Mathf.Lerp(0.25f, 0.8f, intensity);

        if (!burnAudio.isPlaying)
            burnAudio.Play();
    }

    private Transform bounceTarget;
    private Vector3 bounceBaseScale;
    private Coroutine bounceRoutine;

    // Little jelly squash on the station model when something is placed or taken.
    public void Bounce(float amount = 0.12f)
    {
        if (!isActiveAndEnabled)
            return;

        if (bounceTarget == null)
        {
            bounceTarget = transform.Find("Space Visual");
            if (bounceTarget == null)
                return;
            bounceBaseScale = bounceTarget.localScale;
        }

        if (bounceRoutine != null)
            StopCoroutine(bounceRoutine);
        bounceRoutine = StartCoroutine(VisualFx.Squash(bounceTarget, bounceBaseScale, amount, 0.28f));
    }

    private float lockedUntil;
    public bool IsLocked => Time.time < lockedUntil;
    private static readonly Color LockColor = new Color(0.25f, 0.2f, 0.35f);

    // Versus sabotage: nobody can use this station for a while.
    public void Lock(float seconds)
    {
        lockedUntil = Time.time + seconds;
        StartCoroutine(LockRoutine());
    }

    private System.Collections.IEnumerator LockRoutine()
    {
        Bounce(-0.25f);
        VisualFx.Puff(transform.position + fxOffset, LockColor, 14, 0.4f, 1.4f);
        AudioManager.Instance?.PlayNopeSFX();

        // Re-tint every frame: highlight and other tints elsewhere would otherwise wipe it.
        while (IsLocked)
        {
            VisualFx.Tint(gameObject, LockColor, 0.55f + 0.15f * Mathf.Sin(Time.time * 6f));
            yield return null;
        }

        VisualFx.ClearTint(gameObject);
        Bounce(0.15f);
        Sparkle();
    }

    // Cooked food waiting to be grabbed throbs faster and harder as it nears burning. heat 0 = rest.
    // Starts at 30% heat so it never fights the "just cooked" pop; resets once when it stops.
    private bool throbbing;
    protected void ThrobFood(KitchenItemVisualizer visualizer, float heat)
    {
        Transform food = visualizer != null ? visualizer.CurrentVisual : null;
        if (food == null)
            return;

        float h = Mathf.Clamp01((heat - 0.3f) / 0.7f);
        if (h <= 0f)
        {
            if (throbbing)
                food.localScale = visualizer.VisualScale;
            throbbing = false;
            return;
        }

        throbbing = true;
        food.localScale = visualizer.VisualScale * (1f + 0.12f * h * Mathf.Abs(Mathf.Sin(Time.time * (5f + 15f * h))));
    }

    // Food burned to nothing: sooty cloud plus a sad slump.
    protected void BurnPoof()
    {
        VisualFx.Puff(transform.position + fxOffset, new Color(0.15f, 0.13f, 0.12f, 0.9f), 18, 0.5f, 1.6f);
        Bounce(-0.25f);
        AudioManager.Instance?.PlayNopeSFX();
    }

    private const float CookRingWorldSize = 1.3f;
    private static Sprite ringSprite;
    private UnityEngine.UI.Image cookRing;

    // Timer ring around the cooking icon. A child of the icon, so it hides with it.
    protected void SetCookRing(UnityEngine.UI.Image icon, float fill, Color color)
    {
        if (icon == null)
            return;

        if (cookRing == null)
        {
            if (ringSprite == null)
                ringSprite = MakeRingSprite();
            cookRing = new GameObject("Cook Ring", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            RectTransform rt = cookRing.rectTransform, irt = icon.rectTransform;
            rt.SetParent(irt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            // Same round world size on every station, whatever the icon's scale or shape.
            Vector3 s = irt.lossyScale;
            rt.sizeDelta = new Vector2(CookRingWorldSize / s.x, CookRingWorldSize / s.y);
            cookRing.sprite = ringSprite;
            cookRing.type = UnityEngine.UI.Image.Type.Filled;
            cookRing.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            cookRing.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            cookRing.fillClockwise = true;
            cookRing.raycastTarget = false;
            icon.enabled = false; // the ring replaces the old animated icon
        }

        fill = Mathf.Clamp01(fill);
        cookRing.fillAmount = fill;
        cookRing.color = color;
        // Heartbeat over the last 20% so "almost done" reads at a glance.
        float beat = Mathf.Clamp01((fill - 0.8f) / 0.2f) * Mathf.Abs(Mathf.Sin(Time.time * 10f));
        cookRing.rectTransform.localScale = Vector3.one * (1f + 0.12f * beat);
    }

    // White ring with dark ink edges, drawn once.
    private static Sprite MakeRingSprite()
    {
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                float a = Mathf.Clamp01((1f - r) * n / 2f) * Mathf.Clamp01((r - 0.7f) * n / 2f);
                bool ink = r < 0.76f || r > 0.94f;
                byte v = ink ? (byte)30 : (byte)255;
                px[y * n + x] = new Color32(v, v, v, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    protected static float CookRate(PlayerControl user)
    {
        return PowerUpManager.Instance != null ? 1f / PowerUpManager.Instance.GetCookingTimeMultiplier(user) : 1f;
    }

    protected static bool BurnProof(PlayerControl user) =>
        PowerUpManager.Instance != null && PowerUpManager.Instance.HasInstaCook(user);

    public void OpenPanel(bool show)
    {
        if (interactionPanel == null) return;
        if (interactionPanel.panelCanvas == null) return;

        LeanTween.cancel(interactionPanel.panelCanvas.gameObject);

        if (show)
        {
            interactionPanel.panelCanvas.gameObject.SetActive(true);
            interactionPanel.panelCanvas.transform.localScale = Vector3.zero;
            interactionPanel.panelCanvas.transform
                .LeanScale(Vector3.one, animationTime)
                .setEaseOutQuint();
        }
        else
        {
            AudioManager.Instance?.PlayDoneInteractingSFX();
            interactionPanel.panelCanvas.transform
                .LeanScale(Vector3.zero, animationTime)
                .setEaseOutQuint()
                .setOnComplete(() =>
                {
                    interactionPanel.panelCanvas.gameObject.SetActive(false);
                });
        }
    }

    public void Show(PlayerControl player, string message)
    {
        string fullMessage = message;
        if (player != null)
            fullMessage += "\n" + player.GetHeldItemDebug();

        if (interactionPanel != null)
            interactionPanel.ShowMessage(fullMessage);

        if (player != null)
            player.ShowFloatingText(message, player.infoTextColor);
    }
}