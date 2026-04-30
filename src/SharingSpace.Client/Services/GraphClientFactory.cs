using Azure.Identity;
using Microsoft.Graph;
using SharingSpace.Client.Models;

namespace SharingSpace.Client.Services;

/// <summary>
/// Configures and provides the <see cref="GraphServiceClient"/> using the
/// client-credentials (app-only) flow so the Blazor Server can access
/// SharePoint on behalf of the law firm without interactive sign-in for
/// each API call.
///
/// Required app registrations:
///   - Azure AD App registration with Sites.ReadWrite.All and Files.ReadWrite.All
///     application permissions (not delegated) granted by a Global Admin.
/// </summary>
public static class GraphClientFactory
{
    public static GraphServiceClient Create(IConfiguration config)
    {
        var tenantId = config["AzureAd:TenantId"]
            ?? throw new InvalidOperationException("AzureAd:TenantId is not configured.");
        var clientId = config["AzureAd:ClientId"]
            ?? throw new InvalidOperationException("AzureAd:ClientId is not configured.");
        var clientSecret = config["AzureAd:ClientSecret"]
            ?? throw new InvalidOperationException("AzureAd:ClientSecret is not configured.");

        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        return new GraphServiceClient(credential, new[]
        {
            "https://graph.microsoft.com/.default"
        });
    }
}
