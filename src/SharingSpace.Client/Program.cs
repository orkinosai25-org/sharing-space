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

// ── Microsoft Graph – app-only (client-credentials) for server-side calls ───
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return SharingSpace.Client.Services.GraphClientFactory.Create(config);
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
