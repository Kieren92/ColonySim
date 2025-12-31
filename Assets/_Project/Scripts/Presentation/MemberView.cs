using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Visual representation of a Member with pathfinding and building interaction.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MemberView : MonoBehaviour
{
    [Header("References")]
    private Member member;

    private Vector3? cachedTargetPosition = null;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float waypointReachedDistance = 0.5f;

    [Header("UI")]
    [SerializeField] private TextMeshPro nameLabel;
    [SerializeField] private TextMeshPro stateLabel;
    [SerializeField] private GameObject statusIndicator;

    // Components
    private CharacterController characterController;

    // Pathfinding
    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    private bool isFollowingPath = false;

    // Wandering
    private float wanderTimer = 0f;
    private float wanderInterval = 5f;

    // Stuck detection
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private const float STUCK_THRESHOLD = 0.1f; // If moved less than this in 1 second, consider stuck
    private const float STUCK_TIMEOUT = 3f; // Give up after 3 seconds of being stuck

    public void Initialize(Member memberData)
    {
        member = memberData;
        transform.position = member.Position;
        lastPosition = transform.position;
        CreateUIElements();
        UpdateDisplay();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (member == null) return;

        UpdateDisplay();
        CheckBuildingTarget();
        HandleMovement();
        DetectIfStuck();
        FaceUIToCamera();
    }

    /// <summary>
    /// Check if member wants to go to a building and handle that movement.
    /// </summary>
    private void CheckBuildingTarget()
    {
        Building targetBuilding = member.GetTargetBuilding();

        if (targetBuilding == null)
        {
            cachedTargetPosition = null; // Clear cache when no target
            stuckTimer = 0f; // Reset stuck timer
            return;
        }

        if (member.IsUsingBuilding())
        {
            cachedTargetPosition = null; // Clear cache when using building
            stuckTimer = 0f; // Reset stuck timer
            return;
        }

        // CRITICAL FIX: Only get position once, then cache it
        if (!isFollowingPath)
        {
            // Get the use position ONCE
            Vector3 targetWorldPos = targetBuilding.GetUsePosition();
            cachedTargetPosition = targetWorldPos;

            Debug.Log($"{member.PersonName}: Moving to {targetBuilding.Definition.buildingName} at {targetWorldPos}");
            SetDestination(targetWorldPos);
            stuckTimer = 0f; // Reset stuck timer when starting new path
            return;
        }

        // If we ARE following a path, check if we've arrived using cached position
        if (cachedTargetPosition.HasValue)
        {
            float distanceToTarget = Vector3.Distance(transform.position, cachedTargetPosition.Value);

            if (distanceToTarget <= 2f)
            {
                Debug.Log($"{member.PersonName}: Arrived at {targetBuilding.Definition.buildingName}! (distance: {distanceToTarget:F2})");

                StopMovement();
                member.OnArrivedAtBuilding();
                cachedTargetPosition = null; // Clear cache after arrival
                stuckTimer = 0f; // Reset stuck timer
            }
            else if (!isFollowingPath && distanceToTarget <= 3f)
            {
                Debug.Log($"{member.PersonName}: Fallback arrival at {targetBuilding.Definition.buildingName} (distance: {distanceToTarget:F2})");

                member.OnArrivedAtBuilding();
                cachedTargetPosition = null; // Clear cache after arrival
                stuckTimer = 0f; // Reset stuck timer
            }
        }
    }

    /// <summary>
    /// Detect if the member is stuck and hasn't moved in a while.
    /// </summary>
    private void DetectIfStuck()
    {
        // Only check if following a path
        if (!isFollowingPath)
        {
            stuckTimer = 0f;
            lastPosition = transform.position;
            return;
        }

        // Calculate how far we've moved since last check
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < STUCK_THRESHOLD)
        {
            // Not moving much - increment stuck timer
            stuckTimer += Time.deltaTime;

            if (stuckTimer >= STUCK_TIMEOUT)
            {
                Debug.LogWarning($"{member.PersonName}: Stuck for {STUCK_TIMEOUT}s! Giving up on current path.");

                // Clear the target building so simulation can try again or do something else
                if (member.GetTargetBuilding() != null)
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
            // Moving successfully - reset stuck timer
            stuckTimer = 0f;
        }

        // Update last position for next check
        lastPosition = transform.position;
    }

    /// <summary>
    /// Handle all movement logic.
    /// </summary>
    private void HandleMovement()
    {
        // Don't wander if we have a building target
        if (member.GetTargetBuilding() != null)
        {
            return; // CheckBuildingTarget() handles this
        }

        if (isFollowingPath)
        {
            FollowPath();
        }
        else
        {
            // Don't wander if using a building
            if (!member.IsUsingBuilding())
            {
                wanderTimer += Time.deltaTime;
                if (wanderTimer >= wanderInterval)
                {
                    wanderTimer = 0f;
                    WanderToRandomLocation();
                }
            }
        }
    }

    /// <summary>
    /// Follow the current path to destination.
    /// </summary>
    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            isFollowingPath = false;
            return;
        }

        // Get current waypoint
        Vector3 targetWaypoint = currentPath[currentWaypointIndex];
        targetWaypoint.y = transform.position.y;

        // Move towards waypoint
        Vector3 direction = (targetWaypoint - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            // Rotate towards direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            characterController.Move(movement);

            // Update member's position
            member.Position = transform.position;
        }

        // Check if reached waypoint
        float distance = Vector3.Distance(transform.position, targetWaypoint);
        if (distance < waypointReachedDistance)
        {
            currentWaypointIndex++;

            // Reached end of path?
            if (currentWaypointIndex >= currentPath.Count)
            {
                isFollowingPath = false;
                currentPath = null;
                currentWaypointIndex = 0;
                Debug.Log($"{member.PersonName}: Reached end of path");
            }
        }
    }

    /// <summary>
    /// Move to a random nearby walkable location.
    /// </summary>
    private void WanderToRandomLocation()
    {
        if (GridSystem.Instance == null) return;

        Vector2Int currentGridPos = GridSystem.Instance.WorldToGrid(transform.position);
        Vector2Int targetGridPos = new Vector2Int(
            currentGridPos.x + Random.Range(-10, 11),
            currentGridPos.y + Random.Range(-10, 11)
        );

        GridCell targetCell = GridSystem.Instance.GetCell(targetGridPos);
        if (targetCell != null && targetCell.IsWalkable && !targetCell.IsOccupied)
        {
            SetDestination(targetCell.WorldPosition);
        }
    }

    /// <summary>
    /// Set a destination and find a path to it.
    /// </summary>
    public void SetDestination(Vector3 worldPosition)
    {
        if (GridSystem.Instance == null)
        {
            Debug.LogWarning($"{member.PersonName}: GridSystem is null!");
            return;
        }

        List<Vector3> path = GridSystem.Instance.FindPath(transform.position, worldPosition);

        if (path != null && path.Count > 0)
        {
            currentPath = path;
            currentWaypointIndex = 0;
            isFollowingPath = true;
            lastPosition = transform.position; // Reset last position for stuck detection
            stuckTimer = 0f; // Reset stuck timer
            Debug.Log($"{member.PersonName}: Path found with {path.Count} waypoints");
        }
        else
        {
            Debug.LogWarning($"{member.PersonName}: Could not find path to {worldPosition}");

            // If we have a building target and can't path to it, clear it
            if (member.GetTargetBuilding() != null)
            {
                Debug.Log($"{member.PersonName}: Can't path to building, clearing target");
                member.ClearTargetBuilding();
            }
        }
    }

    /// <summary>
    /// Stop current movement.
    /// </summary>
    public void StopMovement()
    {
        isFollowingPath = false;
        currentPath = null;
        currentWaypointIndex = 0;
        stuckTimer = 0f;
    }

    private void UpdateDisplay()
    {
        if (nameLabel != null)
        {
            nameLabel.text = member.PersonName;
        }

        if (stateLabel != null)
        {
            stateLabel.text = member.CurrentState;
            stateLabel.color = GetStateColor(member.CurrentState);
        }

        if (statusIndicator != null)
        {
            var renderer = statusIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetStateColor(member.CurrentState);
            }
        }
    }

    private Color GetStateColor(string state)
    {
        switch (state)
        {
            case "Idle": return Color.green;
            case "SeekingFood": return Color.yellow;
            case "Resting": return Color.blue;
            case "Socializing": return Color.magenta;
            case "Working": return Color.cyan;
            default: return Color.white;
        }
    }

    private void CreateUIElements()
    {
        GameObject nameObj = new GameObject("NameLabel");
        nameObj.transform.SetParent(transform);
        nameObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        nameLabel = nameObj.AddComponent<TextMeshPro>();
        nameLabel.fontSize = 3;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;

        GameObject stateObj = new GameObject("StateLabel");
        stateObj.transform.SetParent(transform);
        stateObj.transform.localPosition = new Vector3(0, 2.2f, 0);
        stateLabel = stateObj.AddComponent<TextMeshPro>();
        stateLabel.fontSize = 2;
        stateLabel.alignment = TextAlignmentOptions.Center;

        statusIndicator = transform.Find("StatusIndicator")?.gameObject;
    }

    private void FaceUIToCamera()
    {
        if (Camera.main == null) return;

        if (nameLabel != null)
        {
            nameLabel.transform.LookAt(Camera.main.transform);
            nameLabel.transform.Rotate(0, 180, 0);
        }

        if (stateLabel != null)
        {
            stateLabel.transform.LookAt(Camera.main.transform);
            stateLabel.transform.Rotate(0, 180, 0);
        }
    }

    public Member GetMember() => member;

    private void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i] + Vector3.up * 0.1f, currentPath[i + 1] + Vector3.up * 0.1f);
            }

            if (currentWaypointIndex < currentPath.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentPath[currentWaypointIndex], 0.3f);
            }
        }
    }
}