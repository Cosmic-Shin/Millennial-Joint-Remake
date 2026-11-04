using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MapSelector : MonoBehaviour
{
    [System.Serializable]
    public class Layout
    {
        public string name;
        public GameObject root;
        public Transform player1Spawn;
        public Transform player2Spawn;
        public Collider[] powerUpAreas;

        [Tooltip("Optional: moves the CustomerQueue here (maps with their own customer platform)")]
        public Transform customerAnchor;
        public Transform[] queuePoints;
        [Tooltip("How far behind the queue customers appear (0 = CustomerQueue default)")]
        public float customerEntranceDistance;
        [Tooltip("Customers waiting in the queue face this (the Order Up counter)")]
        public Transform servingCounter;

        [Tooltip("Optional fixed camera framing for this map")]
        public Transform cameraFocus;
        public float cameraDistance;
    }

    public Layout[] layouts;

    [Header("Shared customer area")]
    public CustomerQueue customerQueue;
    [Tooltip("Shared customer bridge visuals, hidden on maps with a customerAnchor")]
    public GameObject[] customerBridgeVisuals;

    private Vector3 queueHome;
    private Quaternion queueHomeRotation;
    private float defaultEntranceDistance;

    public static MapSelector Instance { get; private set; }

    private static int lastIndex = -1;

    public Layout Current => lastIndex >= 0 && lastIndex < layouts.Length ? layouts[lastIndex] : null;

    private void Awake()
    {
        Instance = this;
        if (customerQueue != null)
        {
            queueHome = customerQueue.transform.position;
            queueHomeRotation = customerQueue.transform.rotation;
            defaultEntranceDistance = customerQueue.entranceDistance;
        }
        Choose(GameSession.Map);
    }

    // null (or an unknown layout name) = random, as before.
    public void Choose(MapDefinition map)
    {
        if (layouts == null || layouts.Length == 0)
            return;

        int count = layouts.Length;
        int index = map != null ? System.Array.FindIndex(layouts, l => l.name == map.layoutName) : -1;
        if (index < 0)
        {
            index = Random.Range(0, count);
            if (count > 1 && index == lastIndex)
                index = (index + Random.Range(1, count)) % count;
        }
        lastIndex = index;

        for (int i = 0; i < count; i++)
            if (layouts[i].root != null)
                layouts[i].root.SetActive(i == index);

        Layout chosen = layouts[index];

        foreach (PlayerControl player in FindObjectsByType<PlayerControl>(FindObjectsSortMode.None))
        {
            Transform spawn = player.playerNumber == 1 ? chosen.player1Spawn : chosen.player2Spawn;
            if (spawn == null)
                continue;

            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = spawn.position;
                body.linearVelocity = Vector3.zero;
            }
        }

        PlaceCustomers(chosen);

        CameraMovement cam = CameraMovement.Instance != null ? CameraMovement.Instance : FindFirstObjectByType<CameraMovement>();
        if (cam != null)
        {
            cam.fixedFocus = chosen.cameraFocus;
            cam.fixedDistance = chosen.cameraDistance;
        }

        PowerUpSpawner spawner = FindFirstObjectByType<PowerUpSpawner>();
        if (spawner != null && chosen.powerUpAreas != null && chosen.powerUpAreas.Length > 0)
            spawner.SetSpawnAreas(chosen.powerUpAreas);
    }

    private void PlaceCustomers(Layout chosen)
    {
        if (customerQueue == null)
            return;

        bool own = chosen.customerAnchor != null;
        Transform q = customerQueue.transform;
        if (own)
            q.SetPositionAndRotation(chosen.customerAnchor.position, chosen.customerAnchor.rotation);
        else
            q.SetPositionAndRotation(queueHome, queueHomeRotation);

        customerQueue.queuePoints = own ? chosen.queuePoints : null;
        customerQueue.faceTarget = own ? chosen.servingCounter : null;
        customerQueue.entranceDistance = own && chosen.customerEntranceDistance > 0f ? chosen.customerEntranceDistance : defaultEntranceDistance;

        foreach (GameObject visual in customerBridgeVisuals)
            if (visual != null)
                visual.SetActive(!own);
    }
}
