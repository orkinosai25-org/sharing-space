using Azure.Identity;
using Microsoft.Graph;

namespace SharingSpace.Client.Services;

/// <summary>
/// Configures and provides <see cref="GraphServiceClient"/> instances using the
/// client-credentials (app-only) flow so the Blazor Server can access SharePoint
/// on behalf of any onboarded law firm without interactive sign-in per API call.
///
/// Multi-tenant usage: call <see cref="CreateForTenant"/> with the target firm's
/// tenant ID (the <c>tid</c> claim from the signed-in user's JWT).  Each call
/// returns a client that uses <c>ClientSecretCredential</c> scoped to that tenant,
/// so a single app registration serves every onboarded firm.
///
/// Required app registration:
///   - Supported account types: "Any Microsoft Entra ID tenant (Multi-tenant)"
///   - Application permissions: Sites.ReadWrite.All, Files.ReadWrite.All,
///     User.Read.All – with admin consent granted by each firm.
/// </summary>
public static class GraphClientFactory
{
    private static readonly string[] DefaultScopes = ["https://graph.microsoft.com/.default"];

    /// <summary>
    /// Creates a <see cref="GraphServiceClient"/> scoped to <paramref name="tenantId"/>.
    /// Use this for multi-tenant scenarios where the tenant ID is known at runtime
    /// (e.g. resolved from the authenticated user's <c>tid</c> JWT claim).
    /// </summary>
    public static GraphServiceClient CreateForTenant(string tenantId, IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("tenantId must not be empty.", nameof(tenantId));

        var clientId = config["AzureAd:ClientId"]
            ?? throw new InvalidOperationException("AzureAd:ClientId is not configured.");
        var clientSecret = config["AzureAd:ClientSecret"]
            ?? throw new InvalidOperationException("AzureAd:ClientSecret is not configured.");

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return new GraphServiceClient(credential, DefaultScopes);
    }

    /// <summary>
    /// Creates a <see cref="GraphServiceClient"/> using the tenant ID from configuration.
    /// Intended for development / single-tenant environments.  In production multi-tenant
    /// deployments prefer <see cref="CreateForTenant"/>.
    /// </summary>
    public static GraphServiceClient Create(IConfiguration config)
    {
        var tenantId = config["AzureAd:TenantId"]
            ?? throw new InvalidOperationException("AzureAd:TenantId is not configured.");
        return CreateForTenant(tenantId, config);
    }
}
