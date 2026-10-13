using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectFirstOnEnable : MonoBehaviour
{
    [Tooltip("Optional; defaults to the first active button in the panel")]
    public Selectable firstSelected;

    private void OnEnable()
    {
        StartCoroutine(SelectNextFrame());
    }

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && JoystickConnected())
            Select();
    }

    private IEnumerator SelectNextFrame()
    {
        yield return null;

        if (JoystickConnected())
            Select();
    }

    private void Select()
    {
        Selectable target = firstSelected != null && firstSelected.IsActive() ? firstSelected : FirstActiveSelectable();

        if (target != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    private Selectable FirstActiveSelectable()
    {
        foreach (Selectable s in GetComponentsInChildren<Selectable>())
        {
            if (s.IsActive() && s.IsInteractable())
                return s;
        }

        return null;
    }

    private static bool JoystickConnected()
    {
        foreach (string name in Input.GetJoystickNames())
        {
            if (!string.IsNullOrEmpty(name))
                return true;
        }

        return false;
    }
}
