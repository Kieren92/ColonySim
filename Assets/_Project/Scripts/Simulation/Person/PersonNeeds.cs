using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all needs for a single person.
/// WHY: Encapsulates need logic, can be used by any Person type.
/// PATTERN: Component pattern - Person "has a" PersonNeeds.
/// </summary>
[System.Serializable]
public class PersonNeeds
{
    /// <summary>
    /// Individual need tracker - combines definition with current state.
    /// </summary>
    [System.Serializable]
    public class Need
    {
        public NeedDefinition definition;
        [Range(0f, 100f)]
        public float currentValue;

        public Need(NeedDefinition def)
        {
            definition = def;
            currentValue = def.defaultValue;
        }

        public bool IsCritical() => definition.IsCritical(currentValue);
        public bool IsEmergency() => definition.IsEmergency(currentValue);
    }

    // The person's actual needs
    private List<Need> needs = new List<Need>();

    // Reference to the person who owns these needs
    private Person owner;

    // Track state for decay modifiers
    private bool isWorking = false;
    private bool isResting = false;

    /// <summary>
    /// Initialize needs for a person.
    /// WHY: Needs must be set up before use.
    /// </summary>
    public void Initialize(Person person, List<NeedDefinition> needDefinitions)
    {
        owner = person;
        needs.Clear();

        foreach (var def in needDefinitions)
        {
            needs.Add(new Need(def));
        }
    }

    /// <summary>
    /// Update all needs over time (call every frame).
    /// </summary>
    public void UpdateNeeds(float deltaTime)
    {
        foreach (var need in needs)
        {
            // Calculate decay
            float decay = need.definition.CalculateDecay(deltaTime, isWorking, isResting);

            // Apply decay
            float previousValue = need.currentValue;
            need.currentValue = Mathf.Max(0f, need.currentValue - decay);

            // Check if need became critical this frame
            if (!need.definition.IsCritical(previousValue) && need.IsCritical())
            {
                GameEvents.TriggerNeedCritical(owner, need.definition, need.currentValue);
            }
        }
    }

    /// <summary>
    /// Satisfy a need by adding value to it.
    /// </summary>
    public void SatisfyNeed(string needName, float amount)
    {
        Need need = GetNeed(needName);
        if (need != null)
        {
            need.currentValue = Mathf.Min(100f, need.currentValue + amount);
        }
    }

    /// <summary>
    /// Get current value of a specific need.
    /// </summary>
    public float GetNeedValue(string needName)
    {
        Need need = GetNeed(needName);
        return need?.currentValue ?? 0f;
    }

    /// <summary>
    /// Get the most urgent (lowest) need.
    /// RETURNS: The need definition and its current value, or null if all satisfied.
    /// </summary>
    public (NeedDefinition definition, float value) GetMostUrgentNeed()
    {
        Need mostUrgent = null;
        float lowestValue = 100f;

        foreach (var need in needs)
        {
            if (need.currentValue < lowestValue)
            {
                lowestValue = need.currentValue;
                mostUrgent = need;
            }
        }

        // Only return if it's actually low
        if (mostUrgent != null && lowestValue < 60f)
        {
            return (mostUrgent.definition, lowestValue);
        }

        return (null, 0f);
    }

    /// <summary>
    /// Get all needs below a threshold.
    /// </summary>
    public List<(NeedDefinition definition, float value)> GetCriticalNeeds()
    {
        var critical = new List<(NeedDefinition, float)>();

        foreach (var need in needs)
        {
            if (need.IsCritical())
            {
                critical.Add((need.definition, need.currentValue));
            }
        }

        return critical;
    }

    /// <summary>
    /// Set activity state for decay modifiers.
    /// </summary>
    public void SetActivityState(bool working, bool resting)
    {
        isWorking = working;
        isResting = resting;
    }

    /// <summary>
    /// Get all needs (for UI display).
    /// </summary>
    public List<Need> GetAllNeeds() => new List<Need>(needs);

    // Helper to find a need by name
    private Need GetNeed(string needName)
    {
        return needs.Find(n => n.definition.needName == needName);
    }
}