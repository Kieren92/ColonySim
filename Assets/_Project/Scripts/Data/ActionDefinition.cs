using UnityEngine;

/// <summary>
/// Categories of actions for organization.
/// </summary>
public enum ActionCategory
{
    Production,      // Making things
    Maintenance,     // Upkeep and repair
    Harvesting,      // Gathering resources
    Construction,    // Building structures
    Social,          // Interactions with people
    Defense,         // Security and combat
    External,        // Town interactions
    Research,        // Learning and development
    Logistics        // Moving and organizing things
}

/// <summary>
/// Defines a single action that members can perform.
/// WHY: Central registry of all possible actions in the game.
/// </summary>
[CreateAssetMenu(fileName = "New Action", menuName = "Ideology/Work/Action Definition")]
public class ActionDefinition : ScriptableObject
{
    [Header("Identity")]
    public string actionName = "Farming";

    [TextArea(2, 3)]
    public string description = "Growing and harvesting crops.";

    public ActionCategory category = ActionCategory.Production;

    [Header("Skill Requirements")]
    [Tooltip("Which skills affect this action and how much?")]
    public SkillContribution[] skillContributions;

    [Tooltip("How do multiple skills combine?")]
    public SkillCombineMode skillCombineMode = SkillCombineMode.Additive;

    [Tooltip("How physically demanding is this? (affects energy drain)")]
    [Range(0f, 2f)]
    public float physicalDemand = 1f;

    [Header("Work Context")]
    [Tooltip("Can this action be done at work buildings?")]
    public bool requiresBuilding = true;

    [Tooltip("Which building types support this action?")]
    public string[] compatibleBuildings = { "Farm" };

    [Header("Ideology Considerations")]
    [Tooltip("Some ideologies may restrict who can do this")]
    public bool canBeRestricted = false;

    [Tooltip("Ideological significance (-5 to +5, 0 = neutral)")]
    [Range(-5f, 5f)]
    public float ideologyWeight = 0f;

    [Header("Visual")]
    public Color actionColor = Color.green;
    public Sprite icon;

    /// <summary>
    /// Calculate how effectively a member can perform this action.
    /// RETURNS: (speedMultiplier, qualityMultiplier)
    /// </summary>
    public (float speed, float quality) CalculateEffectiveness(Member member)
    {
        float speed = SkillCalculator.CalculateSpeedMultiplier(
            member,
            skillContributions,
            skillCombineMode
        );

        float quality = SkillCalculator.CalculateQualityMultiplier(
            member,
            skillContributions,
            skillCombineMode
        );

        return (speed, quality);
    }

    /// <summary>
    /// Can this member perform this action?
    /// </summary>
    public bool CanMemberPerform(Member member)
    {
        return SkillCalculator.MeetsSkillRequirements(member, skillContributions);
    }
}