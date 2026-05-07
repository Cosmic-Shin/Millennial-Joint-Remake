using System;

public enum OrderItemType
{
    Burger,
    Sandwich,
    FriedChicken,
    Fries,
    Soda,
    IceTea,
    OrangeJuice,
    Coffee,
    ChiliDog,
    StrawberryIceCream,
    BubblegumIceCream,
    MangoIceCream
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
        if (item == null || item.IsEmpty) return false;

        if (type == OrderItemType.Burger) return item.IsPlate && item.IsCompleteBurger;
        if (type == OrderItemType.Sandwich) return item.IsPlate && item.IsCompleteSandwich;
        if (type == OrderItemType.FriedChicken) return item.IsPlate && item.IsCompleteFriedChicken;
        if (type == OrderItemType.Fries) return item.IsPlate && item.IsCompleteFries;
        if (type == OrderItemType.ChiliDog) return item.IsPlate && item.IsCompleteChiliDog;
        
        if (type == OrderItemType.Soda) return item.IsCup && item.cupHasSoda;
        if (type == OrderItemType.IceTea) return item.IsCup && item.cupHasIceTea;
        if (type == OrderItemType.OrangeJuice) return item.IsCup && item.cupHasOrangeJuice;
        if (type == OrderItemType.Coffee) return item.IsCup && item.cupHasCoffee;
        
        if (type == OrderItemType.StrawberryIceCream) return item.IsCup && item.coneHasStrawberryIceCream;
        if (type == OrderItemType.BubblegumIceCream) return item.IsCup && item.coneHasBubblegumIceCream;
        if (type == OrderItemType.MangoIceCream) return item.IsCup && item.coneHasMangoIceCream;

        return false;
    }

    public string GetDisplayName()
    {
        return type switch
        {
            OrderItemType.Burger => "Burger",
            OrderItemType.Sandwich => "Sandwich",
            OrderItemType.FriedChicken => "Fried Chicken",
            OrderItemType.Fries => "Fries",
            OrderItemType.Soda => "Soda",
            OrderItemType.IceTea => "Ice Tea",
            OrderItemType.OrangeJuice => "Orange Juice",
            OrderItemType.Coffee => "Coffee",
            OrderItemType.ChiliDog => "Chili Dog",
            OrderItemType.StrawberryIceCream => "Strawberry Ice Cream",
            OrderItemType.BubblegumIceCream => "Bubblegum Ice Cream",
            OrderItemType.MangoIceCream => "Mango Ice Cream",
            _ => "Unknown"
        };
    }
}