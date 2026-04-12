using UnityEngine;

public class KitchenItemVisualizer : MonoBehaviour
{
    public Transform anchor;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    [Header("Item Prefabs")]
    public GameObject platePrefab;
    public GameObject plateWithChickenPrefab;
    public GameObject cupEmptyPrefab;
    public GameObject cupSodaPrefab;
    public GameObject rawChickenPrefab;
    public GameObject cookedChickenPrefab;

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

    private GameObject GetPrefabForItem(KitchenItemData itemData)
    {
        if (itemData == null || itemData.IsEmpty)
            return null;

        if (itemData.type == ItemType.Cup)
        {
            if (itemData.cupHasSoda || itemData.cupBobaDrinkReady)
                return cupSodaPrefab;
            return cupEmptyPrefab;
        }

        if (itemData.type == ItemType.Plate)
        {
            if (itemData.plateHasChicken && plateWithChickenPrefab != null)
                return plateWithChickenPrefab;
            return platePrefab;
        }

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
