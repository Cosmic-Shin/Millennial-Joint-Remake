using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectFirstOnEnable : MonoBehaviour
{
    [Tooltip("Optional; defaults to the first active button in the panel")]
    public Selectable firstSelected;

    private GameObject lastSelected;
    private Selectable focusOnce;
    private bool freshOpen;

    public void FocusNextOpen(Selectable target)
    {
        focusOnce = target;
    }

    private void OnEnable()
    {
        lastSelected = null;
        freshOpen = true;
        StartCoroutine(SelectNextFrame());
    }

    private void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null && current.activeInHierarchy && current.transform.IsChildOf(transform))
            lastSelected = current;
        else if (current == null || !current.activeInHierarchy)
            Select();
    }

    private IEnumerator SelectNextFrame()
    {
        yield return null;
        Select();
    }

    private void Select()
    {
        Selectable target = focusOnce != null && focusOnce.IsActive() ? focusOnce : null;
        focusOnce = null;
        if (target == null && lastSelected != null && lastSelected.activeInHierarchy)
            target = lastSelected.GetComponent<Selectable>();
        if (target == null)
            target = firstSelected != null && firstSelected.IsActive() ? firstSelected : FirstActiveSelectable();

        if (target == null || EventSystem.current == null)
            return;

        lastSelected = target.gameObject;
        if (EventSystem.current.currentSelectedGameObject == target.gameObject)
        {
            if (!freshOpen)
                return;
            EventSystem.current.SetSelectedGameObject(null);
        }
        freshOpen = false;
        EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    private Selectable FirstActiveSelectable()
    {
        foreach (Selectable s in GetComponentsInChildren<Selectable>())
        {
            if (s.IsActive() && s.IsInteractable() && s.navigation.mode != Navigation.Mode.None)
                return s;
        }

        return null;
    }
}

public static class MenuFocus
{
    public static bool PointerMoved(PointerEventData eventData) =>
        eventData != null && eventData.delta.sqrMagnitude > 0.01f;

    public static void Hover(GameObject go)
    {
        Selectable target = StepperRow.Owner(go);
        if (target == null)
            target = go.GetComponent<Selectable>();

        if (target != null && target.IsInteractable() && target.navigation.mode != Navigation.Mode.None && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}
