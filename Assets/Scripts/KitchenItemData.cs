using System;
using UnityEngine;

public enum ItemType
{
    None,
    Plate,
    Bun,
    Bread,
    VeggieRaw,
    VeggieChopped,
    PattyRaw,
    PattyCooked,
    HamRaw,
    HamCooked,
    Cheese,
    Cup,
    FrozenFries,
    FriesCooked,
    ChickenRaw,
    ChickenCooked
}

[Serializable]
public class KitchenItemData
{
    public ItemType type = ItemType.None;

    [Header("Plate Contents")]
    public bool plateHasBun;
    public bool plateHasBread;
    public bool plateHasPatty;   // order matches game rule: Bun ? Patty ? Veggie
    public bool plateHasVeggie;
    public bool plateHasHam;
    public bool plateHasCheese;
    public bool plateHasFries;
    public bool plateHasChicken;

    [Header("Cup Contents")]
    public bool cupHasSoda;
    public bool cupHasCoffee;

    public bool IsEmpty  => type == ItemType.None;
    public bool IsPlate  => type == ItemType.Plate;
    public bool IsCup    => type == ItemType.Cup;
    public bool IsSoda   => type == ItemType.Cup && cupHasSoda;
    public bool IsCoffee => type == ItemType.Cup && cupHasCoffee;

    public bool IsCompleteBurger =>
        type == ItemType.Plate &&
        plateHasBun &&
        plateHasPatty &&
        plateHasVeggie &&
        !plateHasFries &&
        !plateHasChicken &&
        !plateHasHam &&
        !plateHasCheese &&
        !plateHasBread;

    public bool IsCompleteSandwich =>
        type == ItemType.Plate &&
        plateHasBread &&
        plateHasHam &&
        plateHasCheese &&
        !plateHasBun &&
        !plateHasPatty &&
        !plateHasVeggie &&
        !plateHasFries &&
        !plateHasChicken;

    public bool IsCompleteFriedChicken =>
        type == ItemType.Plate &&
        plateHasChicken &&
        !plateHasBun &&
        !plateHasPatty &&
        !plateHasVeggie &&
        !plateHasFries &&
        !plateHasHam &&
        !plateHasCheese &&
        !plateHasBread;

    public bool IsCompleteFries =>
        type == ItemType.Plate &&
        plateHasFries &&
        !plateHasBun &&
        !plateHasPatty &&
        !plateHasVeggie &&
        !plateHasHam &&
        !plateHasCheese &&
        !plateHasBread &&
        !plateHasChicken;

    public bool IsCompleteDrink =>
        type == ItemType.Cup &&
        (cupHasSoda || cupHasCoffee);

    public void Set(ItemType newType)
    {
        type = newType;

        if (newType != ItemType.Plate)
        {
            plateHasBun      = false;
            plateHasBread    = false;
            plateHasPatty    = false;
            plateHasVeggie   = false;
            plateHasHam      = false;
            plateHasCheese   = false;
            plateHasFries    = false;
            plateHasChicken  = false;
        }

        if (newType != ItemType.Cup)
        {
            cupHasSoda        = false;
            cupHasCoffee      = false;
        }
    }

    public void MakePlate()
    {
        type              = ItemType.Plate;
        plateHasBun       = false;
        plateHasBread     = false;
        plateHasPatty     = false;
        plateHasVeggie    = false;
        plateHasHam       = false;
        plateHasCheese    = false;
        plateHasFries     = false;
        plateHasChicken   = false;
        cupHasSoda        = false;
        cupHasCoffee      = false;
    }

    public void Clear()
    {
        type               = ItemType.None;
        plateHasBun        = false;
        plateHasBread      = false;
        plateHasPatty      = false;
        plateHasVeggie     = false;
        plateHasHam        = false;
        plateHasCheese     = false;
        plateHasFries      = false;
        plateHasChicken    = false;
        cupHasSoda         = false;
        cupHasCoffee       = false;
    }

    public void CopyFrom(KitchenItemData other)
    {
        type               = other.type;
        plateHasBun        = other.plateHasBun;
        plateHasBread      = other.plateHasBread;
        plateHasPatty      = other.plateHasPatty;
        plateHasVeggie     = other.plateHasVeggie;
        plateHasHam        = other.plateHasHam;
        plateHasCheese     = other.plateHasCheese;
        plateHasFries      = other.plateHasFries;
        plateHasChicken    = other.plateHasChicken;
        cupHasSoda         = other.cupHasSoda;
        cupHasCoffee       = other.cupHasCoffee;
    }

    public bool IsValidPlateIngredient()
    {
        return type == ItemType.Bun ||
               type == ItemType.Bread ||
               type == ItemType.VeggieChopped ||
               type == ItemType.PattyCooked ||
               type == ItemType.HamCooked ||
               type == ItemType.Cheese ||
               type == ItemType.FriesCooked ||
               type == ItemType.ChickenCooked;
    }

    // FIX: display order now matches game rule (Bun ? Patty ? Veggie)
    public string GetDisplayName()
    {
        if (type == ItemType.Plate)
        {
            string result = "Plate";
            result += plateHasBun      ? " + Bun"           : "";
            result += plateHasBread    ? " + Bread"         : "";
            result += plateHasPatty    ? " + CookedPatty"   : "";
            result += plateHasVeggie   ? " + ChoppedVeggie" : "";
            result += plateHasHam      ? " + CookedHam"     : "";
            result += plateHasCheese   ? " + Cheese"        : "";
            result += plateHasFries    ? " + Fries"         : "";
            result += plateHasChicken  ? " + CookedChicken" : "";

            if (IsCompleteSandwich)
                result += " (Complete Sandwich)";
            else if (IsCompleteBurger)
                result += " (Complete Burger)";
            else if (IsCompleteFriedChicken)
                result += " (Complete Fried Chicken)";
            else if (IsCompleteFries)
                result += " (Complete Fries)";

            return result;
        }

        if (type == ItemType.Cup)
        {
            if (cupHasCoffee)
                return "Cup + Coffee";
            if (cupHasSoda)
                return "Cup + Soda";
            return "Empty Cup";
        }

        return type switch
        {
            ItemType.Bun => "Bun",
            ItemType.Bread => "Bread",
            ItemType.VeggieRaw => "Raw Veggie",
            ItemType.VeggieChopped => "Chopped Veggie",
            ItemType.PattyRaw => "Raw Patty",
            ItemType.PattyCooked => "Cooked Patty",
            ItemType.HamRaw => "Raw Ham",
            ItemType.HamCooked => "Cooked Ham",
            ItemType.Cheese => "Cheese",
            ItemType.FrozenFries => "Frozen Fries",
            ItemType.FriesCooked => "Cooked Fries",
            ItemType.ChickenRaw => "Raw Chicken",
            ItemType.ChickenCooked => "Cooked Chicken",
            _ => type.ToString(),
        };
    }
}
