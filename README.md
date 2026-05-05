# Sharing Space – Secure External Document Sharing

A **B2B SaaS platform** built on top of Microsoft SharePoint and the Microsoft Graph API.
Designed to let companies, clients, and collaborators **securely share documents and workspaces
with external users** — without SharePoint complexity, guest-tenant friction, or additional
Microsoft licences for external parties.

Built with **Blazor Server** (.NET 8) and **Microsoft Fluent UI (Office UI Fabric)**.

> **"Securely share documents with clients and partners — without SharePoint complexity."**

---

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│  Blazor Server (.NET 8)  –  Office UI Fabric Fluent UI   │
│                                                          │
│  Owner Dashboard         Collaborator Dashboard          │
│  ┌─ Workspaces ──┐       ┌─ My Documents ─┐             │
│  │  WorkspaceCard│       │  Document list  │             │
│  └───────────────┘       └─────────────────┘             │
│                                                          │
│  Workspace detail: upload files, share secure links      │
└───────────────────────┬──────────────────────────────────┘
                        │  Microsoft Graph API (HTTPS)
                        ▼
┌──────────────────────────────────────────────────────────┐
│  Any Microsoft 365 Tenant  (Multi-tenant SaaS)           │
│                                                          │
│  Azure AD / Entra ID          SharePoint Online          │
│  ┌─ App Registration ─┐      ┌─ Document Libraries ─┐   │
│  │  Multi-Tenant       │      │  One library per      │   │
│  │  Client Credentials│ ───▶ │  workspace/project    │   │
│  │  Sites.ReadWrite   │      │  Metadata: owner,     │   │
│  │  Files.ReadWrite   │      │  collaborator email   │   │
│  └────────────────────┘      └───────────────────────┘   │
│  Entra B2B Guest Access                                  │
│  ┌─ External users ──────────────────────────────────┐   │
│  │  Microsoft accounts  →  sign in with credentials  │   │
│  │  Gmail / other       →  Email One-Time Passcode   │   │
│  └───────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
```

### Key Design Decisions

| Concern | Solution |
|---------|----------|
| UI framework | Blazor Server + Microsoft Fluent UI (Office UI Fabric) |
| Authentication | Microsoft Identity Web (OIDC) + Entra B2B guest invitations |
| Multi-tenancy | Single app registration; Graph clients scoped per `tid` JWT claim |
| File storage | SharePoint Document Libraries (one per workspace/project) |
| API | Microsoft Graph v1.0 via the official .NET SDK |
| Incremental sync | Graph **delta queries** – only fetches changes since last sync |
| External access | Email OTP (no Microsoft licence required) or existing Microsoft/Google account |
| Security | Least-privilege sharing links, 30-day expiry, sensitivity label ready |
| GDPR | Files never leave the customer organisation's own Microsoft 365 tenant |

---

## Project Structure

```
sharing-space/
├── SharingSpace.slnx                  # .NET solution
├── src/
│   └── SharingSpace.Client/           # Blazor Server app
│       ├── Components/
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor   # Fluent UI shell (header, nav, footer)
│       │   │   └── NavMenu.razor      # FluentNavMenu with role-based links
│       │   ├── Pages/
│       │   │   ├── Home.razor         # Landing page
│       │   │   ├── LawyerDashboard.razor  # Active Cases (Lawyer role)
│       │   │   ├── ClientDashboard.razor  # My Documents (Client role)
│       │   │   └── CaseDetail.razor   # Upload / share / delta sync
│       │   └── Shared/
│       │       ├── CaseCard.razor     # Fluent UI card for a case
│       │       ├── DocumentList.razor # Fluent UI DataGrid for files
│       │       └── FeatureTile.razor  # Home page feature tiles
│       ├── Models/
│       │   ├── Case.cs
│       │   ├── CaseDocument.cs
│       │   ├── DeltaSyncState.cs
│       │   └── UserRole.cs
│       ├── Services/
│       │   ├── GraphService.cs        # All Microsoft Graph API calls
│       │   └── GraphClientFactory.cs  # Client-credentials auth setup
│       ├── Program.cs
│       └── appsettings.json
├── docs/
│   ├── setup-checklist.md             # IT admin configuration guide
│   └── how-to-invite-a-client.md      # 3-page staff guide
└── .env.example                       # Required environment variables
```

---

## Quick Start

### Prerequisites

- .NET 8 SDK
- A Microsoft 365 tenant with SharePoint Online
- An Azure AD App Registration (see [`docs/setup-checklist.md`](docs/setup-checklist.md))

### 1. Configure

```bash
cp .env.example .env
# Fill in AZURE_AD_TENANT_ID, AZURE_AD_CLIENT_ID, AZURE_AD_CLIENT_SECRET,
# SHAREPOINT_SITE_ID, etc.
```

Or edit `src/SharingSpace.Client/appsettings.json` directly.

### 2. Run

```bash
cd src/SharingSpace.Client
dotnet run
```

Open `https://localhost:5001` and sign in with a Microsoft account that has been assigned
the `Lawyer` or `Client` role in Entra ID.

### 3. Deploy

```bash
dotnet publish src/SharingSpace.Client -c Release -o ./publish
# Deploy ./publish to Azure App Service or any HTTPS host
```

---

## Entra B2B Guest Access

External clients (including Gmail users) do not need a Microsoft 365 licence.
They receive a **One-Time Passcode** via email every time they sign in.

See [`docs/setup-checklist.md`](docs/setup-checklist.md) for the full Entra ID and
SharePoint configuration walkthrough.

See [`docs/how-to-invite-a-client.md`](docs/how-to-invite-a-client.md) for the
lawyer-facing staff guide on inviting clients and sharing documents.

---

## Microsoft Graph Features Used

| Feature | Endpoint | Usage |
|---------|----------|-------|
| List drives (cases) | `GET /sites/{id}/drives` | Lawyer dashboard |
| List drive items | `GET /drives/{id}/root/children` | Document list |
| **Delta query** | `GET /drives/{id}/root/delta` | Incremental file sync |
| Upload file | `PUT /drives/{id}/root:/{name}:/content` | Small files ≤ 4 MB |
| Upload session | `POST /drives/{id}/root:/{name}:/createUploadSession` | Large files |
| Create sharing link | `POST /drives/{id}/items/{id}/createLink` | Guest access |

---

## Licence

MIT
