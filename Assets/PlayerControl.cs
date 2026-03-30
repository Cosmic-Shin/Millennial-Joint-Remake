using Unity.VisualScripting;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{
    public bool doMove;
    public float speed;
    Rigidbody rb;
    Vector3 direction;
    bool front, left, back, right;

    public KeyCode MoveUp = KeyCode.W;
    public KeyCode MoveLeft = KeyCode.A;
    public KeyCode MoveDown = KeyCode.S;
    public KeyCode MoveRight = KeyCode.D;

    public KeyCode PrimaryAction = KeyCode.F;
    public KeyCode SecondaryAction = KeyCode.Space;
    public KeyCode Run = KeyCode.LeftShift;
    public KeyCode DropItem = KeyCode.Q;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }



    //Input Check
    private void Update()
    {
        if (rb == null) return;
        front = Input.GetKey(MoveUp);
        left = Input.GetKey(MoveLeft);
        back = Input.GetKey(MoveDown);
        right = Input.GetKey(MoveRight);

        
        if (Input.GetKeyDown(PrimaryAction))
        {
            Debug.LogWarning("Not implemented");
        }

        if (Input.GetKeyDown(SecondaryAction))
        {
            // Nearest Object Detection
            Collider[] hits = Physics.OverlapSphere(transform.position, castRadius, hitMask);
            Collider nearest = null;
            float nearestDist = Mathf.Infinity;
            foreach (Collider hit in hits)
            {
                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = hit;
                }
            }
            if (nearest != null)
            {
                nearest.GetComponent<IInteractable>().Interact(this);
            }
        }

        if (Input.GetKeyDown(DropItem))
        {
            Debug.LogWarning("Not implemented");
        }




        
    }



    //Movement
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
            if (Input.GetKeyDown(Run))
                rb.AddForce(direction.normalized * speed * 20);
            else
                rb.AddForce(direction.normalized * speed * 10);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), speed / 2 * Time.deltaTime);
        }
    }


    [SerializeField] private float castRadius = 0.5f;
    [SerializeField] private LayerMask hitMask;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);

    }
}
