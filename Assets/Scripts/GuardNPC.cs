using UnityEngine;
using UnityEngine.AI;

public class GuardNPC : MonoBehaviour
{
    // [Header("Detection Trigger")]
    // public Collider detectionTrigger; // Removed as requested
    public Transform flashlight; // Assign in inspector
    public float flashlightSweepAngle = 60f; // Total sweep (degrees)
    public float sweepSpeed = 30f; // Degrees per second
    public float fieldOfView = 30f; 
    public float viewDistance = 10f;
    public float caughtDistance = 2f;

    public bool canChase = true; // Set to false when player is in safe zone
    public float chaseBreakDistance = 30f;
    public string[] targetTags = { "Player", "Ghost" };

    private Vector3 originPosition;
    private Quaternion originRotation;
    private NavMeshAgent agent;
    private float sweepTimer = 0f;
    private bool sweepDirection = true; // true = right, false = left
    private Transform target;
    private const float timeToTrigger = 5f;

    public enum State { Guarding, Chasing, Returning }
    public State currentState = State.Guarding;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        originPosition = transform.position;
        originRotation = transform.rotation;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Guarding:
                GuardSweep();
                LookForTarget();
                break;
            case State.Chasing:
                CheckForChase();
                break;
            case State.Returning:
                ReturnToOrigin();
                break;
        }
    }

    private void CheckForChase()
    {
        if (canChase)
        {
            ChaseTarget();
            LookForTarget();
        }
        else
        {
            target = null;
            currentState = State.Returning;
        }
    }


    // Call this from your trigger object's script when something enters the trigger
    public void OnDetectionTriggerEnter(Collider other)
    {
        if (currentState == State.Guarding && canChase)
        {
            if (other.CompareTag("Player"))
            {
                target = other.transform;
                currentState = State.Chasing;
                Debug.Log($"[GuardNPC] Triggered by {other.name} (Player). PRIORITY: Chasing Player.");
            }
            else if (other.CompareTag("Ghost"))
            {
                target = other.transform;
                currentState = State.Chasing;
                Debug.Log($"[GuardNPC] Triggered by {other.name} (Ghost). PRIORITY: Chasing Ghost.");
            }
        }
    }


    void GuardSweep()
    {
        if (flashlight == null) return;
        float halfSweep = flashlightSweepAngle / 2f;
        float angle = Mathf.PingPong(Time.time * sweepSpeed, flashlightSweepAngle) - halfSweep;
        Vector3 euler = flashlight.localRotation.eulerAngles;
        flashlight.localRotation = Quaternion.Euler(euler.x, angle, euler.z);
    }

    void LookForTarget()
    {
        Vector3 fwd = flashlight.forward;
        Vector3 pos = flashlight.position;
        Collider[] hits = Physics.OverlapSphere(pos, viewDistance);
        Transform playerTransform = null;
        Transform ghostTransform = null;
        float playerDist = float.MaxValue;
        float ghostDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector3 dirToTarget = (hit.transform.position - pos).normalized;
                float angleToTarget = Vector3.Angle(fwd, dirToTarget);
                float distToTarget = Vector3.Distance(pos, hit.transform.position);
                if (angleToTarget < fieldOfView / 2f && distToTarget <= viewDistance)
                {
                    if (distToTarget < playerDist) 
                    {
                        playerDist = distToTarget;
                        playerTransform = hit.transform;
                    }
                }
            }
            else if (hit.CompareTag("Ghost"))
            {
                Vector3 dirToTarget = (hit.transform.position - pos).normalized;
                float angleToTarget = Vector3.Angle(fwd, dirToTarget);
                float distToTarget = Vector3.Distance(pos, hit.transform.position);
                if (angleToTarget < fieldOfView / 2f && distToTarget <= viewDistance)
                {
                    if (distToTarget < ghostDist) // Closest ghost
                    {
                        ghostDist = distToTarget;
                        ghostTransform = hit.transform;
                    }
                }
            }
        }

        if (playerTransform != null)
        {
            if (target != playerTransform || currentState != State.Chasing)
            {
                target = playerTransform;
                currentState = State.Chasing;
            }
        }
        else if (ghostTransform != null)
        {
            if (target != ghostTransform || currentState != State.Chasing)
            {
                target = ghostTransform;
                currentState = State.Chasing;
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[GuardNPC] OnTriggerEnter: {other.name} with tag {other.tag}");
        if (other.CompareTag("NPCEXIT"))
        {
            Debug.Log("[GuardNPC] Entered NPCEXIT zone. Cannot chase player.");
            canChase = false;
            target = null;
            currentState = State.Returning;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NPCEXIT"))
        {
            canChase = true;
        }
    }

    void ChaseTarget()
    {
        if (canChase == false)
        {
            Debug.Log("[GuardNPC] Cannot chase (player in safe zone). Returning to origin.");
            target = null;
            currentState = State.Returning;
            return;
        }
        if (target == null)
        {
            Debug.Log("[GuardNPC] Lost target during chase. Returning to origin.");
            currentState = State.Returning;
            return;
        }
        float dist = Vector3.Distance(transform.position, target.position);
        agent.isStopped = false;
        agent.SetDestination(target.position);
        // If target moves out of chaseBreakDistance, stop chasing
        if (dist > chaseBreakDistance)
        {
            Debug.Log($"[GuardNPC] Target escaped beyond {chaseBreakDistance} units. Returning to origin.");
            target = null;
            currentState = State.Returning;
            return;
        }
        if (dist <= caughtDistance)
        {
            if (target != null && target.CompareTag("Player"))
            {
                Debug.Log("[GuardNPC] Caught the Player! Reloading level.");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.Death();
                }
                target = null;
                currentState = State.Returning;
            }
        }
    }

    void ReturnToOrigin()
    {
        float dist = Vector3.Distance(transform.position, originPosition);
        agent.isStopped = false;
        agent.SetDestination(originPosition);
        if (dist < 0.5f)
        {
            Debug.Log("[GuardNPC] Arrived at origin. Resuming guard.");
            agent.isStopped = true;
            transform.rotation = originRotation;
            currentState = State.Guarding;
        }
    }
}
