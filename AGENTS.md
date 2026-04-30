# Agent Rules – Sharing Space (Blazor + Office UI Fabric)

This project is a **Blazor Server** application (.NET 8) using **Microsoft Fluent UI
(Office UI Fabric)** and **Microsoft Graph API**. It is NOT a Next.js project.

## Key conventions

- UI framework: `Microsoft.FluentUI.AspNetCore.Components` (Fluent UI for Blazor)
- Icons: `@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons` (alias required)
- Auth: `Microsoft.Identity.Web` with `AddMicrosoftIdentityWebApp`
- Graph API: `Microsoft.Graph` SDK v5 with client-credentials flow
- Role-based auth: `[Authorize(Roles = "Lawyer")]` / `[Authorize(Roles = "Client")]`
- No Node.js, no npm, no TypeScript, no React in this project
