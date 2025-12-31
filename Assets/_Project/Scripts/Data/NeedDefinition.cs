using UnityEngine;

/// <summary>
/// Defines a single need type (hunger, thirst, sleep, etc).
/// WHY ScriptableObject: Allows designers to create/edit needs without code.
/// PATTERN: Data-driven design - game data lives in assets, not hardcoded.
/// </summary>
[CreateAssetMenu(fileName = "New Need", menuName = "Ideology/Person/Need Definition")]
public class NeedDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name of this need")]
    public string needName = "Hunger";

    [Tooltip("Short description")]
    [TextArea(2, 4)]
    public string description = "The need to eat food regularly.";

    [Header("Gameplay Values")]
    [Tooltip("Starting value when a person is created (0-100)")]
    [Range(0f, 100f)]
    public float defaultValue = 70f;

    [Tooltip("How many points this need loses per in-game hour")]
    [Range(0f, 100f)]
    public float decayRatePerHour = 5f;

    [Tooltip("Below this value, the need is considered 'critical'")]
    [Range(0f, 100f)]
    public float criticalThreshold = 30f;

    [Tooltip("Below this value, severe consequences occur")]
    [Range(0f, 100f)]
    public float emergencyThreshold = 10f;

    [Header("Modifiers")]
    [Tooltip("Does this need decay faster/slower based on activity?")]
    public bool affectedByActivity = true;

    [Tooltip("Multiplier when person is working (typically faster decay)")]
    [Range(0f, 3f)]
    public float workingMultiplier = 1.5f;

    [Tooltip("Multiplier when person is resting (typically slower decay)")]
    [Range(0f, 3f)]
    public float restingMultiplier = 0.5f;

    [Header("Visual")]
    [Tooltip("Color used to represent this need in UI")]
    public Color uiColor = Color.red;

    [Tooltip("Icon for this need (optional, can be added later)")]
    public Sprite icon;

    /// <summary>
    /// Calculate actual decay for this frame based on modifiers.
    /// WHY: Centralizes the decay calculation logic.
    /// </summary>
    public float CalculateDecay(float deltaTime, bool isWorking, bool isResting)
    {
        float baseDecay = decayRatePerHour * (deltaTime / 3600f); // Convert per-hour to per-second

        if (!affectedByActivity)
            return baseDecay;

        if (isWorking)
            return baseDecay * workingMultiplier;

        if (isResting)
            return baseDecay * restingMultiplier;

        return baseDecay;
    }

    /// <summary>
    /// Is this need value in critical state?
    /// </summary>
    public bool IsCritical(float currentValue)
    {
        return currentValue <= criticalThreshold;
    }

    /// <summary>
    /// Is this need value in emergency state?
    /// </summary>
    public bool IsEmergency(float currentValue)
    {
        return currentValue <= emergencyThreshold;
    }
}