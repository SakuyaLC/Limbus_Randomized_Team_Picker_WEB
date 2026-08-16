using Limbus_Randomized_Team_Picker_WEB.Models;
using Limbus_Randomized_Team_Picker_WEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Limbus_Randomized_Team_Picker_WEB.Controllers;

/// <summary>
/// Controller providing team assembly API endpoints.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TeamController : ControllerBase
{
    private readonly ITeamAssemblyService _assemblyService;
    private readonly IIdentityScraperService _scraperService;
    private readonly IMemoryCache _cache;
    private const string IdentitiesCacheKey = "identities_cache";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamController"/>.
    /// </summary>
    public TeamController(ITeamAssemblyService assemblyService, IIdentityScraperService scraperService, IMemoryCache cache)
    {
        _assemblyService = assemblyService;
        _scraperService = scraperService;
        _cache = cache;
    }

    /// <summary>
    /// Assembles a team from selected identities.
    /// </summary>
    /// <param name="request">Request containing selected identity page URLs.</param>
    /// <returns>The assembled team with 12 ordered character slots.</returns>
    [HttpPost("assemble")]
    public async Task<IActionResult> AssembleTeam([FromBody] AssembleTeamRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        if (request.SelectedIdentityPageUrls == null || request.SelectedIdentityPageUrls.Count == 0)
        {
            return BadRequest(new { error = "At least one identity must be selected." });
        }

        try
        {
            // Retrieve identities from cache or scrape
            var identitiesDto = await GetIdentitiesFromCacheAsync();

            // Map DTOs to Identity model for assembly service
            var identities = identitiesDto.Select(dto => new Identity
            {
                CharacterName = dto.CharacterName,
                IdentityName = dto.IdentityName,
                ImageUrl = dto.ImageUrl,
                Rarity = dto.Rarity,
                IsSelected = request.SelectedIdentityPageUrls.Any(url => string.Equals(url, dto.SelectionKey, StringComparison.OrdinalIgnoreCase))
            }).ToList();

            // Assemble team using cryptographically secure random
            var team = await _assemblyService.AssembleAsync(identities);
            return Ok(team);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to assemble team.", details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves identities from cache, or fetches and caches them if not present.
    /// </summary>
    private async Task<IList<IdentityResponseDto>> GetIdentitiesFromCacheAsync()
    {
        return await _cache.GetOrCreateAsync(IdentitiesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
            return await _scraperService.GetIdentitiesAsync();
        }) ?? throw new InvalidOperationException("Failed to retrieve identities from cache.");
    }
}

/// <summary>
/// Request model for team assembly.
/// </summary>
public class AssembleTeamRequest
{
    /// <summary>
    /// List of selected identity page URLs.
    /// </summary>
    public List<string> SelectedIdentityPageUrls { get; set; } = new();
}
