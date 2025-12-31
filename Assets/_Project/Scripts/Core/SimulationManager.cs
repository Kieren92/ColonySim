using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the game simulation - updates all people, handles time, etc.
/// WHY: Central place to control simulation tick rate and manage all simulated entities.
/// PATTERN: Manager/Controller pattern.
/// </summary>
public class SimulationManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Need definitions used by all people")]
    [SerializeField] private List<NeedDefinition> standardNeeds;

    [Tooltip("Skill definitions used by all people")]
    [SerializeField] private List<SkillDefinition> standardSkills;

    [Header("Spawning")]
    [SerializeField] private GameObject memberPrefab;
    [SerializeField] private int startingMembers = 3;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;
    [SerializeField] private float spawnRadius = 5f;

    [Header("Simulation")]
    [Tooltip("Speed of simulation (1.0 = real-time, 2.0 = double speed)")]
    [SerializeField] private float simulationSpeed = 1f;

    // Track all members
    private List<Member> members = new List<Member>();
    private List<MemberView> memberViews = new List<MemberView>();

    // Singleton
    public static SimulationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Validate configuration
        if (standardNeeds == null || standardNeeds.Count == 0)
        {
            Debug.LogError("SimulationManager: No need definitions assigned!");
            return;
        }

        if (standardSkills == null || standardSkills.Count == 0)
        {
            Debug.LogError("SimulationManager: No skill definitions assigned!");
            return;
        }

        // Spawn starting members
        SpawnStartingMembers();
    }

    private void Update()
    {
        // DEBUG: Uncomment to verify simulation is running
        // Debug.Log($"Simulation tick: {members.Count} members");

        // Update all member simulations
        float deltaTime = Time.deltaTime * simulationSpeed;

        foreach (var member in members)
        {
            member.UpdateSimulation(deltaTime);
        }
    }

    /// <summary>
    /// Spawn initial commune members.
    /// </summary>
    private void SpawnStartingMembers()
    {
        for (int i = 0; i < startingMembers; i++)
        {
            SpawnMember();
        }

        Debug.Log($"Spawned {members.Count} starting members");
    }

    /// <summary>
    /// Spawn a new member at a random location.
    /// </summary>
    private Member SpawnMember()
    {
        // Generate random position
        Vector2Int gridPos = GridSystem.Instance.GetRandomWalkableCell();
        Vector3 spawnPos = GridSystem.Instance.GridToWorld(gridPos);

        // Create simulation member
        string memberName = GetRandomName();
        int age = Random.Range(18, 60);

        Member newMember = new Member();
        newMember.InitializeAsMember(memberName, age, standardNeeds, standardSkills);
        newMember.Position = spawnPos;

        // ADD THIS: Give random starting skills for testing
        GiveStartingSkills(newMember);

        members.Add(newMember);

        // Create visual representation
        GameObject memberObj = Instantiate(memberPrefab, spawnPos, Quaternion.identity);
        memberObj.name = $"Member_{memberName}";

        MemberView view = memberObj.GetComponent<MemberView>();
        if (view != null)
        {
            view.Initialize(newMember);
        }

        Debug.Log($"Spawned member: {memberName}");

        return newMember;
    }

    /// <summary>
    /// Give members random starting skills for testing.
    /// </summary>
    private void GiveStartingSkills(Member member)
    {
        // Each member gets random skills (0-5 levels)
        // This creates variety so we can see skill effects

        var skills = member.Skills.GetAllSkills();
        foreach (var skill in skills)
        {
            // Random starting level
            int startingLevel = Random.Range(0, 6); // 0-5

            // Give enough XP to reach that level
            for (int i = 0; i < startingLevel; i++)
            {
                float xpNeeded = skill.definition.GetExperienceForLevel(i + 1);
                member.Skills.AddExperience(skill.definition.skillName, xpNeeded);
            }
        }

        Debug.Log($"{member.PersonName} starting skills: " +
                  $"Farming {member.Skills.GetSkillLevel("Farming")}, " +
                  $"Strength {member.Skills.GetSkillLevel("Strength")}, " +
                  $"Construction {member.Skills.GetSkillLevel("Construction")}");
    }

    /// <summary>
    /// Find a valid spawn position within spawn radius.
    /// </summary>
    private Vector3 FindSpawnPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        return spawnCenter + new Vector3(randomCircle.x, 1f, randomCircle.y);
    }

    /// <summary>
    /// Generate a random name for a member.
    /// </summary>
    private string GenerateRandomName()
    {
        string[] firstNames = { "Alex", "Jordan", "Sam", "Taylor", "Morgan", "Casey", "Riley", "Quinn", "Avery", "Parker" };
        string[] lastNames = { "Smith", "Chen", "Garcia", "Patel", "Kim", "Martinez", "Ahmed", "Johnson", "Williams", "Brown" };

        return $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
    }

    /// <summary>
    /// Get all members.
    /// </summary>
    public List<Member> GetAllMembers() => new List<Member>(members);

    /// <summary>
    /// Get all member views.
    /// </summary>
    public List<MemberView> GetAllMemberViews() => new List<MemberView>(memberViews);

    /// <summary>
    /// Remove a member (death, exile, etc).
    /// </summary>
    public void RemoveMember(Member member, string reason)
    {
        // Find and destroy view
        MemberView view = memberViews.Find(v => v.GetMember() == member);
        if (view != null)
        {
            memberViews.Remove(view);
            Destroy(view.gameObject);
        }

        // Remove from simulation
        members.Remove(member);

        // Trigger event
        GameEvents.TriggerMemberLeft(member, reason);
    }

    /// <summary>
    /// Debug visualization.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }

    /// <summary>
    /// Generate a random name for a member.
    /// </summary>
    private string GetRandomName()
    {
        string[] firstNames = new string[]
        {
        "Alex", "Jordan", "Taylor", "Morgan", "Casey",
        "Riley", "Avery", "Quinn", "Skylar", "River",
        "Dakota", "Sage", "Rowan", "Phoenix", "Ember",
        "Ash", "Blake", "Drew", "Kai", "Reese"
        };

        string[] lastNames = new string[]
        {
        "Chen", "Kim", "Martinez", "Ahmed", "Patel",
        "Johnson", "Williams", "Brown", "Davis", "Miller",
        "Garcia", "Rodriguez", "Wilson", "Moore", "Taylor",
        "Anderson", "Thomas", "Jackson", "White", "Harris"
        };

        string firstName = firstNames[Random.Range(0, firstNames.Length)];
        string lastName = lastNames[Random.Range(0, lastNames.Length)];

        return $"{firstName} {lastName}";
    }

}