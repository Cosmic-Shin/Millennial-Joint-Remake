using System;
using UnityEngine;

public enum ItemType
{
    None,
    Plate,
    Bun,
    VeggieRaw,
    VeggieChopped,
    PattyRaw,
    PattyCooked,
    Cup,
    FrozenFries,
    FriesCooked
}

[Serializable]
public class KitchenItemData
{
    public ItemType type = ItemType.None;

    [Header("Plate Contents")]
    public bool plateHasBun;
    public bool plateHasPatty;   // order matches game rule: Bun → Patty → Veggie
    public bool plateHasVeggie;
    public bool plateHasFries;

    [Header("Cup Contents")]
    public bool cupHasSoda;
    public bool cupHasBoba;      // boba pearls have been added
    public bool cupBobaDrinkReady; // finalized boba drink

    public bool IsEmpty  => type == ItemType.None;
    public bool IsPlate  => type == ItemType.Plate;
    public bool IsCup    => type == ItemType.Cup;

    public bool IsCompleteBurger =>
        type == ItemType.Plate &&
        plateHasBun &&
        plateHasPatty &&
        plateHasVeggie &&
        !plateHasFries;

    public bool IsCompleteFries =>
        type == ItemType.Plate &&
        plateHasFries &&
        !plateHasBun &&
        !plateHasPatty &&
        !plateHasVeggie;

    public bool IsCompleteDrink =>
        type == ItemType.Cup &&
        (cupHasSoda || cupBobaDrinkReady);

    public void Set(ItemType newType)
    {
        type = newType;

        if (newType != ItemType.Plate)
        {
            plateHasBun    = false;
            plateHasPatty  = false;
            plateHasVeggie = false;
            plateHasFries  = false;
        }

        if (newType != ItemType.Cup)
        {
            cupHasSoda        = false;
            cupHasBoba        = false;
            cupBobaDrinkReady = false;
        }
    }

    public void MakePlate()
    {
        type             = ItemType.Plate;
        plateHasBun      = false;
        plateHasPatty    = false;
        plateHasVeggie   = false;
        plateHasFries    = false;
        cupHasSoda       = false;
        cupHasBoba       = false;
        cupBobaDrinkReady = false;
    }

    public void Clear()
    {
        type               = ItemType.None;
        plateHasBun        = false;
        plateHasPatty      = false;
        plateHasVeggie     = false;
        plateHasFries      = false;
        cupHasSoda         = false;
        cupHasBoba         = false;
        cupBobaDrinkReady  = false;
    }

    public void CopyFrom(KitchenItemData other)
    {
        type               = other.type;
        plateHasBun        = other.plateHasBun;
        plateHasPatty      = other.plateHasPatty;
        plateHasVeggie     = other.plateHasVeggie;
        plateHasFries      = other.plateHasFries;
        cupHasSoda         = other.cupHasSoda;
        cupHasBoba         = other.cupHasBoba;
        cupBobaDrinkReady  = other.cupBobaDrinkReady;
    }

    public bool IsValidPlateIngredient()
    {
        return type == ItemType.Bun ||
               type == ItemType.VeggieChopped ||
               type == ItemType.PattyCooked ||
               type == ItemType.FriesCooked;
    }

    // FIX: display order now matches game rule (Bun → Patty → Veggie)
    public string GetDisplayName()
    {
        if (type == ItemType.Plate)
        {
            string result = "Plate";
            result += plateHasBun    ? " + Bun"          : "";
            result += plateHasPatty  ? " + CookedPatty"  : "";
            result += plateHasVeggie ? " + ChoppedVeggie" : "";
            result += plateHasFries  ? " + Fries"        : "";

            if (IsCompleteBurger)
                result += " (Complete Burger)";
            else if (IsCompleteFries)
                result += " (Complete Fries)";

            return result;
        }

        if (type == ItemType.Cup)
        {
            if (cupHasSoda && !cupHasBoba && !cupBobaDrinkReady)
                return "Cup (Soda Drink)";
            if (cupBobaDrinkReady)
                return "Cup (Boba Drink)";
            if (cupHasBoba)
                return "Cup (Boba in progress)";
            return "Cup (Empty)";
        }

        return type.ToString();
    }
}