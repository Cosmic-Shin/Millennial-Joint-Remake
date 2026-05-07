using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTMPHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI targetText;
    public float hoverScale = 1.15f;
    public float scaleSpeed = 12f;

    private Vector3 normalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshProUGUI>();

        if (targetText != null)
        {
            normalScale = targetText.transform.localScale;
            targetScale = normalScale;
        }
    }

    private void Update()
    {
        if (targetText == null) return;

        targetText.transform.localScale = Vector3.Lerp(
            targetText.transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;
    }
}