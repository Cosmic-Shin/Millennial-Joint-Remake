using System;
using UnityEngine;

[Serializable]
public class Order
{
    public OrderItem[] items = new OrderItem[3];
    private bool[] served = new bool[3];

    public void GenerateRandomOrder()
    {
        items[0] = new OrderItem(GetRandomItemType());
        items[1] = new OrderItem(GetRandomItemType());
        items[2] = new OrderItem(GetRandomItemType());
        served[0] = false;
        served[1] = false;
        served[2] = false;
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
            OrderItemType.StrawberryIceCream,  // ADD THESE
            OrderItemType.BubblegumIceCream,   // ADD THESE
            OrderItemType.MangoIceCream        // ADD THESE
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

    public bool IsComplete()
    {
        return served[0] && served[1] && served[2];
    }

    public bool IsServed(int index)
    {
        if (index < 0 || index >= served.Length)
            return false;

        return served[index];
    }

    public OrderItem GetItem(int index)
    {
        if (index < 0 || index >= items.Length)
            return null;

        return items[index];
    }

    public string GetDisplayText()
    {
        string text = "Order:\n";
        text += (served[0] ? "[✓] " : "[ ] ") + items[0].GetDisplayName() + "\n";
        text += (served[1] ? "[✓] " : "[ ] ") + items[1].GetDisplayName() + "\n";
        text += (served[2] ? "[✓] " : "[ ] ") + items[2].GetDisplayName();
        return text;
    }
}