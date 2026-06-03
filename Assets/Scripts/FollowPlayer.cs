using UnityEngine;
using UnityEngine.AI;
using System.Collections;


public class FollowPlayer : MonoBehaviour
{
    public string playerTag = "Player";
    [Header("AI Parameters")]
    [SerializeField] private float stoppingDistance = 5f; // State 2: Stop when not looked at
    [SerializeField] private float walkingDistance = 14f;  // State 3: Stop when looked at
    [SerializeField] private float startWalkingDistance = 15f; // State 1: Only start walking if above this
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 3f;

    private Transform player;
    private NavMeshAgent agent;

    // FSM States
    private enum AIState { State1_Idle, State2_Walking, State3_PlayerLooking }
    private AIState currentState = AIState.State1_Idle;

    private bool wasLookedAt = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;

        StartCoroutine(DelayedPlayerSearch());
    }

    private System.Collections.IEnumerator DelayedPlayerSearch()
    {
        while (player == null)
        {
            SearchForPlayerTag();
            yield return new WaitForSeconds(1f); // Check every second until player is found
        }
    }

    private void SearchForPlayerTag()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player object with tag '" + playerTag + "' not found!");
        }
    }

    void Update()
    {
        if (player == null || agent == null)
            return;

        Vector3 toObject = (transform.position - player.position).normalized;
        Vector3 playerForward = player.forward;
        float angle = Vector3.Angle(playerForward, toObject);
        float distance = Vector3.Distance(transform.position, player.position);
        agent.speed = Mathf.Lerp(minSpeed, maxSpeed, distance / 20f);

        bool isPlayerLooking = angle < 90f;

        switch (currentState)
        {
            case AIState.State1_Idle:
                // State 1: Idle, only start walking if not looked at and above startWalkingDistance
                if (!isPlayerLooking && distance > startWalkingDistance)
                {
                    currentState = AIState.State2_Walking;
                    agent.stoppingDistance = stoppingDistance;
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                else
                {
                    agent.isStopped = true;
                }
                break;

            case AIState.State2_Walking:
                // State 2: Walking toward player, stop if close enough or player looks
                if (isPlayerLooking)
                {
                    currentState = AIState.State3_PlayerLooking;
                    agent.stoppingDistance = walkingDistance;
                    wasLookedAt = true;
                }
                else if (distance <= agent.stoppingDistance)
                {
                    currentState = AIState.State1_Idle;
                    agent.isStopped = true;
                }
                else
                {
                    if (agent.isStopped)
                        agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                break;

            case AIState.State3_PlayerLooking:
                // State 3: Player is looking, stop at walkingDistance, return to walking if not looked at
                if (!isPlayerLooking)
                {
                    currentState = AIState.State2_Walking;
                    agent.stoppingDistance = stoppingDistance;
                }
                else if (distance <= agent.stoppingDistance)
                {
                    currentState = AIState.State1_Idle;
                    agent.isStopped = true;
                }
                else
                {
                    if (agent.isStopped)
                        agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
                break;
        }
    }
}
