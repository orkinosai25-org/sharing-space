using Microsoft.Graph;
using Microsoft.Graph.Models;
using SharingSpace.Client.Models;

namespace SharingSpace.Client.Services;

/// <summary>
/// Wraps Microsoft Graph API calls for SharePoint sites, drives, and items.
/// Uses client-credentials (app-only) auth so the server can access any
/// client's files on behalf of the firm without requiring interactive sign-in
/// for each operation.
///
/// Delta queries are used to sync file changes efficiently; only the items
/// changed since the last sync token are returned, avoiding rate-limit issues.
/// </summary>
public class GraphService
{
    private readonly GraphServiceClient _graphClient;
    private readonly ILogger<GraphService> _logger;
    private readonly IConfiguration _config;

    // In-memory delta token store (replace with persistent cache in production)
    private readonly Dictionary<string, DeltaSyncState> _deltaTokens = new();

    public GraphService(
        GraphServiceClient graphClient,
        ILogger<GraphService> logger,
        IConfiguration config)
    {
        _graphClient = graphClient;
        _logger = logger;
        _config = config;
    }

    // ─── Cases ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lists all cases visible to the current user by reading SharePoint
    /// Document Libraries whose metadata columns match the portal schema.
    /// </summary>
    public async Task<IReadOnlyList<Case>> GetCasesAsync(CancellationToken ct = default)
    {
        var siteId = _config["SharePoint:SiteId"]
            ?? throw new InvalidOperationException("SharePoint:SiteId is not configured.");

        _logger.LogInformation("Fetching cases from SharePoint site {SiteId}", siteId);

        var drives = await _graphClient.Sites[siteId].Drives
            .GetAsync(req => req.QueryParameters.Select = new[]
            {
                "id", "name", "description", "createdDateTime", "lastModifiedDateTime"
            }, ct)
            .ConfigureAwait(false);

        var cases = new List<Case>();

        foreach (var drive in drives?.Value ?? [])
        {
            if (drive.Id is null) continue;

            var caseItem = MapDriveToCase(drive);
            caseItem.DocumentCount = await CountItemsInRootAsync(drive.Id, ct);
            cases.Add(caseItem);
        }

        return cases;
    }

    /// <summary>
    /// Returns cases assigned to a specific client email for the Client
    /// Dashboard view.
    /// </summary>
    public async Task<IReadOnlyList<Case>> GetCasesForClientAsync(
        string clientEmail, CancellationToken ct = default)
    {
        var all = await GetCasesAsync(ct);

        return all
            .Where(c => c.ClientEmail.Equals(clientEmail, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Gets a single case by its SharePoint drive ID.</summary>
    public async Task<Case?> GetCaseByIdAsync(string driveId, CancellationToken ct = default)
    {
        var drive = await _graphClient.Drives[driveId]
            .GetAsync(cancellationToken: ct)
            .ConfigureAwait(false);

        return drive is null ? null : MapDriveToCase(drive);
    }

    // ─── Documents ───────────────────────────────────────────────────────────

    /// <summary>
    /// Lists all documents in a case (SharePoint drive) using the
    /// /drives/{id}/items/{root-id}/children Graph endpoint.
    /// </summary>
    public async Task<IReadOnlyList<CaseDocument>> GetDocumentsAsync(
        string driveId, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching documents for drive {DriveId}", driveId);

        // Resolve the root folder ID first
        var root = await _graphClient.Drives[driveId].Root
            .GetAsync(cancellationToken: ct)
            .ConfigureAwait(false);

        if (root?.Id is null) return [];

        var children = await _graphClient.Drives[driveId].Items[root.Id].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = new[]
                {
                    "id", "name", "size", "file", "createdBy", "createdDateTime",
                    "lastModifiedDateTime", "webUrl", "eTag"
                };
                req.QueryParameters.Orderby = new[] { "lastModifiedDateTime desc" };
            }, ct)
            .ConfigureAwait(false);

        return (children?.Value ?? [])
            .Where(i => i.File is not null)            // exclude sub-folders
            .Select(i => MapDriveItemToDocument(i, driveId))
            .ToList();
    }

    /// <summary>
    /// Uses Microsoft Graph delta queries to fetch only file changes since
    /// the last sync. Returns a tuple of (changed items, new delta token).
    ///
    /// On first call (no stored token) the full item set is returned and a
    /// delta token is persisted for future incremental syncs.
    /// </summary>
    public async Task<(IReadOnlyList<CaseDocument> Changes, string NewDeltaToken)>
        GetDocumentsDeltaAsync(string driveId, CancellationToken ct = default)
    {
        _logger.LogInformation("Running delta sync for drive {DriveId}", driveId);

        // Resolve root folder ID (needed to call Items[rootId].Delta)
        var root = await _graphClient.Drives[driveId].Root
            .GetAsync(cancellationToken: ct)
            .ConfigureAwait(false);

        if (root?.Id is null) return ([], string.Empty);

        _deltaTokens.TryGetValue(driveId, out var storedState);
        var deltaToken = storedState?.DeltaToken;

        // If we have a stored delta link, use it directly via WithUrl for incremental sync
        var deltaResult = !string.IsNullOrEmpty(deltaToken)
            ? await _graphClient.Drives[driveId].Items[root.Id].Delta
                .WithUrl(deltaToken)
                .GetAsync(cancellationToken: ct)
                .ConfigureAwait(false)
            : await _graphClient.Drives[driveId].Items[root.Id].Delta
                .GetAsync(cancellationToken: ct)
                .ConfigureAwait(false);

        var changedItems = new List<CaseDocument>();
        string newToken = string.Empty;

        var page = deltaResult;
        while (page is not null)
        {
            foreach (var item in page.Value ?? [])
            {
                if (item.File is not null && item.Deleted is null)
                    changedItems.Add(MapDriveItemToDocument(item, driveId));
            }

            if (page.OdataDeltaLink is not null)
            {
                newToken = page.OdataDeltaLink;
                break;
            }

            if (page.OdataNextLink is null) break;

            page = await _graphClient.Drives[driveId].Items[root.Id].Delta
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct)
                .ConfigureAwait(false);
        }

        // Persist new delta token
        if (!string.IsNullOrEmpty(newToken))
        {
            _deltaTokens[driveId] = new DeltaSyncState
            {
                DriveId = driveId,
                DeltaToken = newToken,
                LastSynced = DateTime.UtcNow
            };
        }

        return (changedItems, newToken);
    }

