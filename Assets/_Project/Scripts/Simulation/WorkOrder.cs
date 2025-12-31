using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Source of work order - who created it and why.
/// </summary>
public enum WorkOrderSource
{
    PlayerManual,      // Player explicitly created it
    CommuneNeeds,      // AI detected commune needs (low food, etc)
    MemberVote,        // Democratic vote by members
    PlannedEconomy,    // Quota system / 5-year plan
    TownDemand,        // Town wants to trade for something
    LeaderDecision,    // Authoritarian leader commanded it
    Emergency          // Critical situation (starvation, etc)
}

/// <summary>
/// Type of work order - one-time vs ongoing.
/// </summary>
public enum WorkOrderType
{
    OneTime,           // Complete once (craft 10 shirts)
    Ongoing,           // Maintain continuously (keep 50 food)
    Repeating          // Repeat periodically (harvest crops weekly)
}

/// <summary>
/// A work order - specific task that needs doing.
/// WHY: Separates "what needs doing" from "who does it".
/// </summary>
public class WorkOrder
{
    // Identity
    public string OrderID { get; private set; }
    public string OrderName { get; set; }
    public string Description { get; set; }

    // What work
    public ActionDefinition RequiredAction { get; set; }
    public WorkOrderType Type { get; set; }
    public WorkOrderSource Source { get; set; }

    // Targets
    public int TargetQuantity { get; set; }        // How much to produce
    public int CurrentProgress { get; set; }       // How much completed
    public ItemDefinition TargetItem { get; set; } // What item (if applicable)
    public Building TargetBuilding { get; set; }   // Where to work (if applicable)

    // Priority
    public int Priority { get; set; }              // 1 (critical) to 5 (low)

    // Assignment
    public List<Member> AssignedMembers { get; private set; }
    public int MaxAssignedMembers { get; set; }

    // State
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public float CreatedTime { get; set; }
    public float Deadline { get; set; }            // 0 = no deadline

    public WorkOrder(ActionDefinition action, string name)
    {
        OrderID = System.Guid.NewGuid().ToString();
        RequiredAction = action;
        OrderName = name;
        AssignedMembers = new List<Member>();
        IsActive = true;
        IsCompleted = false;
        Priority = 3; // Default medium priority
        MaxAssignedMembers = 3;
        CreatedTime = Time.time;
    }

    /// <summary>
    /// Can this member be assigned to this order?
    /// </summary>
    public bool CanAssignMember(Member member)
    {
        if (!IsActive || IsCompleted) return false;
        if (AssignedMembers.Count >= MaxAssignedMembers) return false;
        if (AssignedMembers.Contains(member)) return false;

        // Check if member's role allows this action
        if (member.GetAssignedRole() != null)
        {
            if (!member.GetAssignedRole().CanPerformAction(RequiredAction))
                return false;
        }

        // Check skill requirements using the new system
        if (!RequiredAction.CanMemberPerform(member))
            return false;

        return true;
    }

    /// <summary>
    /// Assign a member to work on this order.
    /// </summary>
    public bool AssignMember(Member member)
    {
        if (!CanAssignMember(member)) return false;

        AssignedMembers.Add(member);
        Debug.Log($"{member.PersonName} assigned to work order: {OrderName}");
        return true;
    }

    /// <summary>
    /// Remove member from this order.
    /// </summary>
    public void UnassignMember(Member member)
    {
        AssignedMembers.Remove(member);
    }

    /// <summary>
    /// Update progress on this order.
    /// </summary>
    public void AddProgress(int amount)
    {
        CurrentProgress += amount;

        if (Type == WorkOrderType.OneTime && CurrentProgress >= TargetQuantity)
        {
            CompleteOrder();
        }
    }

    /// <summary>
    /// Mark order as completed.
    /// </summary>
    public void CompleteOrder()
    {
        IsCompleted = true;
        IsActive = false;

        // Unassign all members
        foreach (var member in AssignedMembers.ToArray())
        {
            UnassignMember(member);
        }

        Debug.Log($"Work order completed: {OrderName}");
    }

    /// <summary>
    /// Is this order urgent? (high priority or near deadline)
    /// </summary>
    public bool IsUrgent()
    {
        if (Priority <= 2) return true;

        if (Deadline > 0)
        {
            float timeRemaining = Deadline - Time.time;
            return timeRemaining < 3600f; // Less than 1 hour remaining
        }

        return false;
    }

    /// <summary>
    /// Calculate attractiveness of this order to a member.
    /// Higher = more likely to pick this order.
    /// </summary>
    public float CalculateAttractiveness(Member member)
    {
        float score = 0f;

        // Priority matters most
        score += (6 - Priority) * 20f; // Priority 1 = +100, Priority 5 = +20

        // Skill match bonus - check all relevant skills
        if (RequiredAction.skillContributions != null && RequiredAction.skillContributions.Length > 0)
        {
            foreach (var skillContrib in RequiredAction.skillContributions)
            {
                if (skillContrib.skill != null)
                {
                    int memberSkill = member.Skills.GetSkillLevel(skillContrib.skill.skillName);
                    score += memberSkill * 2f * skillContrib.weight; // Weight affects how much this skill matters
                }
            }
        }

        // Urgency bonus
        if (IsUrgent())
            score += 50f;

        // Ideological alignment
        score += RequiredAction.ideologyWeight * member.IdeologyAlignment / 10f;

        return score;
    }
}