using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using Limbus_Randomized_Team_Picker_WEB.Models;

namespace Limbus_Randomized_Team_Picker_WEB.Services;

/// <summary>
/// Implementation of the identity scraper that fetches and parses the Limbus Company Wiki identities page.
/// </summary>
public class IdentityScraperService : IIdentityScraperService
{
    private readonly HttpClient _httpClient;
    private const string WikiBaseUrl = "https://limbuscompany.wiki.gg";
    private const string IdentitiesPageUrl = "https://limbuscompany.wiki.gg/wiki/List_of_Identities";

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityScraperService"/>.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
    public IdentityScraperService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("WikiHttpClient");
    }

    /// <summary>
    /// Fetches and parses the identities list page, returning all matching identities as DTOs.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A collection of extracted identity DTOs.</returns>
    /// <exception cref="WikiAccessDeniedException">Thrown when the wiki returns 403 Forbidden.</exception>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails with a non-success status code.</exception>
    public async Task<IList<IdentityResponseDto>> GetIdentitiesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(IdentitiesPageUrl, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            var reason = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new WikiAccessDeniedException(
                $"The wiki denied access (403 Forbidden). Response: {reason}");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Wiki request failed with status code {response.StatusCode}");
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        var parserOutput = document.QuerySelector(".mw-parser-output");
        var identities = new List<IdentityResponseDto>();

        if (parserOutput != null)
        {
            var currentCharacter = "Unknown";

            // Find all grid containers (divs with display: grid that contain IDRec)
            var allGrids = parserOutput.QuerySelectorAll("div[style*='display: grid']");

            // For each grid container, find its preceding sibling that contains a character header
            for (var i = 0; i < allGrids.Length; i++)
            {
                var grid = allGrids[i];
                var gridCharacterName = currentCharacter;

                // Find the preceding sibling that is a character header div
                var prevSibling = grid.PreviousElementSibling;
                while (prevSibling != null)
                {
                    var b = prevSibling.QuerySelector("b");
                    if (b != null)
                    {
                        var name = b.TextContent.Trim();
                        if (!string.IsNullOrEmpty(name))
                        {
                            gridCharacterName = name;
                            currentCharacter = name;
                            break;
                        }
                    }

                    // Check if we've reached the border container (character header is inside it)
                    if (prevSibling.GetAttribute("style")?.Contains("border:2px solid #810000") == true)
                    {
                        // The character header is the first child of this border container
                        var firstChild = prevSibling.ChildNodes.OfType<IElement>().FirstOrDefault();
                        if (firstChild != null)
                        {
                            var b2 = firstChild.QuerySelector("b");
                            if (b2 != null)
                            {
                                var name = b2.TextContent.Trim();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    gridCharacterName = name;
                                    currentCharacter = name;
                                    break;
                                }
                            }
                        }
                    }

                    prevSibling = prevSibling.PreviousElementSibling;
                }

                // Process all IDRec elements inside this grid container
                var idRecs = grid.QuerySelectorAll(".IDRec");

                foreach (var idRec in idRecs)
                {
                    var dto = ExtractIdentityDto(idRec, gridCharacterName);
                    if (dto != null)
                    {
                        identities.Add(dto);
                    }
                }
            }
        }

        return identities;
    }

    /// <summary>
    /// Extracts identity data from an IDRec element and returns a DTO.
    /// </summary>
    private static IdentityResponseDto? ExtractIdentityDto(IElement idRec, string characterName)
    {
        // Extract rarity from div.IDRar inside IDRec
        var rarDiv = idRec.QuerySelector("div.IDRar");
        int rarity = 1; // default to Rar1
        if (rarDiv != null)
        {
            var rarClass = rarDiv.ClassList?.ToArray() ?? Array.Empty<string>();
            foreach (var cls in rarClass)
            {
                if (cls == "Rar3") rarity = 3;
                else if (cls == "Rar2") rarity = 2;
            }
        }

        var images = idRec.QuerySelectorAll("img");
        IElement? matchingImage = null;
        string? identityFileName = null;

        foreach (var img in images)
        {
            var alt = img.GetAttribute("alt");

            // Use alt attribute for identity detection
            if (string.IsNullOrEmpty(alt))
                continue;

            // Extract filename from alt
            var fileName = System.IO.Path.GetFileName(alt);
            if (string.IsNullOrEmpty(fileName))
                continue;

            // Normalize: remove file extension for filtering
            var imageNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Apply filtering rules for Limbus Company identities
            // Images should contain "Uptied" or be Sinner-related
            bool matches = imageNameWithoutExtension.Contains("Uptied", StringComparison.OrdinalIgnoreCase)
                        || imageNameWithoutExtension.Contains("Sinner", StringComparison.OrdinalIgnoreCase);

            if (matches)
            {
                matchingImage = img;
                identityFileName = fileName;
                break;
            }
        }

        if (matchingImage == null || string.IsNullOrEmpty(identityFileName))
            return null;

        // Find the identity name text link
        var nameDiv = idRec.QuerySelector("div[style*=\"line-height:1.1em\"]");
        IElement? identityLink = null;
        string? identityName = null;

        if (nameDiv != null)
        {
            var link = nameDiv.QuerySelector("a[href]");
            if (link != null)
            {
                var href = link.GetAttribute("href");
                if (!string.IsNullOrEmpty(href) && href.Contains("/wiki/"))
                {
                    identityLink = link;
                    identityName = link.TextContent.Trim();
                }
            }
        }

        // Fallback: derive from image alt attribute
        if (string.IsNullOrEmpty(identityName))
        {
            var alt = matchingImage.GetAttribute("alt") ?? string.Empty;
            identityName = System.IO.Path.GetFileNameWithoutExtension(alt).Replace(" Uptied", "");
        }

        var identityPageUrl = identityLink != null ? ResolveUrl(identityLink.GetAttribute("href")!) : string.Empty;
        var thumbnailUrl = ResolveUrl(matchingImage.GetAttribute("src")!);
        var imageUrl = ConvertThumbnailToOriginalUrl(thumbnailUrl, identityFileName);

        return new IdentityResponseDto
        {
            CharacterName = characterName,
            IdentityName = identityName ?? string.Empty,
            ImageUrl = imageUrl,
            Rarity = rarity,
            SelectionKey = identityPageUrl
        };
    }

    /// <summary>
    /// Converts a wiki thumbnail URL to the original image URL.
    /// Example: /images/thumb/File.png/125px-Filename.png?hash -> /images/Filename.png
    /// </summary>
    private static string ConvertThumbnailToOriginalUrl(string thumbnailUrl, string? fileName)
    {
        try
        {
            var uri = new Uri(thumbnailUrl);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            var path = uri.AbsolutePath;

            // Thumbnail URL format: /images/thumb/Filename.png/125px-OriginalName.png?hash
            // We need to extract the original filename (after the px- part)

            // Try to extract from thumbnail path: match pattern /125px-OriginalName.png
            var match = System.Text.RegularExpressions.Regex.Match(path, @"/\d+px-([^/?]+)");
            if (match.Success)
            {
                var originalFileName = match.Groups[1].Value;
                return baseUrl + "/images/" + originalFileName;
            }

            // Fallback: try to extract the main filename (without px- prefix)
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                var lastSegment = segments.Last();
                // Remove query string if present
                lastSegment = lastSegment.Split('?')[0];

                // If it contains px-, extract after it
                if (lastSegment.Contains("px-"))
                {
                    var originalFileName = lastSegment.Split(new[] { "px-" }, StringSplitOptions.None)[1];
                    return baseUrl + "/images/" + originalFileName;
                }

                return baseUrl + "/images/" + lastSegment;
            }

            // Final fallback: use ImageFileName if available
            if (!string.IsNullOrEmpty(fileName))
            {
                return baseUrl + "/images/" + fileName;
            }

            return thumbnailUrl;
        }
        catch
        {
            // If parsing fails, return the original thumbnail URL
            return thumbnailUrl;
        }
    }

    private static string ResolveUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
            return absoluteUri.ToString();

        if (url.StartsWith("//"))
            return "https:" + url;

        if (url.StartsWith("/"))
            return WikiBaseUrl + url;

        return WikiBaseUrl + "/" + url;
    }
}

/// <summary>
/// Exception thrown when the wiki denies access to the requested page.
/// </summary>
public sealed class WikiAccessDeniedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WikiAccessDeniedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public WikiAccessDeniedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WikiAccessDeniedException"/> class with a reference to the inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="inner">The exception that is the cause of this exception.</param>
    public WikiAccessDeniedException(string message, Exception inner) : base(message, inner)
    {
    }
}
