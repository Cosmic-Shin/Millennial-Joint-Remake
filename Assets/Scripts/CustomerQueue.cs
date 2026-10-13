using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CustomerQueue : MonoBehaviour
{
    [Header("Crewmate Model")]
    public Mesh crewmateMesh;
    [Tooltip("Submesh 0 of the crewmate mesh")]
    public Material outlineMaterial;
    [Tooltip("Submesh 1, one per colour (same order as backpacks)")]
    public Material[] bodyMaterials;
    [Tooltip("Submesh 2, one per colour (same order as bodies)")]
    public Material[] backpackMaterials;
    public TMP_FontAsset bubbleFont;

    [Header("Layout")]
    public float spotSpacing = 1.4f;
    [Tooltip("Where the walkway meets the pad, behind the spots")]
    public float padEntryDistance = 1.3f;
    public float entranceDistance = 14f;
    [Tooltip("Arrivals keep to one side of the walkway, leavers to the other")]
    public float laneOffset = 0.45f;
    [Tooltip("Gap between customers arriving at the same time, so they walk in single file")]
    public float spawnSpacing = 1.3f;

    [Header("Movement")]
    public float walkSpeed = 3.2f;
    public float acceleration = 8f;
    public float turnSpeed = 10f;

    [Header("Reactions")]
    [Tooltip("Served with at least this much of the order time left = early")]
    public float earlyRatio = 0.6f;
    [Tooltip("Served with less than this much left = late")]
    public float lateRatio = 0.25f;
    public float impatientSeconds = 10f;
    public float bubbleDuration = 1.6f;

    [Header("Serve Feedback")]
    [Tooltip("Where a served customer holds their food, relative to their body")]
    public Vector3 foodHoldOffset = new Vector3(0f, 0.55f, 0.42f);
    public float foodScale = 0.8f;
    [Tooltip("Gold coin that flies from the customer to the money counter (none = no coin)")]
    public Material coinMaterial;
    public float coinFlightTime = 0.75f;

    [Header("Patience Bar")]
    public float patienceBarHeight = 1.55f;
    public Vector2 patienceBarSize = new Vector2(0.9f, 0.12f);

    private enum Phase { Arriving, Waiting, Leaving }

    private class Customer
    {
        public GameObject go;
        public Transform root;
        public Transform body;
        public MeshRenderer renderer;
        public readonly Material[] materials = new Material[3];
        public TextMeshPro bubble;
        public Transform patienceBar;
        public Transform patienceFill;
        public MeshRenderer patienceFillRenderer;
        public GameObject food;
        public int player;

        public QueuedOrder order;
        public OrderItemType lastType;
        public Phase phase;
        public readonly Queue<Vector3> path = new Queue<Vector3>();
        public Vector3 spot;
        public float speed;
        public float speedScale;
        public float animTime;
        public float hopTime;
        public float waitTime;
        public bool impatientShown;
        public bool angry;
        public float bubbleTimer;
    }

    private readonly Dictionary<QueuedOrder, Customer> byOrder = new Dictionary<QueuedOrder, Customer>();
    private readonly List<Customer> leaving = new List<Customer>();
    private readonly Stack<Customer> pool = new Stack<Customer>();
    private readonly List<QueuedOrder> toRemove = new List<QueuedOrder>();
    private MaterialPropertyBlock block;
    private float lastSpawnTime = -10f;
    private int spawnBurst;

    private bool IsVersus => OrderManager.Instance != null && OrderManager.Instance.GetCurrentMode() == OrderManager.GameMode.VERSUS;

    private void Update()
    {
        OrderManager om = OrderManager.Instance;
        bool playing = om != null && om.state == OrderManager.GameState.Playing;

        if (playing)
        {
            if (IsVersus)
            {
                Sync(om.GetPlayer1OrderQueue(), 0);
                Sync(om.GetPlayer2OrderQueue(), 3);
            }
            else
            {
                Sync(om.GetOrderQueue(), 0);
            }
        }

        toRemove.Clear();
        foreach (var pair in byOrder)
            if (!playing || !OnBoard(om, pair.Key))
                toRemove.Add(pair.Key);

        foreach (QueuedOrder order in toRemove)
        {
            Customer c = byOrder[order];
            byOrder.Remove(order);

            if (!playing)
            {
                StartLeaving(c, 0.3f);
            }
            else if (order.IsServed())
            {
                ReactToServe(c);
            }
            else
            {
                React(c, Emoji("2639"), new Color(1f, 0.25f, 0.2f), angry: true);
                StartLeaving(c, 1.2f);
            }
        }

        foreach (Customer c in byOrder.Values)
            Tick(c);

        for (int i = leaving.Count - 1; i >= 0; i--)
        {
            Customer c = leaving[i];
            if (Tick(c))
            {
                c.go.SetActive(false);
                pool.Push(c);
                leaving.RemoveAt(i);
            }
        }
    }

    private static bool OnBoard(OrderManager om, QueuedOrder order)
    {
        return om.GetCurrentMode() == OrderManager.GameMode.VERSUS
            ? om.GetPlayer1OrderQueue().Contains(order) || om.GetPlayer2OrderQueue().Contains(order)
            : om.GetOrderQueue().Contains(order);
    }

    private void Sync(List<QueuedOrder> queue, int firstSlot)
    {
        for (int i = 0; i < queue.Count && i < 3; i++)
        {
            QueuedOrder order = queue[i];
            if (order == null)
                continue;

            if (!byOrder.TryGetValue(order, out Customer c))
            {
                if (order.IsServed())
                    continue;

                c = Spawn(order, SpotFor(firstSlot + i), IsVersus ? (firstSlot == 0 ? 1 : 2) : 0);
                byOrder[order] = c;
                continue;
            }

            if (order.IsServed())
            {
                byOrder.Remove(order);
                ReactToServe(c);
                continue;
            }

            Vector3 spot = SpotFor(firstSlot + i);
            if ((spot - c.spot).sqrMagnitude > 0.01f)
                Retarget(c, spot);

            if (order.item.type != c.lastType)
            {
                c.lastType = order.item.type;
                React(c, "Huh?!", new Color(1f, 0.85f, 0.2f));
                c.hopTime = 0.4f;
            }

            if (!c.impatientShown && order.timer <= impatientSeconds)
            {
                c.impatientShown = true;
                React(c, "Hurry!", new Color(1f, 0.4f, 0.25f));
            }
        }
    }

    private void ReactToServe(Customer c)
    {
        float left = c.order.timer / Mathf.Max(0.01f, c.order.duration);

        if (left >= earlyRatio)
        {
            React(c, Emoji("1f60d"), new Color(1f, 0.85f, 0.2f));
            c.hopTime = 1.4f;
            c.speedScale = 1.25f;
        }
        else if (left >= lateRatio)
        {
            React(c, Emoji("1f60b"), new Color(0.35f, 0.95f, 0.4f));
            c.hopTime = 0.9f;
        }
        else
        {
            React(c, Emoji("1f605"), new Color(1f, 0.6f, 0.2f));
            c.speedScale = 0.65f;
        }

        SetTint(c, 0f);
        HoldFood(c);
        StartCoroutine(FlyCoin(c.root.position + Vector3.up * 1.2f, c.player));
        StartLeaving(c, 0.7f);
    }

    private void HoldFood(Customer c)
    {
        GameObject prefab = ItemPrefabRegistry.Instance != null ? ItemPrefabRegistry.Instance.GetPrefabForOrder(c.order.item.type) : null;
        if (prefab == null)
            return;

        c.food = Instantiate(prefab, c.body, false);
        c.food.transform.localPosition = foodHoldOffset;
        c.food.transform.localRotation = Quaternion.identity;
        c.food.transform.localScale = prefab.transform.localScale * foodScale;
        foreach (Collider col in c.food.GetComponentsInChildren<Collider>())
            Destroy(col);
    }

    private static TMP_Text MoneyText(int player)
    {
        if (player == 0)
            return OrderUIManager.Instance != null ? OrderUIManager.Instance.moneyText : null;
        if (VersusUIManager.Instance == null)
            return null;
        return player == 1 ? VersusUIManager.Instance.player1MoneyText : VersusUIManager.Instance.player2MoneyText;
    }

    private IEnumerator FlyCoin(Vector3 from, int player)
    {
        Camera cam = Camera.main;
        TMP_Text target = MoneyText(player);
        if (coinMaterial == null || cam == null || target == null)
            yield break;

        var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(coin.GetComponent<Collider>());
        coin.name = "Coin";
        var coinRenderer = coin.GetComponent<MeshRenderer>();
        coinRenderer.sharedMaterial = coinMaterial;
        coinRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var coinScale = new Vector3(0.45f, 0.04f, 0.45f);

        Vector3 startScreen = cam.WorldToScreenPoint(from);
        float startDepth = startScreen.z;
        const float endDepth = 3f;

        for (float t = 0f; t < coinFlightTime && target != null; t += Time.deltaTime)
        {
            float k = t / coinFlightTime;
            float ease = k * k * (3f - 2f * k);

            Canvas canvas = target.canvas;
            Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(uiCam, target.rectTransform.position);

            Vector2 screen = Vector2.Lerp(startScreen, endScreen, ease) + Vector2.up * Mathf.Sin(k * Mathf.PI) * Screen.height * 0.12f;
            float depth = Mathf.Lerp(startDepth, endDepth, ease);
            coin.transform.position = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));

            float shrink = k < 0.8f ? Mathf.Min(1f, k * 6f) : 1f - (k - 0.8f) / 0.2f;
            coin.transform.localScale = coinScale * (depth / startDepth) * shrink;
            coin.transform.rotation = cam.transform.rotation * Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 14f) * 70f);
            yield return null;
        }

        Destroy(coin);
        if (target != null)
            StartCoroutine(VisualFx.Pop(target.transform, 1.3f, 0.25f));
    }

    private void TickPatience(Customer c)
    {
        bool show = c.phase != Phase.Leaving && c.order != null;
        if (c.patienceBar.gameObject.activeSelf != show)
            c.patienceBar.gameObject.SetActive(show);
        if (!show)
            return;

        float fill = Mathf.Clamp01(c.order.timer / Mathf.Max(0.01f, c.order.duration));
        float w = patienceBarSize.x;
        c.patienceFill.localScale = new Vector3(w * fill, patienceBarSize.y, 1f);
        c.patienceFill.localPosition = new Vector3(-w * (1f - fill) / 2f, 0f, -0.01f);

        Color color = OrderUIManager.PatienceColor(fill);
        if (c.order.timer <= impatientSeconds)
            color = Color.Lerp(color, Color.white, 0.5f + 0.5f * Mathf.Sin(c.animTime * 18f));

        block.SetColor("_BaseColor", color);
        c.patienceFillRenderer.SetPropertyBlock(block);

        Camera cam = Camera.main;
        if (cam != null)
            c.patienceBar.rotation = cam.transform.rotation;
    }

    private Vector3 Local(float x, float z) => transform.TransformPoint(new Vector3(x, 0f, z));

    private Vector3 SpotFor(int slot)
    {
        float x = IsVersus
            ? (slot < 3 ? -(slot + 1) : slot - 2) * spotSpacing
            : (slot - 1) * spotSpacing;
        return Local(x, 0f);
    }

    private void Retarget(Customer c, Vector3 spot)
    {
        c.spot = spot;

        if (c.phase == Phase.Arriving && c.path.Count > 1)
        {
            Vector3 entry = c.path.Dequeue();
            c.path.Clear();
            c.path.Enqueue(entry);
        }
        else
        {
            c.path.Clear();
        }

        c.path.Enqueue(spot);
        c.phase = Phase.Arriving;
    }

    private void StartLeaving(Customer c, float delay)
    {
        c.phase = Phase.Leaving;
        c.path.Clear();
        c.waitTime = delay;
        c.speed = 0f;
        c.path.Enqueue(Local(laneOffset, -padEntryDistance));
        c.path.Enqueue(Local(laneOffset, -entranceDistance - 2f));
        leaving.Add(c);
    }

    private bool Tick(Customer c)
    {
        float dt = Time.deltaTime;
        c.animTime += dt;

        bool moving = false;
        if (c.waitTime > 0f)
        {
            c.waitTime -= dt;
        }
        else if (c.path.Count > 0)
        {
            Vector3 target = c.path.Peek();
            Vector3 to = target - c.root.position;
            to.y = 0f;
            float dist = to.magnitude;
            bool last = c.path.Count == 1;

            if (dist < 0.05f)
            {
                c.path.Dequeue();
                if (c.path.Count == 0 && c.phase == Phase.Arriving)
                    c.phase = Phase.Waiting;
            }
            else
            {
                float maxSpeed = walkSpeed * (c.speedScale > 0f ? c.speedScale : 1f);
                if (last && c.phase != Phase.Leaving)
                    maxSpeed = Mathf.Min(maxSpeed, dist * 2.5f + 0.4f);

                c.speed = Mathf.MoveTowards(c.speed, maxSpeed, acceleration * dt);
                if (c.speed > 0f)
                {
                    c.root.position += to / dist * Mathf.Min(c.speed * dt, dist);
                    moving = true;
                    c.root.rotation = Quaternion.Slerp(c.root.rotation, Quaternion.LookRotation(to), turnSpeed * dt);
                }
            }
        }
        else
        {
            c.speed = 0f;
        }

        if (!moving && c.phase == Phase.Waiting)
            c.root.rotation = Quaternion.Slerp(c.root.rotation, transform.rotation, turnSpeed * 0.6f * dt);

        float urgency = c.phase == Phase.Waiting && c.order != null
            ? 1f - Mathf.Clamp01(c.order.timer / impatientSeconds)
            : 0f;
        if (c.phase == Phase.Waiting)
            SetTint(c, urgency * 0.6f);

        float walk = moving ? Mathf.Clamp01(c.speed / walkSpeed) : 0f;
        float stride = c.animTime * (moving ? 11f * Mathf.Max(0.6f, c.speedScale) : 2f);
        float bob = moving ? Mathf.Abs(Mathf.Sin(stride)) * 0.09f * walk : Mathf.Sin(stride) * 0.02f;
        float tilt = moving ? Mathf.Sin(stride) * 9f * walk : 0f;

        if (urgency > 0f)
            bob += Mathf.Abs(Mathf.Sin(c.animTime * (8f + urgency * 10f))) * 0.05f * urgency;

        float hop = 0f;
        if (c.hopTime > 0f)
        {
            c.hopTime -= dt;
            hop = Mathf.Abs(Mathf.Sin(c.animTime * 11f)) * 0.35f;
        }

        float shake = c.angry ? Mathf.Sin(c.animTime * 45f) * 0.05f : 0f;
        float slump = c.speedScale > 0f && c.speedScale < 1f ? -10f : 0f;

        c.body.localPosition = new Vector3(shake, bob + hop, 0f);
        c.body.localRotation = Quaternion.Euler(slump, 0f, tilt);

        TickBubble(c, dt);
        TickPatience(c);

        return c.phase == Phase.Leaving && c.path.Count == 0;
    }

    private void TickBubble(Customer c, float dt)
    {
        if (c.bubbleTimer <= 0f)
        {
            if (c.bubble.gameObject.activeSelf)
                c.bubble.gameObject.SetActive(false);
            return;
        }

        c.bubbleTimer -= dt;
        float age = bubbleDuration - c.bubbleTimer;
        float pop = age < 0.15f ? Mathf.SmoothStep(0f, 1.15f, age / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((age - 0.15f) / 0.1f));
        c.bubble.transform.localScale = Vector3.one * pop;
        c.bubble.alpha = Mathf.Clamp01(c.bubbleTimer / 0.3f);

        Camera cam = Camera.main;
        if (cam != null)
            c.bubble.transform.rotation = cam.transform.rotation;
    }

    private static string Emoji(string code) => "<size=170%><sprite name=\"" + code + "\"></size>";

    private void React(Customer c, string text, Color color, bool angry = false)
    {
        c.bubble.text = text;
        c.bubble.color = color;
        c.bubble.gameObject.SetActive(true);
        c.bubbleTimer = bubbleDuration;
        if (angry)
        {
            c.angry = true;
            c.speedScale = 1.35f;
            SetTint(c, 0.6f);
        }
    }

    private void SetTint(Customer c, float redness)
    {
        block.SetColor("_BaseColor", Color.Lerp(Color.white, new Color(1f, 0.25f, 0.2f), redness));
        c.renderer.SetPropertyBlock(block, 1);
        c.renderer.SetPropertyBlock(block, 2);
    }

    private Customer Spawn(QueuedOrder order, Vector3 spot, int player)
    {
        Customer c = pool.Count > 0 ? pool.Pop() : Build();
        c.player = player;
        if (c.food != null)
        {
            Destroy(c.food);
            c.food = null;
        }

        spawnBurst = Time.time - lastSpawnTime < 0.75f ? spawnBurst + 1 : 0;
        lastSpawnTime = Time.time;

        c.order = order;
        c.lastType = order.item.type;
        c.phase = Phase.Arriving;
        c.spot = spot;
        c.speed = 0f;
        c.speedScale = 1f;
        c.hopTime = 0f;
        c.angry = false;
        c.impatientShown = false;
        c.bubbleTimer = 0f;
        c.animTime = Random.value * 10f;

        int colour = Random.Range(0, Mathf.Min(bodyMaterials.Length, backpackMaterials.Length));
        c.materials[1] = bodyMaterials[colour];
        c.materials[2] = backpackMaterials[colour];
        c.renderer.sharedMaterials = c.materials;
        SetTint(c, 0f);

        c.root.position = Local(-laneOffset, -entranceDistance - spawnBurst * spawnSpacing);
        c.root.rotation = transform.rotation;
        c.path.Clear();
        c.path.Enqueue(Local(-laneOffset, -padEntryDistance));
        c.path.Enqueue(spot);
        c.bubble.gameObject.SetActive(false);
        c.go.SetActive(true);
        return c;
    }

    private Customer Build()
    {
        block ??= new MaterialPropertyBlock();

        var go = new GameObject("Customer");
        go.transform.SetParent(transform, false);
        var body = new GameObject("Body").transform;
        body.SetParent(go.transform, false);

        body.gameObject.AddComponent<MeshFilter>().sharedMesh = crewmateMesh;
        var renderer = body.gameObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var bubbleGo = new GameObject("Bubble");
        bubbleGo.transform.SetParent(go.transform, false);
        bubbleGo.transform.localPosition = new Vector3(0f, 1.95f, 0f);
        var bubble = bubbleGo.AddComponent<TextMeshPro>();
        if (bubbleFont != null)
            bubble.font = bubbleFont;
        bubble.fontSize = 4.5f;
        bubble.alignment = TextAlignmentOptions.Center;
        bubble.textWrappingMode = TextWrappingModes.NoWrap;
        bubble.outlineWidth = 0.25f;
        bubble.outlineColor = new Color32(30, 20, 20, 255);
        bubble.rectTransform.sizeDelta = new Vector2(4f, 1f);
        bubbleGo.SetActive(false);

        var bar = new GameObject("Patience Bar").transform;
        bar.SetParent(go.transform, false);
        bar.localPosition = new Vector3(0f, patienceBarHeight, 0f);
        BarQuad(bar, "Back", patienceBarSize + new Vector2(0.08f, 0.08f));
        MeshRenderer fill = BarQuad(bar, "Fill", patienceBarSize);

        var c = new Customer
        {
            go = go, root = go.transform, body = body, renderer = renderer, bubble = bubble,
            patienceBar = bar, patienceFill = fill.transform, patienceFillRenderer = fill
        };
        c.materials[0] = outlineMaterial;
        return c;
    }

    private MeshRenderer BarQuad(Transform parent, string name, Vector2 size)
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(quad.GetComponent<Collider>());
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = quad.GetComponent<MeshRenderer>();
        r.sharedMaterial = outlineMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }
}
