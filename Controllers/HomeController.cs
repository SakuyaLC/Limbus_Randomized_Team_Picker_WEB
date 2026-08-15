using Microsoft.AspNetCore.Mvc;

namespace Limbus_Randomized_Team_Picker_WEB.Controllers;

/// <summary>
/// Controller for the main application pages.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Returns the main identities catalog view.
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Error page for unexpected exceptions.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
