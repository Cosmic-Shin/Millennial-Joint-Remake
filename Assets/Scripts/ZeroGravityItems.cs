using System.Collections;
using UnityEngine;

// Asteroid Diner: dropped/thrown items lose gravity and drift upwards while this map is active.
public class ZeroGravityItems : MonoBehaviour
{
    private void OnEnable() => DroppedItem.ZeroGravity = true;
    private void OnDisable() => DroppedItem.ZeroGravity = false;
}
