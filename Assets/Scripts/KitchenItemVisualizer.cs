using UnityEngine;

public class KitchenItemVisualizer : MonoBehaviour
{
    public Transform anchor;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
    public Vector3 localScale = Vector3.one;

    private GameObject currentVisual;
    private GameObject currentPrefab;

    public Transform CurrentVisual => currentVisual != null ? currentVisual.transform : null;
    public Vector3 VisualScale => localScale * (ItemPrefabRegistry.Instance != null ? ItemPrefabRegistry.Instance.foodScale : 1f);

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
        if (System.Array.IndexOf(HotFood, itemData.type) >= 0)
            VisualFx.AddSteam(currentVisual.transform);
    }

    private static readonly ItemType[] HotFood =
        { ItemType.PattyCooked, ItemType.ChickenCooked, ItemType.HotDogCooked, ItemType.FriesCooked, ItemType.Chili, ItemType.ShrimpCooked, ItemType.DonutCooked };

    // Arc the current item in from a world point (e.g. the chef's hands) instead of popping in place.
    public void FlyFrom(Vector3 worldFrom, float time = 0.16f)
    {
        if (currentVisual != null && isActiveAndEnabled)
            StartCoroutine(Fly(currentVisual.transform, worldFrom, time));
    }

    private System.Collections.IEnumerator Fly(Transform t, Vector3 worldFrom, float time)
    {
        Vector3 end = t.localPosition;
        Vector3 start = t.parent.InverseTransformPoint(worldFrom);
        for (float e = 0f; e < time && t != null; e += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, e / time);
            t.localPosition = Vector3.Lerp(start, end, k) + t.parent.InverseTransformVector(Vector3.up) * (Mathf.Sin(k * Mathf.PI) * 0.35f);
            yield return null;
        }
        if (t != null)
            t.localPosition = end;
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
        currentVisual.transform.localScale = VisualScale;

        if (isActiveAndEnabled)
            StartCoroutine(VisualFx.Pop(currentVisual.transform, 0.55f, 0.22f, VisualScale));

        currentPrefab = prefab;
    }
}