namespace SharingSpace.Client.Models;

/// <summary>
/// Persists the Microsoft Graph delta-query token for a SharePoint drive.
/// Used by <see cref="Services.GraphService"/> to efficiently poll for
/// file changes without re-fetching the entire library on each sync.
/// </summary>
public class DeltaSyncState
{
    public string DriveId { get; set; } = string.Empty;
    public string DeltaToken { get; set; } = string.Empty;
    public DateTime LastSynced { get; set; }
}
