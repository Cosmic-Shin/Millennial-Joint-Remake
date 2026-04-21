using System;
using UnityEngine;

public enum ItemType
{
    None,
    Plate,
    Bun,
    Bread,
    VeggieRaw,
    PattyRaw,
    PattyCooked,
    HamRaw,
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
    public bool plateHasPatty;
    public bool plateHasVeggie;
    public bool plateHasHam;     // Ham & Cheese combined — placing Ham completes the sandwich
    public bool plateHasFries;
    public bool plateHasChicken;

    [Header("Cup Contents")]
    public bool cupHasSoda;
    public bool cupHasIceTea;
    public bool cupHasOrangeJuice;
    public bool cupHasCoffee;

    public bool IsEmpty  => type == ItemType.None;
    public bool IsPlate  => type == ItemType.Plate;
    public bool IsCup    => type == ItemType.Cup;
    public bool IsSoda        => type == ItemType.Cup && cupHasSoda;
    public bool IsIceTea      => type == ItemType.Cup && cupHasIceTea;
    public bool IsOrangeJuice => type == ItemType.Cup && cupHasOrangeJuice;
    public bool IsCoffee      => type == ItemType.Cup && cupHasCoffee;

    public bool IsCompleteBurger =>
        type == ItemType.Plate &&
        plateHasBun && plateHasPatty && plateHasVeggie &&
        !plateHasFries && !plateHasChicken &&
        !plateHasHam && !plateHasBread;

    // Sandwich is complete with just Bread + Ham (Ham & Cheese are one ingredient)
    public bool IsCompleteSandwich =>
        type == ItemType.Plate &&
        plateHasBread && plateHasHam &&
        !plateHasBun && !plateHasPatty && !plateHasVeggie &&
        !plateHasFries && !plateHasChicken;

    public bool IsCompleteFriedChicken =>
        type == ItemType.Plate &&
        plateHasChicken &&
        !plateHasBun && !plateHasPatty && !plateHasVeggie &&
        !plateHasFries && !plateHasHam && !plateHasBread;

    public bool IsCompleteFries =>
        type == ItemType.Plate &&
        plateHasFries &&
        !plateHasBun && !plateHasPatty && !plateHasVeggie &&
        !plateHasHam && !plateHasBread && !plateHasChicken;

    public bool IsCompleteDrink =>
        type == ItemType.Cup &&
        (cupHasSoda || cupHasIceTea || cupHasOrangeJuice || cupHasCoffee);

    public void Set(ItemType newType)
    {
        type = newType;

        if (newType != ItemType.Plate)
        {
            plateHasBun     = false;
            plateHasBread   = false;
            plateHasPatty   = false;
            plateHasVeggie  = false;
            plateHasHam     = false;
            plateHasFries   = false;
            plateHasChicken = false;
        }

        if (newType != ItemType.Cup)
        {
            cupHasSoda        = false;
            cupHasIceTea      = false;
            cupHasOrangeJuice = false;
            cupHasCoffee      = false;
        }
    }

    public void MakePlate()
    {
        type            = ItemType.Plate;
        plateHasBun     = false;
        plateHasBread   = false;
        plateHasPatty   = false;
        plateHasVeggie  = false;
        plateHasHam     = false;
        plateHasFries   = false;
        plateHasChicken = false;
        cupHasSoda        = false;
        cupHasIceTea      = false;
        cupHasOrangeJuice = false;
        cupHasCoffee      = false;
    }

    public void Clear()
    {
        type            = ItemType.None;
        plateHasBun     = false;
        plateHasBread   = false;
        plateHasPatty   = false;
        plateHasVeggie  = false;
        plateHasHam     = false;
        plateHasFries   = false;
        plateHasChicken = false;
        cupHasSoda        = false;
        cupHasIceTea      = false;
        cupHasOrangeJuice = false;
        cupHasCoffee      = false;
    }

    public void CopyFrom(KitchenItemData other)
    {
        type            = other.type;
        plateHasBun     = other.plateHasBun;
        plateHasBread   = other.plateHasBread;
        plateHasPatty   = other.plateHasPatty;
        plateHasVeggie  = other.plateHasVeggie;
        plateHasHam     = other.plateHasHam;
        plateHasFries   = other.plateHasFries;
        plateHasChicken = other.plateHasChicken;
        cupHasSoda        = other.cupHasSoda;
        cupHasIceTea      = other.cupHasIceTea;
        cupHasOrangeJuice = other.cupHasOrangeJuice;
        cupHasCoffee      = other.cupHasCoffee;
    }

    public bool IsValidPlateIngredient()
    {
        return type == ItemType.Bun ||
               type == ItemType.Bread ||
               type == ItemType.VeggieRaw ||
               type == ItemType.PattyCooked ||
               type == ItemType.HamRaw ||
               type == ItemType.FriesCooked ||
               type == ItemType.ChickenCooked;
    }

    public string GetDisplayName()
    {
        if (type == ItemType.Plate)
        {
            string result = "Plate";
            result += plateHasBun     ? " + Bun"           : "";
            result += plateHasBread   ? " + Bread"         : "";
            result += plateHasPatty   ? " + CookedPatty"   : "";
            result += plateHasVeggie  ? " + Veggie"        : "";
            result += plateHasHam     ? " + Ham & Cheese"  : "";
            result += plateHasFries   ? " + Fries"         : "";
            result += plateHasChicken ? " + CookedChicken" : "";

            if (IsCompleteSandwich)          result += " (Complete Sandwich)";
            else if (IsCompleteBurger)       result += " (Complete Burger)";
            else if (IsCompleteFriedChicken) result += " (Complete Fried Chicken)";
            else if (IsCompleteFries)        result += " (Complete Fries)";

            return result;
        }

        if (type == ItemType.Cup)
        {
            if (cupHasCoffee)      return "Coffee";
            if (cupHasIceTea)      return "Cup + Ice Tea";
            if (cupHasSoda)        return "Cup + Soda";
            if (cupHasOrangeJuice) return "Cup + Orange Juice";
            return "Empty Cup";
        }

        return type switch
        {
            ItemType.Bun           => "Bun",
            ItemType.Bread         => "Bread",
            ItemType.VeggieRaw     => "Veggie",
            ItemType.PattyRaw      => "Raw Patty",
            ItemType.PattyCooked   => "Cooked Patty",
            ItemType.HamRaw        => "Ham & Cheese",
            ItemType.FrozenFries   => "Frozen Fries",
            ItemType.FriesCooked   => "Cooked Fries",
            ItemType.ChickenRaw    => "Raw Chicken",
            ItemType.ChickenCooked => "Cooked Chicken",
            _ => type.ToString(),
        };
    }
}