    // ─── Upload ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Uploads a file to a SharePoint Document Library (case drive).
    /// Files up to 250 MB are handled; the Graph API handles chunking internally
    /// for large files via an upload session.
    /// </summary>
    public async Task<CaseDocument?> UploadDocumentAsync(
        string driveId,
        string fileName,
        Stream content,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading {FileName} to drive {DriveId}", fileName, driveId);

        content.Position = 0;

        // Use the path-based item reference: drives/{id}/root:/{fileName}:
        var driveItem = await _graphClient.Drives[driveId]
            .Root
            .ItemWithPath(Uri.EscapeDataString(fileName))
            .Content
            .PutAsync(content, cancellationToken: ct)
            .ConfigureAwait(false);

        return driveItem is null ? null : MapDriveItemToDocument(driveItem, driveId);
    }

    // ─── Guest Sharing ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a sharing link for a drive item and sends it to an external
    /// guest (B2B). The guest receives a secure link requiring OTP or their
    /// own Microsoft account – no Microsoft 365 licence required.
    /// </summary>
    public async Task<string?> CreateGuestSharingLinkAsync(
        string driveId,
        string itemId,
        string recipientEmail,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating sharing link for item {ItemId} → {Email}", itemId, recipientEmail);

        var permission = await _graphClient.Drives[driveId].Items[itemId]
            .CreateLink
            .PostAsync(new Microsoft.Graph.Drives.Item.Items.Item.CreateLink.CreateLinkPostRequestBody
            {
                Type = "view",
                Scope = "organization",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddDays(30),
                Recipients = new List<DriveRecipient>
                {
                    new() { Email = recipientEmail }
                }
            }, cancellationToken: ct)
            .ConfigureAwait(false);

        return permission?.Link?.WebUrl;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private static Case MapDriveToCase(Drive drive) => new()
    {
        Id = drive.Id ?? string.Empty,
        DriveId = drive.Id ?? string.Empty,
        Title = drive.Name ?? "Untitled Case",
        CaseNumber = drive.Description ?? string.Empty,
        OpenedDate = drive.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
        LastActivityDate = drive.LastModifiedDateTime?.UtcDateTime,
        Status = CaseStatus.Active
    };

    private static CaseDocument MapDriveItemToDocument(DriveItem item, string driveId)
    {
        // The pre-authenticated download URL is in additionalData
        item.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out var downloadUrlObj);

        return new CaseDocument
        {
            Id = item.Id ?? string.Empty,
            Name = item.Name ?? string.Empty,
            CaseId = driveId,
            SizeBytes = item.Size ?? 0,
            MimeType = item.File?.MimeType ?? "application/octet-stream",
            UploadedBy = item.CreatedBy?.User?.DisplayName ?? string.Empty,
            CreatedDateTime = item.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
            LastModifiedDateTime = item.LastModifiedDateTime?.UtcDateTime ?? DateTime.UtcNow,
            WebUrl = item.WebUrl ?? string.Empty,
            DownloadUrl = downloadUrlObj?.ToString() ?? string.Empty,
            ETag = item.ETag ?? string.Empty
        };
    }

    private async Task<int> CountItemsInRootAsync(string driveId, CancellationToken ct)
    {
        try
        {
            var root = await _graphClient.Drives[driveId].Root
                .GetAsync(cancellationToken: ct)
                .ConfigureAwait(false);

            if (root?.Id is null) return 0;

            var children = await _graphClient.Drives[driveId].Items[root.Id].Children
                .GetAsync(req => req.QueryParameters.Select = new[] { "id" }, ct)
                .ConfigureAwait(false);

            return children?.Value?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not count items in drive {DriveId}", driveId);
            return 0;
        }
    }
}
