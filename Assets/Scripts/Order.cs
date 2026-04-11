using System;
using System.Collections.Generic;
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
        // Random order for 2 people - can be any combination of items
        items[0] = new OrderItem(GetRandomItemType());
        items[1] = new OrderItem(GetRandomItemType());
        served[0] = false;
        served[1] = false;
    }

    private OrderItemType GetRandomItemType()
    {
        var types = new[] {
            OrderItemType.Burger,
            OrderItemType.BaconBurger,
            OrderItemType.FriedChicken,
            OrderItemType.Soda,
            OrderItemType.Boba,
            OrderItemType.Fries };
        return types[UnityEngine.Random.Range(0, types.Length)];
    }

    public OrderItemType? TryServeItem(KitchenItemData item)
    {
        // Check if this item matches any unserved order item
        for (int i = 0; i < 2; i++)
        {
            if (!served[i] && items[i].IsMatching(item))
            {
                served[i] = true;
                return items[i].type;
            }
        }
        return null;
    }

    public bool IsComplete()
    {
        return served[0] && served[1];
    }

    public int GetServedCount()
    {
        return (served[0] ? 1 : 0) + (served[1] ? 1 : 0);
    }

    public string GetDisplayText()
    {
        string text = "Order:\n";
        text += (served[0] ? "[✓] " : "[ ] ") + items[0].GetDisplayName() + "\n";
        text += (served[1] ? "[✓] " : "[ ] ") + items[1].GetDisplayName();
        return text;
    }
}
