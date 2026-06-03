using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FPMovementController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 7f;
    public float jumpForce = 5f;
    public float jumpCheckDistance = 0.2f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private bool isJumping = false;
    private bool isLanding = false;
    private bool isRunning = false;

    public bool IsRunning {
        get { return isRunning; }
        set { isRunning = value; }
    }
    public bool IsLanding {
        get { return isLanding; }
        set { isLanding = value; }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    void Update()
    {
        // WASD movement
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v).normalized;

        if (!isJumping && move.magnitude > 0.1f)
        {
                isRunning = Input.GetKey(KeyCode.LeftShift);
                agent.speed = isRunning ? runSpeed : walkSpeed;
            agent.destination = transform.position + move;
        }

        // Jump
        if (!isJumping && Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            isJumping = true;
            agent.enabled = false;
            agent.updatePosition = false;
            agent.updateRotation = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        if (isJumping && IsGrounded() && rb.linearVelocity.y <= 0.01f)
        {
            isJumping = false;
                isLanding = true;
            rb.isKinematic = true;
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
    }

    bool IsGrounded()
    {
        // Raycast from slightly above the bottom of the collider
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, jumpCheckDistance + 0.1f);
    }
}
