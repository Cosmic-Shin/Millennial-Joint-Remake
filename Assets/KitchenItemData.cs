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
    public bool plateHasVeggie;
    public bool plateHasPatty;

    public bool IsEmpty => type == ItemType.None;
    public bool IsPlate => type == ItemType.Plate;

    public bool IsCompleteSandwich =>
        type == ItemType.Plate &&
        plateHasBun &&
        plateHasVeggie &&
        plateHasPatty;

    public void Set(ItemType newType)
    {
        type = newType;

        if (newType != ItemType.Plate)
        {
            plateHasBun = false;
            plateHasVeggie = false;
            plateHasPatty = false;
        }
    }

    public void MakePlate()
    {
        type = ItemType.Plate;
        plateHasBun = false;
        plateHasVeggie = false;
        plateHasPatty = false;
    }

    public void Clear()
    {
        type = ItemType.None;
        plateHasBun = false;
        plateHasVeggie = false;
        plateHasPatty = false;
    }

    public void CopyFrom(KitchenItemData other)
    {
        type = other.type;
        plateHasBun = other.plateHasBun;
        plateHasVeggie = other.plateHasVeggie;
        plateHasPatty = other.plateHasPatty;
    }

    public bool IsValidPlateIngredient()
    {
        return type == ItemType.Bun ||
               type == ItemType.VeggieChopped ||
               type == ItemType.PattyCooked;
    }

    public string GetDisplayName()
    {
        if (type == ItemType.Plate)
        {
            string result = "Plate";
            result += plateHasBun ? " + Bun" : "";
            result += plateHasVeggie ? " + ChoppedVeggie" : "";
            result += plateHasPatty ? " + CookedPatty" : "";

            if (IsCompleteSandwich)
                result += " (Complete Sandwich)";

            return result;
        }

        return type.ToString();
    }
}