using Limbus_Randomized_Team_Picker_WEB.Models;
using Limbus_Randomized_Team_Picker_WEB.Services;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="TeamController"/>.
    /// </summary>
    public TeamController(ITeamAssemblyService assemblyService, IIdentityScraperService scraperService)
    {
        _assemblyService = assemblyService;
        _scraperService = scraperService;
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

        try
        {
            var identities = await _scraperService.GetIdentitiesAsync(CancellationToken.None);

            // Mark identities based on selected URLs
            var identitiesList = identities as List<Identity> ?? identities.ToList();
            var selectedUrls = new HashSet<string>(request.SelectedIdentityPageUrls, StringComparer.OrdinalIgnoreCase);
            foreach (var identity in identitiesList)
            {
                identity.IsSelected = selectedUrls.Contains(identity.IdentityPageUrl);
            }

            // Assemble team using cryptographically secure random
            var team = _assemblyService.Assemble(identitiesList);
            return Ok(team);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to assemble team.", details = ex.Message });
        }
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
