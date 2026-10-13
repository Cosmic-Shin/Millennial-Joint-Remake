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

    protected static float CookRate(PlayerControl user)
    {
        return PowerUpManager.Instance != null ? 1f / PowerUpManager.Instance.GetCookingTimeMultiplier(user) : 1f;
    }

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