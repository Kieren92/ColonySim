using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Ideology.Structures;

public class MemberView : MonoBehaviour
{
    private Member member;
    private CharacterController controller;
    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    private bool isFollowingPath = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float waypointReachedDistance = 0.2f;

    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderInterval = 5f;
    private float wanderTimer = 0f;

    // Stuck detection
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_TIMEOUT = 3f;

    public void Initialize(Member memberData)
    {
        member = memberData;
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.radius = 0.3f;
            controller.height = 1.8f;
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (member == null) return;

        CheckBuildingTarget();
        HandleMovement();
        DetectIfStuck();
    }

    private void CheckBuildingTarget()
    {
        Structure targetStructure = member.GetTargetStructure();

        if (targetStructure == null) return;

        // Check if already at building
        float distanceToBuilding = Vector3.Distance(transform.position, targetStructure.WorldPosition);

        if (distanceToBuilding < 2f && !isFollowingPath)
        {
            member.OnArrivedAtBuilding();
            StopMovement();
            return;
        }

        // Not at building yet - path to it
        if (!isFollowingPath)
        {
            Vector3 usePosition = targetStructure.GetUsePosition(member);

            Debug.Log($"{member.PersonName}: Moving to {targetStructure.Definition.structureName} at {usePosition}");
            SetDestination(usePosition);

            stuckTimer = 0f;
        }
    }

    private void DetectIfStuck()
    {
        Debug.Log($"{member.PersonName}: Position={(int)transform.position.x},{(int)transform.position.z}, " +
          $"Target={currentDestination}, Distance={Vector3.Distance(transform.position, currentDestination):F2}, " +
          $"Velocity={navMeshAgent.velocity.magnitude:F2}");

        if (!isFollowingPath)
        {

            stuckTimer = 0f;
            lastPosition = transform.position;
            return;
        }

        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < STUCK_THRESHOLD)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= STUCK_TIMEOUT)
            {
                Debug.LogWarning($"{member.PersonName}: Stuck for {STUCK_TIMEOUT}s! Giving up on current path.");

                if (member.GetTargetStructure() != null)
                {
                    Debug.Log($"{member.PersonName}: Clearing unreachable target building");
                    member.ClearTargetBuilding();
                }

                StopMovement();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    private void HandleMovement()
    {
        if (member.GetTargetStructure() != null)
        {
            return;
        }

        if (isFollowingPath)
        {
            FollowPath();
        }
        else
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderInterval)
            {
                wanderTimer = 0f;
                WanderToRandomLocation();
            }
        }
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            StopMovement();
            return;
        }

        Vector3 targetWaypoint = currentPath[currentWaypointIndex];
        Vector3 direction = (targetWaypoint - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        controller.Move(movement);



        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
        if (distanceToWaypoint < waypointReachedDistance)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= currentPath.Count)
            {
                Debug.Log($"{member.PersonName}: Reached end of path");
                StopMovement();
            }
        }
    }

    private void WanderToRandomLocation()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        Vector2Int gridPos = GridSystem.Instance.WorldToGrid(randomPoint);
        if (GridSystem.Instance.IsCellWalkable(gridPos))
        {
            Vector3 targetPos = GridSystem.Instance.GridToWorld(gridPos);
            SetDestination(targetPos);
        }
    }

    private void SetDestination(Vector3 destination)
    {
        Vector2Int startGrid = GridSystem.Instance.WorldToGrid(transform.position);
        Vector2Int endGrid = GridSystem.Instance.WorldToGrid(destination);

        List<Vector3> worldPath = GridSystem.Instance.FindPath(startGrid, endGrid);

        if (worldPath != null && worldPath.Count > 0)
        {
            currentPath = worldPath; // Already in world positions
            currentWaypointIndex = 0;
            isFollowingPath = true;

            stuckTimer = 0f;
            lastPosition = transform.position;

            Debug.Log($"{member.PersonName}: Path found with {currentPath.Count} waypoints");
        }
        else
        {
            Debug.LogWarning($"{member.PersonName}: No path found to destination");
            StopMovement();
        }
    }

    private void StopMovement()
    {
        isFollowingPath = false;
        currentPath = null;
        currentWaypointIndex = 0;
    }

    public Member GetMember() => member;
}