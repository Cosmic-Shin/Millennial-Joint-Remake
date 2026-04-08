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
    PattyCooked
}

[Serializable]
public class KitchenItemData
{
    public ItemType type = ItemType.None;

    [Header("Plate Contents")]
    public bool plateHasBun;
    public bool plateHasPatty;   // order matches game rule: Bun → Patty → Veggie
    public bool plateHasVeggie;

    public bool IsEmpty  => type == ItemType.None;
    public bool IsPlate  => type == ItemType.Plate;

    public bool IsCompleteSandwich =>
        type == ItemType.Plate &&
        plateHasBun &&
        plateHasPatty &&
        plateHasVeggie;

    public void Set(ItemType newType)
    {
        type = newType;

        if (newType != ItemType.Plate)
        {
            plateHasBun    = false;
            plateHasPatty  = false;
            plateHasVeggie = false;
        }
    }

    public void MakePlate()
    {
        type           = ItemType.Plate;
        plateHasBun    = false;
        plateHasPatty  = false;
        plateHasVeggie = false;
    }

    public void Clear()
    {
        type           = ItemType.None;
        plateHasBun    = false;
        plateHasPatty  = false;
        plateHasVeggie = false;
    }

    public void CopyFrom(KitchenItemData other)
    {
        type           = other.type;
        plateHasBun    = other.plateHasBun;
        plateHasPatty  = other.plateHasPatty;
        plateHasVeggie = other.plateHasVeggie;
    }

    public bool IsValidPlateIngredient()
    {
        return type == ItemType.Bun ||
               type == ItemType.VeggieChopped ||
               type == ItemType.PattyCooked;
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

            if (IsCompleteSandwich)
                result += " (Complete Sandwich)";

            return result;
        }

        return type.ToString();
    }
}