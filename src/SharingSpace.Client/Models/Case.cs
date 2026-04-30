namespace SharingSpace.Client.Models;

/// <summary>
/// Represents a legal case stored as a SharePoint Document Library.
/// Metadata columns (ClientName, CaseId, Status) are mapped from
/// the SharePoint list column schema via the Microsoft Graph API.
/// </summary>
public class Case
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.Active;
    public string AssignedLawyer { get; set; } = string.Empty;
    public DateTime OpenedDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public string SharePointSiteId { get; set; } = string.Empty;
    public string DriveId { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
}

public enum CaseStatus
{
    Active,
    UnderReview,
    Closed
}
