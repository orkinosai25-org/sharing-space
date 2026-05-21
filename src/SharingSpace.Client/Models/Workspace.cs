namespace SharingSpace.Client.Models;

/// <summary>
/// The core business object of the Sharing Space platform.
/// Represents a shared workspace backed by a SharePoint Document Library.
///
/// Each workspace is scoped to a single Microsoft Entra ID tenant
/// (<see cref="TenantId"/>) and has exactly one assigned owner (a lawyer
/// or administrator within that tenant).  The backing SharePoint resource
/// is identified by <see cref="DriveId"/>, which is populated after
/// the workspace has been provisioned via the Microsoft Graph API.
/// </summary>
public class Workspace
{
    /// <summary>Primary identifier – the SharePoint List ID returned by Graph.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable display name of the workspace.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description shown to workspace members.</summary>
    public string Description { get; set; } = string.Empty;

    // ─── Tenant linkage ───────────────────────────────────────────────────

    /// <summary>
    /// Microsoft Entra ID tenant GUID that owns this workspace.
    /// Populated automatically from the authenticated user's <c>tid</c> claim.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    // ─── Owner assignment ─────────────────────────────────────────────────

    /// <summary>Display name of the user responsible for this workspace.</summary>
    public string OwnerDisplayName { get; set; } = string.Empty;

    /// <summary>Email address of the workspace owner (used for sharing links).</summary>
    public string OwnerEmail { get; set; } = string.Empty;

    // ─── Lifecycle ────────────────────────────────────────────────────────

    public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;

    public DateTime CreatedDate { get; set; }

    public DateTime? LastActivityDate { get; set; }

    // ─── SharePoint link ──────────────────────────────────────────────────

    /// <summary>
    /// Graph Drive ID of the backing SharePoint Document Library.
    /// This is set after the workspace is provisioned and is used for
    /// all subsequent file operations via <c>GraphService</c>.
    /// </summary>
    public string DriveId { get; set; } = string.Empty;

    /// <summary>SharePoint List ID used to retrieve the associated drive.</summary>
    public string SharePointListId { get; set; } = string.Empty;

    /// <summary>Browser-accessible URL of the SharePoint document library.</summary>
    public string WebUrl { get; set; } = string.Empty;

    public int DocumentCount { get; set; }
}

public enum WorkspaceStatus
{
    Active,
    Archived,
    Closed
}
