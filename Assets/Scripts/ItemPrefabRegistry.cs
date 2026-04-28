using System.Collections.Generic;
using UnityEngine;

public class ItemPrefabRegistry : MonoBehaviour
{
    public static ItemPrefabRegistry Instance { get; private set; }

    [Header("Plate Prefabs")]
    public GameObject platePrefab;
    public GameObject plateWithBunPrefab;
    public GameObject plateWithBunAndPattyPrefab;
    public GameObject plateCompleteBurgerPrefab;
    public GameObject plateWithBreadPrefab;
    public GameObject plateWithBreadAndHamPrefab;
    public GameObject plateWithChickenPrefab;
    public GameObject plateWithFriesPrefab;

    // 🔥 NEW CHILI DOG PREFABS
    public GameObject plateWithDogBunPrefab;
    public GameObject plateWithDogBunAndHotdogPrefab;
    public GameObject plateCompleteChiliDogPrefab;

    [Header("Cup Prefabs")]
    public GameObject cupEmptyPrefab;
    public GameObject cupSodaPrefab;
    public GameObject cupIceTeaPrefab;
    public GameObject cupOrangeJuicePrefab;
    public GameObject cupCoffeePrefab;

    [Header("Ingredient Prefabs")]
    public GameObject bunPrefab;
    public GameObject breadPrefab;
    public GameObject rawPattyPrefab;
    public GameObject cookedPattyPrefab;
    public GameObject rawVeggiePrefab;
    public GameObject rawHamPrefab;
    public GameObject rawChickenPrefab;
    public GameObject cookedChickenPrefab;
    public GameObject frozenFriesPrefab;
    public GameObject cookedFriesPrefab;

    // 🔥 NEW INGREDIENT PREFABS
    public GameObject dogBunPrefab;
    public GameObject hotdogRawPrefab;
    public GameObject hotdogCookedPrefab;

    private Dictionary<ItemType, GameObject> simplePrefabLookup;
    private Dictionary<string, GameObject> platePrefabLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeLookupTables();
    }

    private void InitializeLookupTables()
    {
        simplePrefabLookup = new Dictionary<ItemType, GameObject>
        {
            { ItemType.Bun, bunPrefab },
            { ItemType.Bread, breadPrefab },
            { ItemType.PattyRaw, rawPattyPrefab },
            { ItemType.PattyCooked, cookedPattyPrefab },
            { ItemType.VeggieRaw, rawVeggiePrefab },
            { ItemType.HamRaw, rawHamPrefab },
            { ItemType.FrozenFries, frozenFriesPrefab },
            { ItemType.FriesCooked, cookedFriesPrefab },
            { ItemType.ChickenRaw, rawChickenPrefab },
            { ItemType.ChickenCooked, cookedChickenPrefab },

            // 🔥 NEW
            { ItemType.DogBun, dogBunPrefab },
            { ItemType.HotDogRaw, hotdogRawPrefab },
            { ItemType.HotDogCooked, hotdogCookedPrefab }
        };

        platePrefabLookup = new Dictionary<string, GameObject>
        {
            { "complete_sandwich", plateWithBreadAndHamPrefab },
            { "complete_burger", plateCompleteBurgerPrefab },
            { "bread_ham", plateWithBreadAndHamPrefab },
            { "bun_patty", plateWithBunAndPattyPrefab },
            { "bread", plateWithBreadPrefab },
            { "bun", plateWithBunPrefab },
            { "chicken", plateWithChickenPrefab },
            { "fries", plateWithFriesPrefab },

            // 🔥 NEW CHILI DOG STATES
            { "dogbun", plateWithDogBunPrefab },
            { "dogbun_hotdog", plateWithDogBunAndHotdogPrefab },
            { "complete_chilidog", plateCompleteChiliDogPrefab },

            { "empty", platePrefab }
        };
    }

    public GameObject GetPrefabForItem(KitchenItemData itemData)
    {
        if (itemData == null || itemData.IsEmpty)
            return null;

        if (itemData.type == ItemType.Cup)
            return GetCupPrefab(itemData);

        if (itemData.type == ItemType.Plate)
            return GetPlatePrefab(itemData);

        if (simplePrefabLookup.TryGetValue(itemData.type, out GameObject prefab))
            return prefab;

        return null;
    }

    private GameObject GetCupPrefab(KitchenItemData itemData)
    {
        if (itemData.cupHasCoffee) return cupCoffeePrefab;
        if (itemData.cupHasIceTea) return cupIceTeaPrefab;
        if (itemData.cupHasSoda) return cupSodaPrefab;
        if (itemData.cupHasOrangeJuice) return cupOrangeJuicePrefab;

        return cupEmptyPrefab;
    }

    private GameObject GetPlatePrefab(KitchenItemData itemData)
    {
        // 🔥 CHILI DOG (TOP PRIORITY)
        if (itemData.IsCompleteChiliDog)
            return platePrefabLookup["complete_chilidog"];

        if (itemData.plateHasDogBun && itemData.plateHasHotdog)
            return platePrefabLookup["dogbun_hotdog"];

        if (itemData.plateHasDogBun)
            return platePrefabLookup["dogbun"];

        // EXISTING SYSTEM
        if (itemData.IsCompleteSandwich)
            return platePrefabLookup["complete_sandwich"];

        if (itemData.IsCompleteBurger)
            return platePrefabLookup["complete_burger"];

        if (itemData.plateHasBread && itemData.plateHasHam)
            return platePrefabLookup["bread_ham"];

        if (itemData.plateHasBun && itemData.plateHasPatty)
            return platePrefabLookup["bun_patty"];

        if (itemData.plateHasBread)
            return platePrefabLookup["bread"];

        if (itemData.plateHasBun)
            return platePrefabLookup["bun"];

        if (itemData.plateHasChicken)
            return platePrefabLookup["chicken"];

        if (itemData.plateHasFries)
            return platePrefabLookup["fries"];

        return platePrefabLookup["empty"];
    }
}