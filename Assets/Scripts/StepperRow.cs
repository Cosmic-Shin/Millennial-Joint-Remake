using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StepperRow : Selectable, ISubmitHandler
{
    public Button previous;
    public Button next;

    private static readonly Dictionary<GameObject, StepperRow> owners = new Dictionary<GameObject, StepperRow>();

    public static StepperRow Owner(GameObject arrow) =>
        owners.TryGetValue(arrow, out StepperRow row) && row != null ? row : null;

    protected override void Awake()
    {
        base.Awake();
        Register(previous);
        Register(next);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (previous != null) owners.Remove(previous.gameObject);
        if (next != null) owners.Remove(next.gameObject);
    }

    private void Register(Button arrow)
    {
        if (arrow == null)
            return;

        owners[arrow.gameObject] = this;
        Navigation none = arrow.navigation;
        none.mode = Navigation.Mode.None;
        arrow.navigation = none;
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left && previous != null)
        {
            previous.onClick.Invoke();
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Right && next != null)
        {
            next.onClick.Invoke();
            eventData.Use();
        }
        else
        {
            base.OnMove(eventData);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        next?.onClick.Invoke();
    }
}
