using UnityEngine;

public class KitchenItemVisualizer : MonoBehaviour
{
    public Transform anchor;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    private GameObject currentVisual;
    private GameObject currentPrefab;

    public void Refresh(KitchenItemData itemData)
    {
        if (itemData == null || itemData.IsEmpty)
        {
            ClearVisual();
            return;
        }

        if (ItemPrefabRegistry.Instance == null)
        {
            Debug.LogError("ItemPrefabRegistry not found in scene!");
            ClearVisual();
            return;
        }

        GameObject prefab = ItemPrefabRegistry.Instance.GetPrefabForItem(itemData);

        if (prefab == null)
        {
            Debug.LogWarning("No prefab found for item: " + itemData.GetDisplayName());
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

    private void CreateVisual(GameObject prefab)
    {
        Transform parent = anchor != null ? anchor : transform;

        currentVisual = Instantiate(prefab, parent);
        currentVisual.transform.localPosition = localPosition;
        currentVisual.transform.localEulerAngles = localEulerAngles;
        currentVisual.transform.localScale = localScale;

        currentPrefab = prefab;
    }
}