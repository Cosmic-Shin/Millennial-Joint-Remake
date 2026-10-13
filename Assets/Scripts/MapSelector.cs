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
    }

    public Layout[] layouts;

    private static int lastIndex = -1;

    private void Awake()
    {
        if (layouts == null || layouts.Length == 0)
            return;

        int count = layouts.Length;
        int index = Random.Range(0, count);
        if (count > 1 && index == lastIndex)
            index = (index + Random.Range(1, count)) % count;
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

        PowerUpSpawner spawner = FindFirstObjectByType<PowerUpSpawner>();
        if (spawner != null && chosen.powerUpAreas != null && chosen.powerUpAreas.Length > 0)
            spawner.SetSpawnAreas(chosen.powerUpAreas);
    }
}
