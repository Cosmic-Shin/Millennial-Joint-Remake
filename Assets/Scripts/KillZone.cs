using UnityEngine;

// Trigger under gaps / edges: players who fall in respawn at their spawn point for the current map.
// (Dropped items already destroy themselves when they fall far enough.)
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerControl player = other.GetComponentInParent<PlayerControl>();
        MapSelector.Layout layout = MapSelector.Instance != null ? MapSelector.Instance.Current : null;
        if (player == null || layout == null)
            return;

        Transform spawn = player.playerNumber == 1 ? layout.player1Spawn : layout.player2Spawn;
        if (spawn == null)
            return;

        Rigidbody body = player.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.position = spawn.position;
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        VisualFx.Puff(spawn.position + Vector3.up * 0.2f, Color.white, 10, 0.4f);
    }
}
