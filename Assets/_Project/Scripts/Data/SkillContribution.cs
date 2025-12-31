using UnityEngine;

/// <summary>
/// How multiple skills combine to affect an outcome.
/// </summary>
public enum SkillCombineMode
{
    Additive,        // Sum all bonuses: skill1 + skill2 + skill3
    Multiplicative,  // Multiply all bonuses: skill1 * skill2 * skill3
    WeightedAverage, // Average based on weights
    DominantSkill    // Only the highest skill matters
}

/// <summary>
/// Defines how a single skill contributes to an action's effectiveness.
/// WHY: Actions can be affected by multiple skills with different weights.
/// </summary>
[System.Serializable]
public class SkillContribution
{
    [Tooltip("Which skill affects this action?")]
    public SkillDefinition skill;

    [Tooltip("How much does this skill matter? (0-1, where 1.0 = 100% weight)")]
    [Range(0f, 1f)]
    public float weight = 1f;

    [Tooltip("Does this skill affect speed?")]
    public bool affectsSpeed = true;

    [Tooltip("Does this skill affect quality?")]
    public bool affectsQuality = true;

    [Tooltip("Minimum skill level required (-1 = no requirement)")]
    public int minimumLevel = -1;
}

/// <summary>
/// Helper class for calculating combined skill effects.
/// </summary>
public static class SkillCalculator
{
    /// <summary>
    /// Calculate total speed multiplier from multiple skill contributions.
    /// EXAMPLE: 
    /// - Farming level 10 (weight 1.0) = 1.5x speed
    /// - Strength level 5 (weight 0.3) = 1.08x speed
    /// - Additive result = 1.58x speed total
    /// </summary>
    public static float CalculateSpeedMultiplier(
        Member member,
        SkillContribution[] contributions,
        SkillCombineMode mode)
    {
        if (contributions == null || contributions.Length == 0)
            return 1f;

        switch (mode)
        {
            case SkillCombineMode.Additive:
                return CalculateAdditive(member, contributions, true);

            case SkillCombineMode.Multiplicative:
                return CalculateMultiplicative(member, contributions, true);

            case SkillCombineMode.WeightedAverage:
                return CalculateWeightedAverage(member, contributions, true);

            case SkillCombineMode.DominantSkill:
                return CalculateDominant(member, contributions, true);

            default:
                return 1f;
        }
    }

    /// <summary>
    /// Calculate total quality multiplier from multiple skill contributions.
    /// </summary>
    public static float CalculateQualityMultiplier(
        Member member,
        SkillContribution[] contributions,
        SkillCombineMode mode)
    {
        if (contributions == null || contributions.Length == 0)
            return 1f;

        switch (mode)
        {
            case SkillCombineMode.Additive:
                return CalculateAdditive(member, contributions, false);

            case SkillCombineMode.Multiplicative:
                return CalculateMultiplicative(member, contributions, false);

            case SkillCombineMode.WeightedAverage:
                return CalculateWeightedAverage(member, contributions, false);

            case SkillCombineMode.DominantSkill:
                return CalculateDominant(member, contributions, false);

            default:
                return 1f;
        }
    }

    /// <summary>
    /// Additive: Sum all weighted bonuses.
    /// FORMULA: 1.0 + (bonus1 * weight1) + (bonus2 * weight2) + ...
    /// </summary>
    private static float CalculateAdditive(
        Member member,
        SkillContribution[] contributions,
        bool isSpeed)
    {
        float total = 1f; // Start at baseline (1.0x)

        foreach (var contribution in contributions)
        {
            if (contribution.skill == null) continue;
            if (isSpeed && !contribution.affectsSpeed) continue;
            if (!isSpeed && !contribution.affectsQuality) continue;

            int skillLevel = member.Skills.GetSkillLevel(contribution.skill.skillName);

            // Check minimum requirement
            if (contribution.minimumLevel > 0 && skillLevel < contribution.minimumLevel)
                continue;

            // Get bonus from skill
            float bonus = isSpeed
                ? contribution.skill.GetSpeedMultiplier(skillLevel) - 1f  // Convert 1.5x to 0.5 bonus
                : contribution.skill.GetQualityMultiplier(skillLevel) - 1f;

            // Apply weight
            total += bonus * contribution.weight;
        }

        return total;
    }

