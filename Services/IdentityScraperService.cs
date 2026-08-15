using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io;
using Limbus_Randomized_Team_Picker_WEB.Models;
using System.Linq;

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
    /// Fetches and parses the identities list page, returning all matching identities.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A collection of extracted identities.</returns>
    /// <exception cref="WikiAccessDeniedException">Thrown when the wiki returns 403 Forbidden.</exception>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request fails with a non-success status code.</exception>
    public async Task<IList<Identity>> GetIdentitiesAsync(CancellationToken cancellationToken = default)
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

        // Diagnostic tracing
        var allIdRecs = document.QuerySelectorAll(".IDRec");
        System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Total .IDRec elements: {allIdRecs.Length}");

        var allImages = document.QuerySelectorAll("img");
        System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Total img elements: {allImages.Length}");

        var imagesWithAlt = document.QuerySelectorAll("img[alt]");
        System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Total img[alt] elements: {imagesWithAlt.Length}");

        // Sequential DOM traversal: walk through all direct children of the parser-output div,
        // tracking the current character name as we encounter character header divs and grid containers.
        var parserOutput = document.QuerySelector(".mw-parser-output");
        var identities = new List<Identity>();
        var matchingCount = 0;

        System.Diagnostics.Debug.WriteLine($"[IdentityScraper] parserOutput found: {parserOutput != null}");

        if (parserOutput != null)
        {
            var currentCharacter = "Unknown";

            // Find all character headers in the document (span[id] > b)
            var allCharacterSpans = parserOutput.QuerySelectorAll("span[id]");
            var characterNames = new List<string>();
            foreach (var span in allCharacterSpans)
            {
                var b = span.QuerySelector("b");
                if (b != null)
                {
                    var name = b.TextContent.Trim();
                    if (!string.IsNullOrEmpty(name) && name.Length > 1 && name.Length < 100)
                    {
                        characterNames.Add(name);
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Found {characterNames.Count} character headers");

            // Find all grid containers (divs with display: grid that contain IDRec)
            var allGrids = parserOutput.QuerySelectorAll("div[style*='display: grid']");
            System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Found {allGrids.Length} grid containers");

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

                System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Grid {i} has character: {gridCharacterName}");

                // Process all IDRec elements inside this grid container
                var idRecs = grid.QuerySelectorAll(".IDRec");
                System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Grid {i} has {idRecs.Length} IDRec elements");

                foreach (var idRec in idRecs)
                {
                    var identity = ExtractIdentity(idRec, gridCharacterName);
                    if (identity != null)
                    {
                        identities.Add(identity);
                        matchingCount++;
                        System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Matched: {identity.IdentityName} ({identity.ImageFileName}) Character: {gridCharacterName}");
                    }
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"[IdentityScraper] Total matching identities: {matchingCount}");

        return identities.Distinct(new IdentityEqualityComparer()).ToList();
    }

    private static Identity? ExtractIdentity(IElement idRec, string characterName)
    {
        var images = idRec.QuerySelectorAll("img");
        IElement? matchingImage = null;
        string? identityFileName = null;

        foreach (var img in images)
        {
            var alt = img.GetAttribute("alt");
            var src = img.GetAttribute("src");

            // Use alt attribute for identity detection (e.g., "Seven Assoc. South Section 6 Yi Sang Uptied.png")
            if (string.IsNullOrEmpty(alt))
                continue;

            // Extract filename from alt (it should be just the filename, not a path)
            var fileName = System.IO.Path.GetFileName(alt);
            if (string.IsNullOrEmpty(fileName))
                continue;

            // Normalize: remove file extension for filtering
            var imageNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);

            // Apply filtering rules to the normalized name
            bool matches = imageNameWithoutExtension.StartsWith("LCB Sinner", StringComparison.OrdinalIgnoreCase)
                        || imageNameWithoutExtension.EndsWith("Uptied", StringComparison.OrdinalIgnoreCase);

            if (matches)
            {
                matchingImage = img;
                identityFileName = fileName;
                break;
            }
        }

        if (matchingImage == null || string.IsNullOrEmpty(identityFileName))
            return null;

        // Find the identity name text link: <div style="line-height:1.1em..."><a href="...wiki/...">IdentityName</a></div>
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

        // Fallback: if the text link wasn't found, try to derive from the image alt attribute
        if (string.IsNullOrEmpty(identityName))
        {
            var alt = matchingImage.GetAttribute("alt") ?? string.Empty;
            identityName = System.IO.Path.GetFileNameWithoutExtension(alt).Replace(" Uptied", "");
        }

        var identityPageUrl = identityLink != null ? ResolveUrl(identityLink.GetAttribute("href")!) : string.Empty;
        var imageUrl = ResolveUrl(matchingImage.GetAttribute("src")!);

        return new Identity
        {
            CharacterName = characterName,
            IdentityName = identityName ?? string.Empty,
            IdentityPageUrl = identityPageUrl,
            ImageUrl = imageUrl,
            ImageFileName = identityFileName
        };
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
/// Equality comparer for <see cref="Identity"/> objects based on identity page URL.
/// </summary>
internal sealed class IdentityEqualityComparer : IEqualityComparer<Identity>
{
    public bool Equals(Identity? x, Identity? y)
    {
        if (x is null || y is null)
            return false;

        return string.Equals(x.IdentityPageUrl, y.IdentityPageUrl, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(Identity obj)
    {
        return obj.IdentityPageUrl?.ToLowerInvariant().GetHashCode() ?? 0;
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
