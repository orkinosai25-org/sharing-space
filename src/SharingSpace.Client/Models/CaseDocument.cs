namespace SharingSpace.Client.Models;

/// <summary>
/// Represents a document stored in a SharePoint Document Library
/// and retrieved via the Microsoft Graph /drives/{id}/items endpoint.
/// </summary>
public class CaseDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string CaseName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime CreatedDateTime { get; set; }
    public DateTime LastModifiedDateTime { get; set; }
    public string WebUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ETag { get; set; } = string.Empty;

    /// <summary>Human-readable file size (e.g. "1.4 MB").</summary>
    public string DisplaySize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{SizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
