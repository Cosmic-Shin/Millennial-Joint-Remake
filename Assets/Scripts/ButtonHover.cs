using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ButtonTMPHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI targetText;
    public Image targetImage;
    public float hoverScale = 1.15f;
    public float scaleSpeed = 12f;
    public bool scaleImageInstead = false; // Toggle between scaling text or image

    private Vector3 normalScale;
    private Vector3 targetScale;
    private Transform scaleTarget;

    private void Awake()
    {
        // Determine what to scale based on the toggle
        if (scaleImageInstead)
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            scaleTarget = targetImage?.transform;
        }
        else
        {
            if (targetText == null)
                targetText = GetComponentInChildren<TextMeshProUGUI>();
            scaleTarget = targetText?.transform;
        }

        if (scaleTarget != null)
        {
            normalScale = scaleTarget.localScale;
            targetScale = normalScale;
        }
    }

    private void Update()
    {
        if (scaleTarget == null) return;

        scaleTarget.localScale = Vector3.Lerp(
            scaleTarget.localScale,
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