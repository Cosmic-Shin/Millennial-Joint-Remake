using UnityEngine;

public class KitchenItemVisualizer : MonoBehaviour
{
    public Transform anchor;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    [Header("Item Prefabs")]
    public GameObject platePrefab;
    public GameObject plateWithBunPrefab;
    public GameObject plateWithBunAndPattyPrefab;
    public GameObject plateCompleteBurgerPrefab;
    public GameObject plateWithBreadPrefab;
    public GameObject plateWithBreadAndHamPrefab;
    public GameObject plateCompleteSandwichPrefab;
    public GameObject plateWithChickenPrefab;
    public GameObject cupEmptyPrefab;
    public GameObject cupSodaPrefab;
    public GameObject cupCoffeePrefab;
    public GameObject rawChickenPrefab;
    public GameObject cookedChickenPrefab;
    public GameObject frozenFriesPrefab;
    public GameObject cookedFriesPrefab;
    public GameObject bunPrefab;
    public GameObject breadPrefab;
    public GameObject rawPattyPrefab;
    public GameObject cookedPattyPrefab;
    public GameObject rawVeggiePrefab;
    public GameObject choppedVeggiePrefab;
    public GameObject rawHamPrefab;
    public GameObject cookedHamPrefab;
    public GameObject cheesePrefab;

    private GameObject currentVisual;
    private GameObject currentPrefab;

    public void Refresh(KitchenItemData itemData)
    {
        if (itemData == null || itemData.IsEmpty)
        {
            ClearVisual();
            return;
        }

        GameObject prefab = GetPrefabForItem(itemData);
        if (prefab == null)
        {
            ClearVisual();
            return;
        }

        if (currentVisual != null && currentPrefab == prefab)
            return;

        ClearVisual();
        CreateVisual(prefab);
    }

    public void ClearVisual()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
            currentPrefab = null;
        }
    }

    public void CopyPrefabReferencesFrom(KitchenItemVisualizer source)
    {
        if (source == null)
            return;

        platePrefab = source.platePrefab;
        plateWithBunPrefab = source.plateWithBunPrefab;
        plateWithBunAndPattyPrefab = source.plateWithBunAndPattyPrefab;
        plateCompleteBurgerPrefab = source.plateCompleteBurgerPrefab;
        plateWithBreadPrefab = source.plateWithBreadPrefab;
        plateWithBreadAndHamPrefab = source.plateWithBreadAndHamPrefab;
        plateCompleteSandwichPrefab = source.plateCompleteSandwichPrefab;
        plateWithChickenPrefab = source.plateWithChickenPrefab;
        cupEmptyPrefab = source.cupEmptyPrefab;
        cupSodaPrefab = source.cupSodaPrefab;
        cupCoffeePrefab = source.cupCoffeePrefab;
        rawChickenPrefab = source.rawChickenPrefab;
        cookedChickenPrefab = source.cookedChickenPrefab;
        frozenFriesPrefab = source.frozenFriesPrefab;
        cookedFriesPrefab = source.cookedFriesPrefab;
        bunPrefab = source.bunPrefab;
        breadPrefab = source.breadPrefab;
        rawPattyPrefab = source.rawPattyPrefab;
        cookedPattyPrefab = source.cookedPattyPrefab;
        rawVeggiePrefab = source.rawVeggiePrefab;
        choppedVeggiePrefab = source.choppedVeggiePrefab;
        rawHamPrefab = source.rawHamPrefab;
        cookedHamPrefab = source.cookedHamPrefab;
        cheesePrefab = source.cheesePrefab;
    }

    private GameObject GetPrefabForItem(KitchenItemData itemData)
    {
        if (itemData == null || itemData.IsEmpty)
            return null;

        if (itemData.type == ItemType.Cup)
        {
            if (itemData.cupHasCoffee)
                return cupCoffeePrefab;
            if (itemData.cupHasSoda)
                return cupSodaPrefab;
            return cupEmptyPrefab;
        }

        if (itemData.type == ItemType.Plate)
        {
            if (itemData.IsCompleteSandwich && plateCompleteSandwichPrefab != null)
                return plateCompleteSandwichPrefab;
            if (itemData.IsCompleteBurger && plateCompleteBurgerPrefab != null)
                return plateCompleteBurgerPrefab;
            if (itemData.plateHasBread && itemData.plateHasHam && plateWithBreadAndHamPrefab != null)
                return plateWithBreadAndHamPrefab;
            if (itemData.plateHasBun && itemData.plateHasPatty && plateWithBunAndPattyPrefab != null)
                return plateWithBunAndPattyPrefab;
            if (itemData.plateHasBread && plateWithBreadPrefab != null)
                return plateWithBreadPrefab;
            if (itemData.plateHasBun && plateWithBunPrefab != null)
                return plateWithBunPrefab;
            if (itemData.plateHasChicken && plateWithChickenPrefab != null)
                return plateWithChickenPrefab;
            return platePrefab;
        }

        if (itemData.type == ItemType.Bun)
            return bunPrefab;

        if (itemData.type == ItemType.Bread)
            return breadPrefab;

        if (itemData.type == ItemType.PattyRaw)
            return rawPattyPrefab;

        if (itemData.type == ItemType.PattyCooked)
            return cookedPattyPrefab;

        if (itemData.type == ItemType.VeggieRaw)
            return rawVeggiePrefab;

        if (itemData.type == ItemType.VeggieChopped)
            return choppedVeggiePrefab;

        if (itemData.type == ItemType.HamRaw)
            return rawHamPrefab;

        if (itemData.type == ItemType.HamCooked)
            return cookedHamPrefab;

        if (itemData.type == ItemType.Cheese)
            return cheesePrefab;

        if (itemData.type == ItemType.FrozenFries)
            return frozenFriesPrefab;

        if (itemData.type == ItemType.FriesCooked)
            return cookedFriesPrefab;

        if (itemData.type == ItemType.ChickenRaw)
            return rawChickenPrefab;

        if (itemData.type == ItemType.ChickenCooked)
            return cookedChickenPrefab;

        return null;
    }

    private void CreateVisual(GameObject prefab)
    {
        if (prefab == null)
            return;

        Transform parent = anchor != null ? anchor : transform;
        currentVisual = Instantiate(prefab, parent);
        currentVisual.transform.localPosition = localPosition;
        currentVisual.transform.localEulerAngles = localEulerAngles;
        currentVisual.transform.localScale = localScale;
        currentPrefab = prefab;
    }
}
