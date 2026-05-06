using Azure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.FluentUI.AspNetCore.Components;
using SharingSpace.Client.Components;
using SharingSpace.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication – Microsoft Identity Platform (Entra ID / Azure AD) ──────
// Supports both Microsoft/Outlook accounts and external guests using
// Email One-Time Passcode (OTP) via Entra B2B.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

// ── Microsoft Graph – per-tenant app-only (client-credentials) factory ───────
// Registered as a singleton Func so that GraphService (scoped, per Blazor circuit)
// can obtain a GraphServiceClient for the current user's tenant at call time.
// Each call to the factory creates a credential bound to the given tenant ID,
// allowing a single app registration to serve every onboarded law firm.
builder.Services.AddSingleton<Func<string, GraphServiceClient>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return tenantId => SharingSpace.Client.Services.GraphClientFactory.CreateForTenant(tenantId, config);
});

builder.Services.AddScoped<GraphService>();

// ── Razor / Blazor Server ────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Office UI Fabric – Fluent UI for Blazor ──────────────────────────────────
builder.Services.AddFluentUIComponents();

var app = builder.Build();

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Microsoft Identity UI routes (login / logout / challenge)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
