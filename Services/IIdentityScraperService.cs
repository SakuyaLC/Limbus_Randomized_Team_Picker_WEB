using Limbus_Randomized_Team_Picker_WEB.Models;

namespace Limbus_Randomized_Team_Picker_WEB.Services;

/// <summary>
/// Service responsible for scraping identity data from the Limbus Company Wiki.
/// </summary>
public interface IIdentityScraperService
{
    /// <summary>
    /// Fetches and parses the identities list page, returning all matching identities as DTOs.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A collection of extracted identity DTOs.</returns>
    Task<IList<IdentityResponseDto>> GetIdentitiesAsync(CancellationToken cancellationToken = default);
}
