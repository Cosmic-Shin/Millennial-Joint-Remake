using System;
using UnityEngine;

[Serializable]
public class Order
{
    public OrderItem[] items = new OrderItem[2];
    private bool[] served = new bool[2];

    public Order()
    {
        items[0] = null;
        items[1] = null;
        served[0] = false;
        served[1] = false;
    }

    public void GenerateRandomOrder()
    {
        items[0] = new OrderItem(GetRandomItemType());
        items[1] = new OrderItem(GetRandomItemType());
        served[0] = false;
        served[1] = false;
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
            OrderItemType.Coffee
        };
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    public OrderItemType? TryServeItem(KitchenItemData item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || served[i]) continue;
            if (items[i].IsMatching(item))
            {
                served[i] = true;
                return items[i].type;
            }
        }
        return null;
    }

    public bool IsComplete() => served[0] && served[1];

    public int GetServedCount() => (served[0] ? 1 : 0) + (served[1] ? 1 : 0);

    public string GetDisplayText()
    {
        string text = "Order:\n";
        text += (served[0] ? "[✓] " : "[ ] ") + items[0].GetDisplayName() + "\n";
        text += (served[1] ? "[✓] " : "[ ] ") + items[1].GetDisplayName();
        return text;
    }
}