using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ButtonTMPHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public TextMeshProUGUI targetText;
    public Image targetImage;
    public float hoverScale = 1.15f;
    public float scaleSpeed = 12f;
    public bool scaleImageInstead = false;

    private Vector3 normalScale;
    private Vector3 targetScale;
    private Transform scaleTarget;
    private bool hovered;
    private bool selected;

    private void Awake()
    {
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

    private void OnDisable()
    {
        hovered = false;
        selected = false;
        Refresh();
        if (scaleTarget != null)
            scaleTarget.localScale = normalScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => OnPointerMove(eventData);

    public void OnPointerMove(PointerEventData eventData)
    {
        if (hovered || !MenuFocus.PointerMoved(eventData))
            return;

        hovered = true;
        MenuFocus.Hover(gameObject);
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        Refresh();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        hovered = false;
        Refresh();
    }

    private void Refresh()
    {
        targetScale = hovered || selected ? normalScale * hoverScale : normalScale;
    }
}