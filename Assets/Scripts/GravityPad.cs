using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Purple gravity-flip floor pad.
//  ZeroGHop:     players who step on float in an arc along the tile's arrow to a safe landing spot.
//  ItemLauncher: a Sender launches items dropped on it (or the held item, on interact) to its paired Receiver.
[RequireComponent(typeof(BoxCollider))]
[DefaultExecutionOrder(100)] // after PlayerControl, so the same interact press can't re-pick up the item we drop
public class GravityPad : MonoBehaviour
{
    public enum PadMode { ZeroGHop, ItemLauncher }

    public PadMode mode = PadMode.ZeroGHop;

    [Header("Feedback")]
    public Renderer glowRenderer;
    [ColorUsage(false)] public Color glowColor = new Color(0.65f, 0.3f, 1f);
    [Tooltip("Glow while ready to use (breathes gently)")]
    public float idleGlow = 3f;
    public float activeGlow = 8f;
    [Tooltip("Glow right after use; it slowly charges back up to idle over the cooldown")]
    public float cooldownGlow = 0.1f;
    public float pulseTime = 0.35f;
    [Tooltip("Seconds before the pad can launch again")]
    public float cooldown = 3f;
    public AudioClip activateSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    public UnityEvent onActivated;

    [Header("Zero-G Hop")]
    [Tooltip("Floor tile size, for the hop distance")]
    public float tileSize = 1.3f;
    [Tooltip("How far the pad throws you along its painted arrow (the pad's -Z), in tiles")]
    public float hopTiles = 5.25f;
    public float hopHeight = 1.35f;
    public float hopTime = 0.6f;
    public float playerRadius = 0.45f;
    [Tooltip("Landing floor may differ from the take-off floor by at most this much")]
    public float maxStepHeight = 0.35f;

    [Header("Item Launcher")]
    public bool isSender;
    public GravityPad receiver;
    public float launchTime = 1.2f;
    public float launchHeight = 4f;

