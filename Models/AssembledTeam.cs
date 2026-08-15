namespace Limbus_Randomized_Team_Picker_WEB.Models;

/// <summary>
/// Represents the assembled team with exactly 12 ordered character slots.
/// </summary>
public class AssembledTeam
{
    /// <summary>
    /// Gets the ordered list of 12 team members.
    /// Always contains exactly 12 TeamMember objects in the fixed character order.
    /// </summary>
    public List<TeamMember> Members { get; set; } = new();
}
