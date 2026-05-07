using System.Collections;
using UnityEngine;
using TMPro;

public class WorldTextFade : MonoBehaviour
{
    public float fadeInTime = 0.3f;
    public float stayTime = 0.5f;
    public float fadeOutTime = 0.8f;
    public float floatSpeed = 1f;
    private Camera mainCam;

    private TextMeshPro textMesh;

    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshPro>();
        mainCam = Camera.main;
    }

    public void Play(string text, Color color)
    {
        if (textMesh == null) return;

        textMesh.text = text;
        textMesh.color = new Color(color.r, color.g, color.b, 0f);

        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float timer = 0f;

        // 🔥 FADE IN
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            SetAlpha(timer / fadeInTime);
            FloatUp();
            yield return null;
        }

        SetAlpha(1f);

        // 🔥 STAY
        yield return new WaitForSeconds(stayTime);

        // 🔥 FADE OUT
        timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            SetAlpha(1f - (timer / fadeOutTime));
            FloatUp();
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float a)
    {
        Color c = textMesh.color;
        c.a = a;
        textMesh.color = c;
    }

    private void LateUpdate()
    {
        if (mainCam == null) return;

        // 🔥 Make it face the camera
        transform.forward = mainCam.transform.forward;
    }

    private void FloatUp()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
    }
}