    public static readonly List<GravityPad> All = new List<GravityPad>();
    private static readonly HashSet<PlayerControl> hopping = new HashSet<PlayerControl>();
    private static readonly List<Vector3> reservedLandings = new List<Vector3>();
    private static readonly HashSet<DroppedItem> launching = new HashSet<DroppedItem>();
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private readonly HashSet<PlayerControl> playersOnPad = new HashSet<PlayerControl>();
    private MaterialPropertyBlock block;
    private int glowIndex = -1;
    private float lastActivation = -99f;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        block = new MaterialPropertyBlock();
        if (glowRenderer != null)
        {
            Material[] mats = glowRenderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && (mats[i].name.StartsWith("gravtile_glow") || mats[i].name.StartsWith("receiver_glow_purple")))
                    glowIndex = i;
        }
    }

    private void Start()
    {
        if (mode == PadMode.ItemLauncher && isSender && receiver != null)
            BuildArrow();
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable()
    {
        All.Remove(this);
        playersOnPad.Clear();
    }

    private bool Ready(bool allowSimultaneous)
    {
        float since = Time.time - lastActivation;
        return since >= cooldown || (allowSimultaneous && since < 0.2f);
    }

    public void Activate()
    {
        if (Time.time - lastActivation >= 0.2f)
            lastActivation = Time.time;
        if (activateSound != null)
            AudioSource.PlayClipAtPoint(activateSound, transform.position, soundVolume);
        else
            AudioManager.Instance?.PlayPadSFX();
        onActivated?.Invoke();
    }

    private void Update()
    {
        // Used: flash, go dark, charge back up (a visible timer). Ready: flash once, then breathe.
        float since = Time.time - lastActivation;
        float readyFor = since - cooldown;
        float glow = since < pulseTime ? Mathf.Lerp(activeGlow, cooldownGlow, since / pulseTime)
            : readyFor < 0f ? Mathf.Lerp(cooldownGlow, idleGlow * 0.35f, (since - pulseTime) / (cooldown - pulseTime))
            : readyFor < pulseTime ? Mathf.Lerp(activeGlow, idleGlow, readyFor / pulseTime)
            : idleGlow * (0.8f + 0.2f * Mathf.Sin(Time.time * 4f));
        SetGlow(glowRenderer, glowIndex, glow);
        SetGlow(arrowRenderer, 0, glow);

        // Sender: holding an item and pressing interact on the pad puts it down here, which launches it.
        if (mode == PadMode.ItemLauncher && isSender && receiver != null && Ready(false))
            foreach (PlayerControl p in playersOnPad)
                if (p != null && !p.heldItem.IsEmpty && p.IsPrimaryActionPressed())
                {
                    p.DropItemAtPosition(transform.position + Vector3.up * 0.6f);
                    break;
                }
    }

    private void SetGlow(Renderer r, int index, float intensity)
    {
        if (r == null || index < 0)
            return;
        r.GetPropertyBlock(block, index);
        block.SetColor(EmissionId, glowColor * intensity);
        r.SetPropertyBlock(block, index);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerControl player = other.GetComponentInParent<PlayerControl>();
        if (player != null)
        {
            playersOnPad.Add(player);
            if (mode == PadMode.ZeroGHop && !hopping.Contains(player) && Ready(true))
            {
                Activate();
                StartCoroutine(Hop(player));
            }
            return;
        }

        if (mode == PadMode.ItemLauncher && isSender && receiver != null)
        {
            DroppedItem item = other.GetComponent<DroppedItem>();
            if (item != null && !launching.Contains(item))
            {
                Activate();
                StartCoroutine(Launch(item));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerControl player = other.GetComponentInParent<PlayerControl>();
        if (player != null)
            playersOnPad.Remove(player);
    }

    // ---------- Zero-G hop ----------

    private IEnumerator Hop(PlayerControl player)
    {
        hopping.Add(player);
        Rigidbody body = player.GetComponent<Rigidbody>();
        Vector3 start = body.position;

        float floorY = Physics.Raycast(start, Vector3.down, out RaycastHit below, 5f, ~0, QueryTriggerInteraction.Ignore) ? below.point.y : start.y - 1f;
        float pivotHeight = start.y - floorY;

        Vector3 dir = Flat(-transform.forward).normalized; // the arrow painted on the tile points along -Z

        bool found = FindLanding(player, start, dir, floorY, out Vector3 landingFloor);
        Vector3 landing = found ? landingFloor + Vector3.up * pivotHeight : start; // nowhere safe: bounce in place
        float height = found ? hopHeight : hopHeight * 0.4f;
        reservedLandings.Add(landing);

        player.inputLocked = true;
        player.doMove = false;
        body.isKinematic = true;
        Vector3 travel = Flat(landing - start);
        if (travel.sqrMagnitude > 0.01f)
            player.transform.rotation = Quaternion.LookRotation(travel);

        for (float t = 0f; t < hopTime; t += Time.fixedDeltaTime)
        {
            float k = t / hopTime;
            float ease = 1f - (1f - k) * (1f - k) * (1f - k); // hard launch, gentle float down
            Vector3 p = Vector3.Lerp(start, landing, ease) + Vector3.up * Mathf.Sin(k * Mathf.PI) * height;
            body.MovePosition(p);
            yield return new WaitForFixedUpdate();
        }

        body.position = landing;
        player.transform.position = landing;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        player.inputLocked = false;
        reservedLandings.Remove(landing);
        hopping.Remove(player);
        VisualFx.Puff(landing - Vector3.up * (pivotHeight - 0.1f), glowColor, 8, 0.35f);
    }

    private bool FindLanding(PlayerControl player, Vector3 start, Vector3 dir, float floorY, out Vector3 landing)
    {
        // Farthest spot along the arrow first, stepping back a tile at a time if it's blocked.
        var candidates = new List<Vector3>();
        Vector3 side = Vector3.Cross(Vector3.up, dir);
        for (float n = hopTiles; n >= 1f; n -= 1f)
            foreach (int lateral in new[] { 0, 1, -1 })
                candidates.Add(start + dir * n * tileSize + side * lateral * tileSize);

        foreach (Vector3 c in candidates)
            if (IsSafeLanding(c, player, floorY, out landing))
                return true;

        landing = start;
        return false;
    }

    // Walkable floor at about the same height, nothing in the way, not on a pad, not where someone else is landing.
    private bool IsSafeLanding(Vector3 point, PlayerControl player, float floorY, out Vector3 floor)
    {
        floor = point;
        if (!Physics.Raycast(new Vector3(point.x, floorY + 3f, point.z), Vector3.down, out RaycastHit hit, 8f, ~0, QueryTriggerInteraction.Ignore))
            return false; // gap / hole
        if (hit.normal.y < 0.7f || Mathf.Abs(hit.point.y - floorY) > maxStepHeight)
            return false;
        if (hit.collider.GetComponentInParent<BaseStation>() != null || hit.collider.GetComponentInParent<PlayerControl>() != null)
            return false;

        floor = hit.point;
        Vector3 bottom = floor + Vector3.up * (playerRadius + 0.1f);
        Vector3 top = floor + Vector3.up * 1.6f;
        foreach (Collider c in Physics.OverlapCapsule(bottom, top, playerRadius, ~0, QueryTriggerInteraction.Ignore))
            if (c.GetComponentInParent<PlayerControl>() != player)
                return false;

        float spacing = playerRadius * 2f + 0.3f;
        foreach (Vector3 r in reservedLandings)
            if (Flat(r - floor).magnitude < spacing)
                return false;
        foreach (GravityPad pad in All)
            if (Flat(pad.transform.position - floor).magnitude < pad.tileSize)
                return false;
        return true;
    }

    // ---------- Item launcher ----------

    private IEnumerator Launch(DroppedItem item)
    {
        launching.Add(item);
        Rigidbody body = item.GetComponent<Rigidbody>();
        Collider col = item.GetComponent<Collider>();
        body.isKinematic = true;
        col.enabled = false;

        Vector3 start = item.transform.position;
        for (float t = 0f; t < launchTime && item != null; t += Time.deltaTime)
        {
            float k = t / launchTime;
            Vector3 target = receiver.transform.position + Vector3.up * 0.5f; // follows the receiver while islands move
            item.transform.position = Vector3.Lerp(start, target, k) + Vector3.up * 4f * launchHeight * k * (1f - k);
            yield return null;
        }

        if (item != null)
        {
            Vector3 spot = receiver.FreeSpot(item);
            item.transform.position = spot;
            body.position = spot;
            col.enabled = true;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            receiver.Activate();
            launching.Remove(item);
        }
    }

    // Pad centre, or the nearest free walkable tile around it if an item is already sitting here.
    private Vector3 FreeSpot(DroppedItem incoming)
    {
        Vector3 center = transform.position + Vector3.up * 0.5f;
        if (IsFreeForItem(center, incoming))
            return center;

        Vector3 best = center;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 offset = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * tileSize;
            Vector3 p = center + offset;
            if (offset.magnitude < bestDistance && IsFreeForItem(p, incoming)
                && Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.point.y - transform.position.y) < maxStepHeight
                && hit.collider.GetComponentInParent<BaseStation>() == null)
            {
                best = new Vector3(p.x, hit.point.y + 0.5f, p.z);
                bestDistance = offset.magnitude;
            }
        }
        return best;
    }

    private static bool IsFreeForItem(Vector3 p, DroppedItem incoming)
    {
        foreach (Collider c in Physics.OverlapSphere(p, 0.6f, ~0, QueryTriggerInteraction.Ignore))
        {
            DroppedItem other = c.GetComponent<DroppedItem>();
            if (other != null && other != incoming)
                return false;
        }
        return true;
    }

    // ---------- Sender arrow ----------

    private Renderer arrowRenderer;

    private void BuildArrow()
    {
        var go = new GameObject("Arrow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 0.12f;
        go.transform.rotation = Quaternion.LookRotation(Flat(receiver.transform.position - transform.position).normalized);

        float s = tileSize * 0.35f;
        var mesh = new Mesh { name = "PadArrow" };
        mesh.vertices = new[]
        {
            new Vector3(-0.25f * s, 0f, -s), new Vector3(0.25f * s, 0f, -s), new Vector3(0.25f * s, 0f, 0.1f * s), new Vector3(-0.25f * s, 0f, 0.1f * s),
            new Vector3(-0.7f * s, 0f, 0.1f * s), new Vector3(0.7f * s, 0f, 0.1f * s), new Vector3(0f, 0f, s),
        };
        mesh.triangles = new[] { 0, 3, 2, 0, 2, 1, 4, 6, 5 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        arrowRenderer = go.AddComponent<MeshRenderer>();
        arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (glowRenderer != null && glowIndex >= 0)
            arrowRenderer.sharedMaterial = glowRenderer.sharedMaterials[glowIndex];
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
