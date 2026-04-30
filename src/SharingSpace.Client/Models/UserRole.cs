namespace SharingSpace.Client.Models;

/// <summary>
/// Represents the role a portal user has been assigned.
/// Lawyers can manage cases; Clients can only view their own documents.
/// </summary>
public enum UserRole
{
    Lawyer,
    Client
}
