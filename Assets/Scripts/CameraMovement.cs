using System.Collections;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform Cam;
    public float LerpTime;
    public Transform[] FollowGroup;
    public float DistanceOffset;
    public float ZoomLimit = 10;

    [Header("Round Start Fly-In")]
    public float IntroDistance = 32f;
    public float IntroYaw = 35f;

    [Header("Shake")]
    public float shakeFrequency = 22f;
    public float maxShakeRoll = 2.5f;

    public static CameraMovement Instance { get; private set; }

    private Quaternion baseRotation;
    private Quaternion camBaseRotation;
    private float introYawLeft;

    private Vector3 camBase;
    private float shakeAmount;
    private float trauma;
    private float traumaDecay;
    private float noiseSeed;
    private bool hitStopping;

    private void Awake()
    {
        Instance = this;
        baseRotation = transform.rotation;
        camBase = Cam.localPosition;
        camBaseRotation = Cam.localRotation;
        noiseSeed = Random.value * 100f;
    }

    public void PlayIntro()
    {
        camBase = new(0, 0, -IntroDistance);
        Cam.localPosition = camBase;
        introYawLeft = IntroYaw;
    }

    public void Shake(float amount, float duration)
    {
        if (trauma > 0f && amount < shakeAmount * trauma)
            return;

        shakeAmount = amount;
        trauma = 1f;
        traumaDecay = 1f / Mathf.Max(0.01f, duration);
    }

    public void HitStop(float seconds)
    {
        if (!hitStopping && Time.timeScale == 1f)
            StartCoroutine(HitStopRoutine(seconds));
    }

    private IEnumerator HitStopRoutine(float seconds)
    {
        hitStopping = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);

        if (Time.timeScale == 0.05f)
            Time.timeScale = 1f;

        hitStopping = false;
    }

    private static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);

    private void LateUpdate()
    {
        Vector3 pos = Vector3.zero;
        float dis = ZoomLimit;

        foreach (Transform t in FollowGroup)
        {
            pos += t.position;
            float d = Vector3.Distance(transform.position, t.position);
            if (d > dis && d > ZoomLimit) dis = d;
        }

        pos /= FollowGroup.Length;

        float follow = Damp(LerpTime, Time.deltaTime);
        float zoom = Damp(LerpTime / 2f, Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, pos, follow);
        camBase = Vector3.Lerp(camBase, new(0, 0, -dis - DistanceOffset), zoom);

        Vector3 shake = Vector3.zero;
        float roll = 0f;
        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - traumaDecay * Time.unscaledDeltaTime);
            float power = trauma * trauma;
            float t = Time.unscaledTime * shakeFrequency;
            shake = new Vector3(Noise(t, 0f), Noise(t, 1f), 0f) * shakeAmount * power;
            roll = Noise(t, 2f) * maxShakeRoll * Mathf.Clamp01(shakeAmount / 0.45f) * power;
        }

        Cam.localPosition = camBase + shake;
        Cam.localRotation = camBaseRotation * Quaternion.Euler(0f, 0f, roll);

        if (introYawLeft != 0f)
        {
            introYawLeft = Mathf.Abs(introYawLeft) < 0.05f ? 0f : Mathf.Lerp(introYawLeft, 0f, zoom);
            transform.rotation = Quaternion.Euler(0, introYawLeft, 0) * baseRotation;
        }
    }

    private float Noise(float t, float channel) => Mathf.PerlinNoise(t, noiseSeed + channel * 17f) * 2f - 1f;
}
