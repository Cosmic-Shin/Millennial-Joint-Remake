using UnityEngine;

// Constant local spin, e.g. propeller blades.
public class Spin : MonoBehaviour
{
    public Vector3 degreesPerSecond = new Vector3(0f, 720f, 0f);

    private void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
}
