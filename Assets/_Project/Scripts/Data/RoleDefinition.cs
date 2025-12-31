using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a role (job) that members can be assigned to.
/// WHY: Player-created roles define what work a member will do.
/// PATTERN: Composition - roles are built from a set of allowed actions.
/// </summary>
[CreateAssetMenu(fileName = "New Role", menuName = "Ideology/Work/Role Definition")]
public class RoleDefinition : ScriptableObject
{
    [Header("Identity")]
    public string roleName = "Farmer";

    [TextArea(2, 4)]
    public string description = "Responsible for growing food and tending crops.";

    [Header("Allowed Actions")]
    [Tooltip("What actions can members with this role perform?")]
    public List<ActionDefinition> allowedActions = new List<ActionDefinition>();

    [Header("Action Priorities")]
    [Tooltip("Priority order for actions (higher = do first). Must match allowedActions.")]
    public List<int> actionPriorities = new List<int>();

    [Header("Role Properties")]
    [Tooltip("How many members should ideally have this role?")]
    public int targetMemberCount = 2;

    [Tooltip("Is this a leadership role?")]
    public bool isLeadershipRole = false;

    [Tooltip("Can members with this role be reassigned easily?")]
    public bool allowsReassignment = true;

    [Header("Ideology Integration")]
    [Tooltip("Does this role exist in collectivist communes?")]
    public bool availableInCollectivist = true;

    [Tooltip("Does this role exist in individualist communes?")]
    public bool availableInIndividualist = true;

    [Tooltip("Can members self-assign to this role in anarchist communes?")]
    public bool allowsSelfAssignment = true;

    [Header("Visual")]
    public Color roleColor = Color.green;
    public Sprite icon;

    /// <summary>
    /// Get the priority of a specific action.
    /// RETURNS: Priority (higher = more important), or -1 if action not allowed.
    /// </summary>
    public int GetActionPriority(ActionDefinition action)
    {
        int index = allowedActions.IndexOf(action);
        if (index == -1) return -1;

        if (index < actionPriorities.Count)
            return actionPriorities[index];

        return 0; // Default priority
    }

    /// <summary>
    /// Can this role perform a specific action?
    /// </summary>
    public bool CanPerformAction(ActionDefinition action)
    {
        return allowedActions.Contains(action);
    }

    /// <summary>
    /// Get all allowed actions sorted by priority (highest first).
    /// </summary>
    public List<ActionDefinition> GetActionsByPriority()
    {
        var sorted = new List<ActionDefinition>(allowedActions);

        sorted.Sort((a, b) =>
        {
            int priorityA = GetActionPriority(a);
            int priorityB = GetActionPriority(b);
            return priorityB.CompareTo(priorityA); // Descending order
        });

        return sorted;
    }
}