    /// <summary>
    /// Multiplicative: Multiply all weighted multipliers.
    /// FORMULA: multiplier1^weight1 * multiplier2^weight2 * ...
    /// </summary>
    private static float CalculateMultiplicative(
        Member member,
        SkillContribution[] contributions,
        bool isSpeed)
    {
        float result = 1f;

        foreach (var contribution in contributions)
        {
            if (contribution.skill == null) continue;
            if (isSpeed && !contribution.affectsSpeed) continue;
            if (!isSpeed && !contribution.affectsQuality) continue;

            int skillLevel = member.Skills.GetSkillLevel(contribution.skill.skillName);

            if (contribution.minimumLevel > 0 && skillLevel < contribution.minimumLevel)
                continue;

            float multiplier = isSpeed
                ? contribution.skill.GetSpeedMultiplier(skillLevel)
                : contribution.skill.GetQualityMultiplier(skillLevel);

            // Apply weight as exponent: multiplier^weight
            result *= Mathf.Pow(multiplier, contribution.weight);
        }

        return result;
    }

    /// <summary>
    /// Weighted Average: Average the multipliers based on weights.
    /// </summary>
    private static float CalculateWeightedAverage(
        Member member,
        SkillContribution[] contributions,
        bool isSpeed)
    {
        float weightedSum = 0f;
        float totalWeight = 0f;

        foreach (var contribution in contributions)
        {
            if (contribution.skill == null) continue;
            if (isSpeed && !contribution.affectsSpeed) continue;
            if (!isSpeed && !contribution.affectsQuality) continue;

            int skillLevel = member.Skills.GetSkillLevel(contribution.skill.skillName);

            if (contribution.minimumLevel > 0 && skillLevel < contribution.minimumLevel)
                continue;

            float multiplier = isSpeed
                ? contribution.skill.GetSpeedMultiplier(skillLevel)
                : contribution.skill.GetQualityMultiplier(skillLevel);

            weightedSum += multiplier * contribution.weight;
            totalWeight += contribution.weight;
        }

        return totalWeight > 0 ? weightedSum / totalWeight : 1f;
    }

    /// <summary>
    /// Dominant Skill: Only the highest weighted skill matters.
    /// </summary>
    private static float CalculateDominant(
        Member member,
        SkillContribution[] contributions,
        bool isSpeed)
    {
        float bestMultiplier = 1f;

        foreach (var contribution in contributions)
        {
            if (contribution.skill == null) continue;
            if (isSpeed && !contribution.affectsSpeed) continue;
            if (!isSpeed && !contribution.affectsQuality) continue;

            int skillLevel = member.Skills.GetSkillLevel(contribution.skill.skillName);

            if (contribution.minimumLevel > 0 && skillLevel < contribution.minimumLevel)
                continue;

            float multiplier = isSpeed
                ? contribution.skill.GetSpeedMultiplier(skillLevel)
                : contribution.skill.GetQualityMultiplier(skillLevel);

            // Apply weight
            float weightedMultiplier = 1f + ((multiplier - 1f) * contribution.weight);

            if (weightedMultiplier > bestMultiplier)
                bestMultiplier = weightedMultiplier;
        }

        return bestMultiplier;
    }

    /// <summary>
    /// Check if member meets minimum skill requirements.
    /// </summary>
    public static bool MeetsSkillRequirements(Member member, SkillContribution[] contributions)
    {
        if (contributions == null || contributions.Length == 0)
            return true;

        foreach (var contribution in contributions)
        {
            if (contribution.skill == null) continue;
            if (contribution.minimumLevel <= 0) continue;

            int skillLevel = member.Skills.GetSkillLevel(contribution.skill.skillName);
            if (skillLevel < contribution.minimumLevel)
                return false;
        }

        return true;
    }
}