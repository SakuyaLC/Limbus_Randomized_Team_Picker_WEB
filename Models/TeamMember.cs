using Limbus_Randomized_Team_Picker_WEB.Models;

namespace Limbus_Randomized_Team_Picker_WEB.Models;

/// <summary>
/// Represents a team member slot with an optionally selected identity.
/// </summary>
public class TeamMember
{
    /// <summary>
    /// Gets or sets the character name (e.g., "Yi Sang", "Gregor").
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected identity, or null if no identity is selected for this character.
    /// </summary>
    public Identity? Identity { get; set; } = null;
}
