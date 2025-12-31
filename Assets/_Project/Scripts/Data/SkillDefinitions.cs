using UnityEngine;

/// <summary>
/// Category of skill for organization.
/// </summary>
public enum SkillCategory
{
    Physical,      // Strength, dexterity, stamina
    Mental,        // Intelligence, planning, research
    Social,        // Charisma, teaching, negotiation
    Crafting,      // Making and repairing things
    Survival,      // Farming, foraging, medicine
    Combat,        // Fighting and defense
    Artistic       // Music, art, writing
}

/// <summary>
/// Defines a skill type that members can have and improve.
/// WHY ScriptableObject: Centralized skill definitions, no string typos.
/// </summary>
[CreateAssetMenu(fileName = "New Skill", menuName = "Ideology/Person/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    public string skillName = "Farming";

    [TextArea(2, 4)]
    public string description = "Ability to grow crops and tend to plants.";

    public SkillCategory category = SkillCategory.Survival;

    [Header("Progression")]
    [Tooltip("Maximum skill level")]
    [Range(1, 20)]
    public int maxLevel = 20;

    [Tooltip("How quickly this skill is learned (higher = faster)")]
    [Range(0.1f, 5f)]
    public float learningRate = 1f;

    [Tooltip("Experience curve multiplier (higher = harder to level up)")]
    [Range(1f, 3f)]
    public float experienceCurve = 1.5f;

    [Header("Effects")]
    [Tooltip("Speed bonus per skill level (e.g., 0.05 = 5% faster per level)")]
    [Range(0f, 0.2f)]
    public float speedBonusPerLevel = 0.05f;

    [Tooltip("Quality bonus per skill level (e.g., 0.03 = 3% better quality per level)")]
    [Range(0f, 0.2f)]
    public float qualityBonusPerLevel = 0.03f;

    [Header("Decay")]
    [Tooltip("Does this skill decay when not used?")]
    public bool canDecay = false;

    [Tooltip("Decay rate per in-game week")]
    [Range(0f, 1f)]
    public float decayRate = 0.1f;

    [Header("Visual")]
    public Color skillColor = Color.cyan;
    public Sprite icon;

    /// <summary>
    /// Calculate XP required for next level.
    /// </summary>
    public float GetExperienceForLevel(int level)
    {
        return 100f * Mathf.Pow(level, experienceCurve);
    }

    /// <summary>
    /// Get speed multiplier for a given skill level.
    /// EXAMPLE: Level 10 with 0.05 bonus = 1.5x speed
    /// </summary>
    public float GetSpeedMultiplier(int level)
    {
        return 1f + (level * speedBonusPerLevel);
    }

    /// <summary>
    /// Get quality multiplier for a given skill level.
    /// </summary>
    public float GetQualityMultiplier(int level)
    {
        return 1f + (level * qualityBonusPerLevel);
    }
}