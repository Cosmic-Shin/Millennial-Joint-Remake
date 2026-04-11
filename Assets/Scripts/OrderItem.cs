using System;
using UnityEngine;

public enum OrderItemType
{
    Burger,
    BaconBurger,
    FriedChicken,
    Soda,
    Boba,
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

        if (type == OrderItemType.BaconBurger)
            return item.IsPlate && item.IsCompleteBaconBurger;

        if (type == OrderItemType.FriedChicken)
            return item.IsPlate && item.IsCompleteFriedChicken;

        if (type == OrderItemType.Soda)
            return item.IsCup && item.cupHasSoda && !item.cupHasBoba && !item.cupBobaDrinkReady;

        if (type == OrderItemType.Boba)
            return item.IsCup && item.cupBobaDrinkReady;

        if (type == OrderItemType.Fries)
            return item.IsPlate && item.IsCompleteFries;

        return false;
    }

    public string GetDisplayName()
    {
        return type switch
        {
            OrderItemType.Burger => "Burger",
            OrderItemType.BaconBurger => "Bacon Burger",
            OrderItemType.FriedChicken => "Fried Chicken",
            OrderItemType.Soda => "Soda",
            OrderItemType.Boba => "Boba",
            OrderItemType.Fries => "Fries",
            _ => "Unknown"
        };
    }
}
