using Limbus_Randomized_Team_Picker_WEB.Models;
using Limbus_Randomized_Team_Picker_WEB.Services;
using Microsoft.AspNetCore.Mvc;

namespace Limbus_Randomized_Team_Picker_WEB.Controllers;

/// <summary>
/// Controller providing API endpoints for Limbus Company identities.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class IdentitiesController : ControllerBase
{
    private readonly IIdentityScraperService _scraperService;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentitiesController"/>.
    /// </summary>
    /// <param name="scraperService">The identity scraper service.</param>
    public IdentitiesController(IIdentityScraperService scraperService)
    {
        _scraperService = scraperService;
    }

    /// <summary>
    /// Retrieves all scraped identities from the Limbus Company Wiki.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A JSON array of identities, or an error response if scraping fails.</returns>
    [HttpGet]
    public async Task<IActionResult> GetIdentities(CancellationToken cancellationToken = default)
    {
        try
        {
            var identities = await _scraperService.GetIdentitiesAsync(cancellationToken);
            return Ok(identities);
        }
        catch (Limbus_Randomized_Team_Picker_WEB.Services.WikiAccessDeniedException ex)
        {
            return StatusCode(403, new { error = "Access to the wiki was denied.", details = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { error = "An error occurred while communicating with the wiki.", details = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while scraping identities.", details = ex.Message });
        }
    }
}
