using System;
using UnityEngine;

/// <summary>
/// Central event system for game-wide communication.
/// WHY: Allows systems to communicate without direct references (loose coupling).
/// PATTERN: Static events that anyone can subscribe to or trigger.
/// </summary>
public static class GameEvents
{
    // ===== PERSON EVENTS =====

    /// <summary>
    /// Fired when any person's need becomes critical (below threshold).
    /// Listeners: UI (show warnings), AI (prioritize actions), Story (generate events)
    /// </summary>
    public static event Action<Person, NeedDefinition, float> OnNeedCritical;

    /// <summary>
    /// Fired when a person's state changes (idle → working, etc).
    /// Listeners: Animation system, UI, activity tracker
    /// </summary>
    public static event Action<Person, string, string> OnPersonStateChanged; // person, oldState, newState

    /// <summary>
    /// Fired when a person gains or improves a skill.
    /// Listeners: UI, achievement system, story generator
    /// </summary>
    public static event Action<Person, SkillDefinition, int> OnSkillChanged; // person, skill, newLevel

    // ===== MEMBER EVENTS (Commune-specific) =====

    /// <summary>
    /// Fired when a new member joins the commune.
    /// Listeners: Commune manager, UI, ideology system, story generator
    /// </summary>
    public static event Action<Member> OnMemberJoined;

    /// <summary>
    /// Fired when a member leaves or is exiled.
    /// Listeners: Commune manager, UI, relationship system, story generator
    /// </summary>
    public static event Action<Member, string> OnMemberLeft; // member, reason

    // ===== IDEOLOGY EVENTS =====

    /// <summary>
    /// Fired when a person adopts or changes a belief.
    /// Listeners: Ideology system, UI, relationship system, story generator
    /// </summary>
    public static event Action<Person, string, float> OnBeliefChanged; // person, beliefName, newAlignment

    // ===== RELATIONSHIP EVENTS =====

    /// <summary>
    /// Fired when two people interact socially.
    /// Listeners: Relationship system, story generator
    /// </summary>
    public static event Action<Person, Person, string> OnSocialInteraction; // person1, person2, interactionType

    // ===== UTILITY METHODS =====

    /// <summary>
    /// Trigger a need critical event.
    /// WHY: Centralized triggering helps with debugging and ensures consistency.
    /// </summary>
    public static void TriggerNeedCritical(Person person, NeedDefinition need, float currentValue)
    {
        OnNeedCritical?.Invoke(person, need, currentValue);

        // Optional: Log for debugging
        #if UNITY_EDITOR
        Debug.Log($"[EVENT] {person.PersonName}'s {need.needName} is critical: {currentValue:F1}");
        #endif
    }

    public static void TriggerPersonStateChanged(Person person, string oldState, string newState)
    {
        OnPersonStateChanged?.Invoke(person, oldState, newState);
    }

    public static void TriggerSkillChanged(Person person, SkillDefinition skill, int newLevel)
    {
        OnSkillChanged?.Invoke(person, skill, newLevel);
    }

    public static void TriggerMemberJoined(Member member)
    {
        OnMemberJoined?.Invoke(member);

        #if UNITY_EDITOR
        Debug.Log($"[EVENT] New member joined: {member.PersonName}");
        #endif
    }

    public static void TriggerMemberLeft(Member member, string reason)
    {
        OnMemberLeft?.Invoke(member, reason);
    }

    public static void TriggerBeliefChanged(Person person, string beliefName, float newAlignment)
    {
        OnBeliefChanged?.Invoke(person, beliefName, newAlignment);
    }

    public static void TriggerSocialInteraction(Person person1, Person person2, string interactionType)
    {
        OnSocialInteraction?.Invoke(person1, person2, interactionType);
    }

    /// <summary>
    /// Clear all event subscriptions (useful for scene transitions).
    /// WARNING: Only call this when transitioning scenes or cleaning up.
    /// </summary>
    public static void ClearAllEvents()
    {
        OnNeedCritical = null;
        OnPersonStateChanged = null;
        OnSkillChanged = null;
        OnMemberJoined = null;
        OnMemberLeft = null;
        OnBeliefChanged = null;
        OnSocialInteraction = null;
    }
}