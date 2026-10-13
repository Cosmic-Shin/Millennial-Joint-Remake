using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChalkButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("Image set to Filled / Horizontal / Left; fillAmount is animated")]
    public Image underline;
    public float drawTime = 0.15f;

    [Tooltip("Optional; defaults to AudioManager's chalk hover sound")]
    public AudioClip hoverSound;

    private float target;

    private void OnEnable()
    {
        target = 0f;
        if (underline != null)
            underline.fillAmount = 0f;
    }

    private void Update()
    {
        if (underline != null)
            underline.fillAmount = Mathf.MoveTowards(underline.fillAmount, target, Time.unscaledDeltaTime / drawTime);
    }

    public void OnPointerEnter(PointerEventData eventData) => SetDrawn(true);
    public void OnPointerExit(PointerEventData eventData) => SetDrawn(false);
    public void OnSelect(BaseEventData eventData) => SetDrawn(true);
    public void OnDeselect(BaseEventData eventData) => SetDrawn(false);

    private void SetDrawn(bool drawn)
    {
        if (drawn && target < 1f)
        {
            if (hoverSound != null)
                AudioManager.Instance?.PlayOneShot(hoverSound);
            else
                AudioManager.Instance?.PlayChalkHoverSFX();
        }

        target = drawn ? 1f : 0f;
    }
}
