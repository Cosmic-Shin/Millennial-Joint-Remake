using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroppedItem : MonoBehaviour, IInteractable
{
    public KitchenItemData item = new KitchenItemData();
    private KitchenItemVisualizer visualizer;

    private const float fallDestroyY = -15f;

    [HideInInspector] public PlayerControl droppedBy;
    private const float forgetDropperDistance = 2.5f;

    // Live list for map hazards (vacuum etc.), so they don't search the scene every frame.
    public static readonly System.Collections.Generic.List<DroppedItem> All = new System.Collections.Generic.List<DroppedItem>();
    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    // Set by a ZeroGravityItems component on the active map (Asteroid Diner).
    public static bool ZeroGravity;
    private const float zeroGDrift = 0.35f;   // m/s upwards: grab it before it floats out of reach
    private const float zeroGLostHeight = 6f;
    private bool floating;

    private void Update()
    {
        if (transform.position.y < fallDestroyY)
            Destroy(gameObject);

        if (floating)
        {
            Transform v = visualizer != null ? visualizer.CurrentVisual : null;
            if (v != null)
                v.Rotate(20f * Time.deltaTime, 45f * Time.deltaTime, 0f, Space.Self); // lazy zero-G tumble
            if (transform.position.y > zeroGLostHeight)
            {
                VisualFx.Puff(transform.position, Color.white, 10, 0.3f, 1f);
                Destroy(gameObject);
                return;
            }
        }

        if (droppedBy != null && Vector3.Distance(droppedBy.transform.position, transform.position) > forgetDropperDistance)
            droppedBy = null;

        TryCatch();
    }

    private const float catchRadius = 1.3f;
    private const float catchMinSpeed = 3f;
    private Rigidbody body;

    // Thrown food flying past an empty-handed chef gets caught mid-air.
    private void TryCatch()
    {
        if (body == null)
            body = GetComponent<Rigidbody>();
        if (body.linearVelocity.sqrMagnitude < catchMinSpeed * catchMinSpeed || !PlayerControl.GameplayActive)
            return;

        foreach (PlayerControl p in FindObjectsByType<PlayerControl>(FindObjectsSortMode.None))
        {
            if (p == droppedBy || !p.heldItem.IsEmpty || !p.CanMove)
                continue;
            if ((p.transform.position + Vector3.up - transform.position).sqrMagnitude > catchRadius * catchRadius)
                continue;

            Interact(p);
            p.OnCaught(transform.position);
            return;
        }
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

        if (ZeroGravity)
        {
            // No gravity: whatever velocity it was dropped/thrown with carries on (gently damped) plus a slow rise.
            floating = true;
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0.8f;
            VisualFx.Puff(transform.position, new Color(0.7f, 0.85f, 1f, 0.8f), 6, 0.2f, 0.6f);
        }
    }

    private void FixedUpdate()
    {
        // Steady slow rise: the drop/throw kick lands on the first physics step (after Initialize),
        // so the cap has to live here, not in Initialize.
        if (floating && body != null)
        {
            Vector3 v = body.linearVelocity;
            if (v.y > zeroGDrift)
                v.y = zeroGDrift;
            else
                v.y = Mathf.MoveTowards(v.y, zeroGDrift, 2f * Time.fixedDeltaTime);
            body.linearVelocity = v;
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
