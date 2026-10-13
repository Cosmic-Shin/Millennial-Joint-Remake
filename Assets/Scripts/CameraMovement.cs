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

    public static CameraMovement Instance { get; private set; }

    private Quaternion baseRotation;
    private float introYawLeft;

    private Vector3 camBase;
    private float shakeAmount;
    private float shakeTimeLeft;
    private float shakeDuration;
    private bool hitStopping;

    private void Awake()
    {
        Instance = this;
        baseRotation = transform.rotation;
        camBase = Cam.localPosition;
    }

    public void PlayIntro()
    {
        camBase = new(0, 0, -IntroDistance);
        Cam.localPosition = camBase;
        introYawLeft = IntroYaw;
    }

    public void Shake(float amount, float duration)
    {
        if (shakeTimeLeft > 0f && amount < shakeAmount)
            return;

        shakeAmount = amount;
        shakeDuration = shakeTimeLeft = duration;
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

    private void Update()
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

        float lerpTime = LerpTime * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, pos, lerpTime);
        camBase = Vector3.Lerp(camBase, new(0, 0, -dis - DistanceOffset), lerpTime / 2);

        Vector3 shake = Vector3.zero;
        if (shakeTimeLeft > 0f)
        {
            shakeTimeLeft -= Time.unscaledDeltaTime;
            shake = (Vector3)Random.insideUnitCircle * shakeAmount * Mathf.Clamp01(shakeTimeLeft / shakeDuration);
        }

        Cam.localPosition = camBase + shake;

        if (introYawLeft != 0f)
        {
            introYawLeft = Mathf.Abs(introYawLeft) < 0.05f ? 0f : Mathf.Lerp(introYawLeft, 0f, lerpTime / 2);
            transform.rotation = Quaternion.Euler(0, introYawLeft, 0) * baseRotation;
        }
    }
}
