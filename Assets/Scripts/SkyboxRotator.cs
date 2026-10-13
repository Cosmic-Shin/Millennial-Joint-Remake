using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float degreesPerSecond = 1f;

    private Material sky;

    private void Start()
    {
        sky = new Material(RenderSettings.skybox);
        RenderSettings.skybox = sky;
    }

    private void Update()
    {
        sky.SetFloat("_Rotation", Time.unscaledTime * degreesPerSecond % 360f);
    }
}
