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

    /// <summary>
    /// Gets or sets the rarity of this identity (1 = Rar1, 2 = Rar2, 3 = Rar3).
    /// </summary>
    public int Rarity { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether this identity is currently selected by the user.
    /// </summary>
    public bool IsSelected { get; set; } = false;
}
