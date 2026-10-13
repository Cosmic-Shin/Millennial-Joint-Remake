using UnityEngine;

[RequireComponent(typeof(Light))]
public class FireGlow : MonoBehaviour
{
    public float baseIntensity = 2.5f;
    public float flicker = 0.8f;
    public float speed = 9f;

    private Light glow;
    private float seed;

    private void Awake()
    {
        glow = GetComponent<Light>();
        seed = Random.value * 100f;
    }

    private void Update()
    {
        glow.intensity = baseIntensity + (Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f) * 2f * flicker;
    }
}
