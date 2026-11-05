using System;
using UnityEngine;

// Scales the background particles under this object (stars, stardust, sprinkles) by the Particles option.
public class ParticleQuality : MonoBehaviour
{
    public static readonly string[] Names = { "Off", "Low", "Medium", "High" };
    private static readonly float[] Scale = { 0f, 0.25f, 0.6f, 1f };
    private static event Action Changed;

    public static int Level
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("ParticleLevel", 3), 0, 3);
        set
        {
            PlayerPrefs.SetInt("ParticleLevel", value);
            Changed?.Invoke();
        }
    }

    private ParticleSystem[] systems;
    private float[] baseRate;
    private int[] baseMax;

    private void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        baseRate = new float[systems.Length];
        baseMax = new int[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            baseRate[i] = systems[i].emission.rateOverTimeMultiplier;
            baseMax[i] = systems[i].main.maxParticles;
        }
    }

    private void OnEnable()
    {
        Changed += Apply;
        Apply();
    }

    private void OnDisable() => Changed -= Apply;

    private void Apply()
    {
        float s = Scale[Level];
        for (int i = 0; i < systems.Length; i++)
        {
            var emission = systems[i].emission;
            emission.rateOverTimeMultiplier = baseRate[i] * s;
            var main = systems[i].main;
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(baseMax[i] * s));
            if (s == 0f)
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else if (!systems[i].isPlaying)
                systems[i].Play();
        }
    }
}
