using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))] // We'll use the Health script we made
public class EnemyAI : MonoBehaviour
{
    // Define the AI's possible states
    private enum AIState
    {
        Idle,
        Chasing,
        Attacking,
        Fleeing
    }

    private AIState currentState;

    [Header("Core References")]
    public Transform player; // Set this to your player's transform
    private NavMeshAgent navAgent;
    private Health health;
    public AIWeaponController weapon; // The AI's gun script

    [Header("AI Settings")]
    public float sightRange = 20f;
    public float attackRange = 10f;
    public float fleeDistance = 15f;
    public float fleeDuration = 3.0f;
    public float strafeDistance = 3f;

    private bool isFleeing = false;
    private bool isStrafing = false; // --- NEW --- To control dodging

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();

        // Find the player by their tag (Make sure your player has this tag!)
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("AI cannot find GameObject with tag 'Player'!");
        }

        if (weapon == null)
        {
            Debug.LogError("AI is missing its AIWeaponController reference!");
        }

        // --- IMPORTANT ---
        // We tell our Health script to notify *this* script when it gets hurt.
        health.OnDamaged += ReactToDamage;

        currentState = AIState.Idle;
    }

    void Update()
    {
        if (player == null || !health.IsAlive())
        {
            if (navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
            }
            return;
        }

        // Run the logic for the current state
        switch (currentState)
        {
            case AIState.Idle:
                IdleState();
                break;
            case AIState.Chasing:
                ChasingState();
                break;
            case AIState.Attacking:
                AttackingState();
                break;
            case AIState.Fleeing:
                FleeingState();
                break;
        }
    }

    // --- STATE LOGIC ---

    private void IdleState()
    {
        // Stand still
        if (navAgent.isOnNavMesh) navAgent.isStopped = true;

        // Look for the player
        if (Vector3.Distance(transform.position, player.position) <= sightRange)
        {
            ChangeState(AIState.Chasing);
        }
    }

    private void ChasingState()
    {
        // Stop any dodging
        if (isStrafing)
        {
            StopCoroutine(StrafeCoroutine());
            isStrafing = false;
        }

        // Move towards the player
        if (navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(player.position);
        }

        // If in attack range, switch to Attacking
        if (Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            ChangeState(AIState.Attacking);
        }
        // If player gets too far away, go back to Idle
        else if (Vector3.Distance(transform.position, player.position) > sightRange)
        {
            ChangeState(AIState.Idle);
        }
    }

    private void AttackingState()
    {
        // Look at the player
        Vector3 lookPos = player.position - transform.position;
        lookPos.y = 0; // Keep the AI from tilting up or down
        transform.rotation = Quaternion.LookRotation(lookPos);

        // Shoot at the player
        if (weapon != null)
        {
            weapon.Shoot(player.position); // Tell the gun to shoot
        }

        // --- NEW DODGING/STRAFING LOGIC ---
        // If we aren't already strafing and our last move is done, start a new strafe
        if (!isStrafing && navAgent.isOnNavMesh && navAgent.remainingDistance < 0.5f)
        {
            StartCoroutine(StrafeCoroutine());
        }
        // --- END NEW LOGIC ---

        // If player gets out of attack range, go back to Chasing
        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            ChangeState(AIState.Chasing);
        }
    }

    private void FleeingState()
    {
        // Stop any dodging
        if (isStrafing)
        {
            StopCoroutine(StrafeCoroutine());
            isStrafing = false;
        }

        // The logic to find a flee point is handled in the ReactToDamage coroutine
        // This state just waits until the coroutine is done
        if (!isFleeing)
        {
            // If we are not actively running, switch to chasing
            ChangeState(AIState.Chasing);
        }
    }

    // --- NEW DODGE/STRAFE COROUTINE ---
    private IEnumerator StrafeCoroutine()
    {
        isStrafing = true;

        // Pick a random side (left or right)
        float direction = (Random.value > 0.5f) ? 1f : -1f;

        // Get a target position to the side
        Vector3 strafeTarget = transform.position + (transform.right * direction * strafeDistance);

        // Find the closest valid point on the NavMesh
        if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit hit, strafeDistance, NavMesh.AllAreas))
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(hit.position);
        }

        // Wait until the AI reaches the strafe point
        yield return new WaitUntil(() => navAgent.remainingDistance < 0.5f || !navAgent.hasPath);

        // Stop moving
        if (navAgent.isOnNavMesh) navAgent.isStopped = true;

        // Wait a random time before the next strafe
        yield return new WaitForSeconds(Random.Range(0.5f, 2f));

        isStrafing = false;
    }

    // --- STATE MANAGEMENT ---

    private void ChangeState(AIState newState)
    {
        if (currentState == newState || isFleeing) return;
        currentState = newState;
    }

    // This is called by our Health script!
    public void ReactToDamage()
    {
        // Don't flee if already fleeing or dead
        if (isFleeing || !health.IsAlive()) return;

        StartCoroutine(FleeCoroutine());
    }

    private IEnumerator FleeCoroutine()
    {
        isFleeing = true;
        ChangeState(AIState.Fleeing);
        Debug.Log("AI is fleeing!");

        // Find a direction away from the player
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        Vector3 fleeTarget = transform.position + directionAwayFromPlayer * fleeDistance;

        // Find the closest valid point on the NavMesh
        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, fleeDistance, NavMesh.AllAreas))
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(hit.position);
        }

        // Wait for the flee duration
        yield return new WaitForSeconds(fleeDuration);

        // Fleeing is over, go back to chasing
        isFleeing = false;
        ChangeState(AIState.Chasing);
    }

    // Draw gizmos in the editor to see the AI's ranges
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, fleeDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, strafeDistance);
    }
}

