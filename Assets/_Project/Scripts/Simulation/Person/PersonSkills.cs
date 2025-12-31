using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all skills for a person.
/// WHY: Encapsulates skill logic, tracks experience and levels.
/// </summary>
public class PersonSkills
{
    /// <summary>
    /// Represents a single skill and its current state.
    /// </summary>
    [System.Serializable]
    public class Skill
    {
        public SkillDefinition definition;
        public int currentLevel;
        public float currentExperience;
    }

    private Person person;
    private List<Skill> skills = new List<Skill>();

    /// <summary>
    /// Initialize skills from skill definitions.
    /// </summary>
    public void Initialize(Person owner, List<SkillDefinition> skillDefs)
    {
        person = owner;

        foreach (var skillDef in skillDefs)
        {
            Skill newSkill = new Skill
            {
                definition = skillDef,
                currentLevel = 0,  // Start at level 0
                currentExperience = 0f
            };

            skills.Add(newSkill);
        }
    }

    /// <summary>
    /// Add experience to a skill.
    /// WHY: Members gain experience by doing work.
    /// </summary>
    public void AddExperience(string skillName, float amount)
    {
        Skill skill = GetSkill(skillName);
        if (skill == null)
        {
            Debug.LogWarning($"Skill '{skillName}' not found on {person.PersonName}");
            return;
        }

        // Apply learning rate modifier
        float adjustedAmount = amount * skill.definition.learningRate;
        skill.currentExperience += adjustedAmount;

        // Check for level up
        float requiredXP = skill.definition.GetExperienceForLevel(skill.currentLevel + 1);

        while (skill.currentExperience >= requiredXP && skill.currentLevel < skill.definition.maxLevel)
        {
            skill.currentExperience -= requiredXP;
            skill.currentLevel++;

            Debug.Log($"{person.PersonName} leveled up {skillName} to level {skill.currentLevel}!");
            GameEvents.TriggerSkillChanged(person, skill.definition, skill.currentLevel);

            // Recalculate required XP for next level
            requiredXP = skill.definition.GetExperienceForLevel(skill.currentLevel + 1);
        }
    }

    /// <summary>
    /// Get current level of a skill.
    /// </summary>
    public int GetSkillLevel(string skillName)
    {
        Skill skill = GetSkill(skillName);
        return skill?.currentLevel ?? 0;
    }

    /// <summary>
    /// Get speed multiplier for a skill.
    /// </summary>
    public float GetSpeedMultiplier(string skillName)
    {
        Skill skill = GetSkill(skillName);
        if (skill == null) return 1f;

        return skill.definition.GetSpeedMultiplier(skill.currentLevel);
    }

    /// <summary>
    /// Get quality multiplier for a skill.
    /// </summary>
    public float GetQualityMultiplier(string skillName)
    {
        Skill skill = GetSkill(skillName);
        if (skill == null) return 1f;

        return skill.definition.GetQualityMultiplier(skill.currentLevel);
    }

    /// <summary>
    /// Get all skills (for UI display).
    /// </summary>
    public List<Skill> GetAllSkills() => new List<Skill>(skills);

    /// <summary>
    /// Find a skill by name.
    /// </summary>
    private Skill GetSkill(string skillName)
    {
        return skills.Find(s => s.definition.skillName == skillName);
    }
}