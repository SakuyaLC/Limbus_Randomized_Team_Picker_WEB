using Limbus_Randomized_Team_Picker_WEB.Models;

namespace Limbus_Randomized_Team_Picker_WEB.Services;

/// <summary>
/// Service responsible for assembling a team from selected identities.
/// </summary>
public interface ITeamAssemblyService
{
    /// <summary>
    /// Assembles a team of 12 characters by randomly selecting one selected identity per character.
    /// </summary>
    /// <param name="identities">All available identities with selection state.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An AssembledTeam with exactly 12 ordered team member slots.</returns>
    Task<AssembledTeam> AssembleAsync(List<Identity> identities, CancellationToken cancellationToken = default);
}
