using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all people in the game (Members and Townspeople).
/// WHY: Shared functionality - all people have needs, skills, beliefs.
/// PATTERN: Abstract base class - concrete types inherit from this.
/// NOTE: This is pure C# - no MonoBehaviour, no Unity dependencies.
/// </summary>
public abstract class Person
{
    // ===== IDENTITY =====
    public string PersonName { get; protected set; }
    public int Age { get; protected set; }
    public string PersonID { get; protected set; } // Unique identifier

    // ===== SIMULATION COMPONENTS =====
    public PersonNeeds Needs { get; protected set; }
    public PersonSkills Skills { get; protected set; }
    public Inventory PersonalInventory { get; protected set; }

    // ===== STATE =====
    public string CurrentState { get; protected set; } = "Idle";
    public Vector3 Position { get; set; } // World position (used by presentation layer)

    // ===== CONFIGURATION =====
    protected List<NeedDefinition> needDefinitions;
    protected List<SkillDefinition> skillDefinitions;

    /// <summary>
    /// Initialize the person with configuration.
    /// MUST be called after construction.
    /// </summary>
    public virtual void Initialize(string name, int age, List<NeedDefinition> needs, List<SkillDefinition> skills)
    {
        PersonName = name;
        Age = age;
        PersonID = System.Guid.NewGuid().ToString();

        needDefinitions = needs;
        skillDefinitions = skills;

        // Initialize components
        Needs = new PersonNeeds();
        Needs.Initialize(this, needDefinitions);

        Skills = new PersonSkills();
        Skills.Initialize(this, skillDefinitions);

        PersonalInventory = new Inventory($"{name}'s Inventory", capacity: 20); // 20 slot capacity
    }

    /// <summary>
    /// Update simulation (called every frame by simulation manager).
    /// WHY abstract: Different person types have different update logic.
    /// </summary>
    public abstract void UpdateSimulation(float deltaTime);

    /// <summary>
    /// Change to a new state.
    /// </summary>
    protected void ChangeState(string newState)
    {
        string oldState = CurrentState;
        CurrentState = newState;
        GameEvents.TriggerPersonStateChanged(this, oldState, newState);
    }

    /// <summary>
    /// Get a description of this person (for debugging/UI).
    /// </summary>
    public virtual string GetDescription()
    {
        return $"{PersonName}, Age {Age}, State: {CurrentState}";
    }
}