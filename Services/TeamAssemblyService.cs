using System.Security.Cryptography;
using Limbus_Randomized_Team_Picker_WEB.Models;

namespace Limbus_Randomized_Team_Picker_WEB.Services;

/// <summary>
/// Service that assembles a team using cryptographically secure random selection.
/// </summary>
public class TeamAssemblyService : ITeamAssemblyService
{
    /// <summary>
    /// Fixed order of 12 characters for team assembly.
    /// This order is ALWAYS maintained regardless of sorting or filtering.
    /// </summary>
    private static readonly string[] TeamOrder =
    [
        "Yi Sang",
        "Faust",
        "Sinclair",
        "Ryōshū",
        "Meursault",
        "Hong Lu",
        "Heathcliff",
        "Ishmael",
        "Rodion",
        "Don Quixote",
        "Outis",
        "Gregor"
    ];

    /// <summary>
    /// Assembles a team of 12 characters by randomly selecting one selected identity per character.
    /// Uses cryptographically secure random number generation.
    /// </summary>
    public AssembledTeam Assemble(List<Identity> identities)
    {
        var team = new AssembledTeam();

        // Group selected identities by character name
        var selectedByIdentity = identities
            .Where(i => i.IsSelected)
            .GroupBy(i => i.CharacterName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Create exactly 12 team members in fixed order
        foreach (var characterName in TeamOrder)
        {
            var member = new TeamMember
            {
                CharacterName = characterName
            };

            if (selectedByIdentity.TryGetValue(characterName, out var characterIdentities))
            {
                // Randomly select one identity using cryptographically secure random
                var selectedIndex = RandomNumberGenerator.GetInt32(0, characterIdentities.Count);
                member.Identity = characterIdentities[selectedIndex];
            }

            team.Members.Add(member);
        }

        return team;
    }
}
