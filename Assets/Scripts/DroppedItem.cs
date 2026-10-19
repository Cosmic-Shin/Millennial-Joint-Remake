using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : MonoBehaviour, IInteractable
{
    public KitchenItemData item = new KitchenItemData();
    private KitchenItemVisualizer visualizer;

    private const float fallDestroyY = -15f;

    [HideInInspector] public PlayerControl droppedBy;
    private const float forgetDropperDistance = 2.5f;

    private void Update()
    {
        if (transform.position.y < fallDestroyY)
            Destroy(gameObject);

        if (droppedBy != null && Vector3.Distance(droppedBy.transform.position, transform.position) > forgetDropperDistance)
            droppedBy = null;
    }

    private const float landImpactSpeed = 2.5f;
    private static readonly Color landDustColor = new Color(0.85f, 0.8f, 0.72f, 0.8f);
    private float lastLandTime = -1f;
    private Coroutine landSquash;

    private void OnCollisionEnter(Collision collision)
    {
        float impact = collision.relativeVelocity.magnitude;
        if (impact < landImpactSpeed || Time.time - lastLandTime < 0.15f)
            return;

        lastLandTime = Time.time;
        float strength = Mathf.Clamp01(impact / 10f);

        VisualFx.Puff(collision.GetContact(0).point, landDustColor, Mathf.RoundToInt(Mathf.Lerp(4f, 12f, strength)), Mathf.Lerp(0.25f, 0.45f, strength));
        AudioManager.Instance?.PlayLandSFX(strength + 0.3f);

        Transform visual = visualizer != null ? visualizer.CurrentVisual : null;
        if (visual != null)
        {
            if (landSquash != null)
                StopCoroutine(landSquash);
            visual.localScale = visualizer.VisualScale;
            landSquash = StartCoroutine(VisualFx.Squash(visual, visualizer.VisualScale, Mathf.Lerp(0.15f, 0.35f, strength), 0.3f));
        }
    }

    public void Initialize(KitchenItemData sourceItem, KitchenItemVisualizer sourceVisualizer)
    {
        if (sourceItem != null)
            item.CopyFrom(sourceItem);

        if (sourceVisualizer != null)
        {
            visualizer = gameObject.AddComponent<KitchenItemVisualizer>();
            visualizer.anchor = transform;
            visualizer.localPosition = Vector3.zero;
            visualizer.localEulerAngles = Vector3.zero;
            visualizer.localScale = Vector3.one;
            visualizer.Refresh(item);
        }
    }

    public bool CanInteractWith(PlayerControl player)
    {
        if (player == null) return false;
        return player.heldItem.IsEmpty && !item.IsEmpty;
    }

    public void Interact(PlayerControl player)
    {
        if (player == null) return;
        if (!player.heldItem.IsEmpty) return;

        player.heldItem.CopyFrom(item);
        player.RefreshHeldItemDisplay();
        AudioManager.Instance?.PlayCounterTopInteractSFX();
        Destroy(gameObject);
    }
}
