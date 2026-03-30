using Unity.VisualScripting;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public bool doMove;
    public float speed;
    Rigidbody rb;
    Vector3 direction;
    bool front, left, back, right;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (rb == null) return;
        front = Input.GetKey(KeyCode.W);
        left  = Input.GetKey(KeyCode.A);
        back  = Input.GetKey(KeyCode.S);
        right = Input.GetKey(KeyCode.D);
    }

    private void FixedUpdate()
    {
        if (!doMove) return;

        if (rb == null) return;
        direction = new(
            (right ? 1 : 0) - (left ? 1 : 0),
            0,
            (front ? 1 : 0) - (back ? 1 : 0));

        if (front || left || back || right && direction != Vector3.zero)
        {
            rb.AddForce(direction.normalized * speed * 10);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), speed/2 * Time.deltaTime);
        }
    }
}
