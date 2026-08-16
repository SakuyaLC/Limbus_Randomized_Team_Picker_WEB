namespace Limbus_Randomized_Team_Picker_WEB.Models;

/// <summary>
/// Lightweight DTO for identity data returned by API endpoints.
/// Contains only the fields required by the frontend UI.
/// </summary>
public sealed class IdentityResponseDto
{
    /// <summary>
    /// The character name associated with this identity (e.g., "Yi Sang", "Gregor").
    /// </summary>
    public string CharacterName { get; init; } = string.Empty;

    /// <summary>
    /// The identity name (e.g., "Seven Assoc. South Section 6 Uptied").
    /// </summary>
    public string IdentityName { get; init; } = string.Empty;

    /// <summary>
    /// Absolute URL to the identity image (original, not thumbnail).
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// The rarity level (1 = R, 2 = SR, 3 = SSR).
    /// </summary>
    public int Rarity { get; init; }

    /// <summary>
    /// Unique identifier derived from the identity page URL, used for team selection.
    /// </summary>
    public string SelectionKey { get; init; } = string.Empty;
}
