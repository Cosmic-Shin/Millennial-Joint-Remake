using System;
using UnityEngine;

[Serializable]
public class QueuedOrder
{
    public OrderItem item;
    public float timer;
    public const float TIMER_DURATION = 45f;
    public const float VERSUS_TIMER_DURATION = 60f;
    private bool served = false;

    public QueuedOrder(float duration)
    {
        item = new OrderItem(GetRandomItemType());
        timer = duration;
        served = false;
    }

    private OrderItemType GetRandomItemType()
    {
        var types = new[]
        {
            OrderItemType.Burger,
            OrderItemType.Sandwich,
            OrderItemType.FriedChicken,
            OrderItemType.Fries,
            OrderItemType.Soda,
            OrderItemType.IceTea,
            OrderItemType.OrangeJuice,
            OrderItemType.Coffee,
            OrderItemType.ChiliDog,
            OrderItemType.StrawberryIceCream,
            OrderItemType.BubblegumIceCream,
            OrderItemType.MangoIceCream
        };

        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    public void UpdateTimer(float deltaTime)
    {
        if (timer > 0f)
            timer -= deltaTime;
    }

    public bool IsExpired()
    {
        return timer <= 0f;
    }

    public bool TryServe(KitchenItemData item)
    {
        if (served)
            return false;

        if (this.item.IsMatching(item))
        {
            served = true;
            return true;
        }

        return false;
    }

    public bool IsServed()
    {
        return served;
    }

    public string GetDisplayName()
    {
        return item.GetDisplayName();
    }
}
