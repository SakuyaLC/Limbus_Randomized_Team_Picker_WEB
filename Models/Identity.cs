namespace Limbus_Randomized_Team_Picker_WEB.Models;

/// <summary>
/// Represents a scraped identity from the Limbus Company Wiki.
/// </summary>
public class Identity
{
    /// <summary>
    /// Gets or sets the character name associated with this identity.
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identity name.
    /// </summary>
    public string IdentityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute URL to the identity wiki page.
    /// </summary>
    public string IdentityPageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the absolute URL to the identity image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name of the identity image.
    /// </summary>
    public string ImageFileName { get; set; } = string.Empty;
}
