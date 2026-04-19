using System;
using UnityEngine;

public enum OrderItemType
{
    Burger,
    Sandwich,
    FriedChicken,
    Soda,
    Coffee,
    Fries
}

[Serializable]
public class OrderItem
{
    public OrderItemType type;

    public OrderItem(OrderItemType itemType)
    {
        type = itemType;
    }

    public bool IsMatching(KitchenItemData item)
    {
        if (item.IsEmpty) return false;

        if (type == OrderItemType.Burger)
            return item.IsPlate && item.IsCompleteBurger;

        if (type == OrderItemType.Sandwich)
            return item.IsPlate && item.IsCompleteSandwich;

        if (type == OrderItemType.FriedChicken)
            return item.IsPlate && item.IsCompleteFriedChicken;

        if (type == OrderItemType.Soda)
            return item.IsCup && item.cupHasSoda;

        if (type == OrderItemType.Coffee)
            return item.IsCup && item.cupHasCoffee;

        if (type == OrderItemType.Fries)
            return item.IsPlate && item.IsCompleteFries;

        return false;
    }

    public string GetDisplayName()
    {
        return type switch
        {
            OrderItemType.Burger => "Burger",
            OrderItemType.Sandwich => "Sandwich",
            OrderItemType.FriedChicken => "Fried Chicken",
            OrderItemType.Soda => "Soda",
            OrderItemType.Coffee => "Coffee",
            OrderItemType.Fries => "Fries",
            _ => "Unknown"
        };
    }
}
