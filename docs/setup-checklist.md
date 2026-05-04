# Setup Checklist – Secure Legal Client Portal

This checklist covers everything an IT administrator or Solution Architect needs to
configure before going live with the Sharing Space portal.

---

## 1 · Azure AD / Entra ID – Multi-Tenant App Registration

> **Why multi-tenant?** A single app registration in your publishing tenant lets any
> law firm grant access with one click, without you needing a separate registration
> per customer.

1. Go to **https://entra.microsoft.com** → **App registrations** → **New registration**.
2. Name: `Sharing Space Legal Portal`
3. Supported account types: **"Accounts in any organisational directory (Any Microsoft Entra ID tenant – Multitenant)"** ← critical for SaaS.
4. Redirect URIs (type: **Web**):
   - `https://<your-domain>/signin-oidc` (authentication callback)
   - `https://<your-domain>/onboard` (admin consent callback)
5. After creation, note the **Application (client) ID** and **Directory (tenant) ID**.
6. Go to **Certificates & secrets** → **New client secret** → copy the secret value immediately.
7. Go to **API permissions** → **Add a permission** → **Microsoft Graph** → **Application permissions**:
   - `Sites.ReadWrite.All`
   - `Files.ReadWrite.All`
   - `User.Read.All` (to resolve guest user display names)
8. Click **Grant admin consent** for all permissions (for your own publishing tenant).

> **For each customer tenant**: the law firm's Global Administrator must visit
> `https://<your-domain>/onboard` and click **"Grant Access to Your Firm's Microsoft 365"**.
> This triggers the Microsoft admin consent flow and grants your app access to their tenant.

---

## 2 · Portal Configuration

Set the following values in `appsettings.json` (or via environment variables):

```json
{
  "AzureAd": {
    "TenantId": "organizations",
    "ClientId": "<your-app-client-id>",
    "ClientSecret": "<your-client-secret>",
    "Domain": "<your-publishing-tenant>.onmicrosoft.com"
  },
  "SharePoint": {
    "SiteId": "<default-site-id-for-dev>",
    "SiteUrl": "https://<tenant>.sharepoint.com/sites/LegalPortal",
    "TenantName": "<tenant>"
  },
  "Portal": {
    "FirmName": "Your Firm LLP"
  }
}
```

> Setting `TenantId` to `"organizations"` allows any Microsoft Entra ID tenant to
> authenticate. Individual firm tenant IDs are resolved at runtime from the signed-in
> user's `tid` JWT claim and used to scope Graph API calls to that firm's data only.

---

## 3 · Entra B2B External Sharing (Guest Invitations)

1. Go to **Entra ID** → **External Identities** → **External collaboration settings**.
2. Set **Guest user access** to _"Guest users have the same access as members"_.
3. Set **Guest invite settings** to _"Anyone in the organisation can invite guest users"_.
4. Enable **Email one-time passcode** for guests who do not have a Microsoft account
   (`External Identities` → `All identity providers` → toggle OTP on).

---

## 4 · SharePoint Tenant-Level External Sharing

1. Go to **SharePoint Admin Center** (`https://<tenant>-admin.sharepoint.com`).
2. **Policies** → **Sharing**.
3. Set SharePoint and OneDrive sharing to **"New and existing guests"**.
4. Under **Advanced settings** enable:
   - ✅ Allow guests to share items they don't own
   - ✅ Guests must sign in using the same account to which sharing invitations are sent

---

## 5 · SharePoint Site – Master Client Portal

1. Create a new **Communication Site** named `LegalPortal`.
2. Note the site URL (e.g. `https://yourtenant.sharepoint.com/sites/LegalPortal`).
3. Get the site ID via Graph Explorer:
   ```
   GET https://graph.microsoft.com/v1.0/sites/yourtenant.sharepoint.com:/sites/LegalPortal
   ```
   Copy the `id` field value → this is your `SharePoint:SiteId`.
4. For each client matter, create a **Document Library** (one per case):
   - Add a column `CaseNumber` (single-line text)
   - Add a column `ClientEmail` (single-line text)
   - Add a column `Status` (choice: Active | UnderReview | Closed)

---

## 6 · Portal Deployment

1. Clone the repository and fill in `appsettings.json` (or set environment variables via `.env`):
   ```json
   {
     "AzureAd": {
       "TenantId": "organizations",
       "ClientId": "...",
       "ClientSecret": "..."
     },
     "SharePoint": {
       "SiteId": "...",
       "SiteUrl": "https://yourtenant.sharepoint.com/sites/LegalPortal"
     },
     "Portal": {
       "FirmName": "Your Firm LLP"
     }
   }
   ```
2. Build and publish:
   ```bash
   dotnet publish src/SharingSpace.Client -c Release -o ./publish
   ```
3. Host on **Azure App Service** (or any HTTPS host).
4. Set a **custom domain** (e.g. `portal.lawfirm.com`) pointing to the App Service.
5. Add the custom domain's `/onboard` and `/signin-oidc` paths to the Redirect URIs in the App Registration.

---

## 7 · Security Hardening (Least-Privilege)

- Each case Document Library should have its own **site permissions group** rather than
  inheriting from the root site – this prevents a client seeing another client's files.
- Apply **Sensitivity Labels** (Microsoft Purview) to the case libraries:
  - `Confidential – Client Matter` label with encryption so files are protected even if
    a link is inadvertently forwarded.
- Enable **Conditional Access** policy: require MFA for all lawyer accounts.
- Set **link expiry** to 30 days (configured in the portal's `GraphService.CreateGuestSharingLinkAsync`).

---

## 8 · Post-Go-Live Verification

- [ ] Law firm admin visits `/onboard` and clicks "Grant Access" → lands back on `/onboard?admin_consent=True`
- [ ] Sign in as a lawyer from the onboarded tenant → confirm Active Cases loads
- [ ] Upload a test document → confirm it appears in SharePoint
- [ ] Generate a guest sharing link → confirm OTP email arrives at the external address
- [ ] Sign in as a guest client → confirm only their documents are visible
- [ ] Run a delta sync → confirm only changed files are returned
