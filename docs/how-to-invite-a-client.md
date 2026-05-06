# How to Share Documents with a Collaborator

**For workspace owners. Estimated time: 5 minutes per collaborator.**

---

## Before You Start

Make sure you have:
- [ ] The collaborator's **full name** and **email address** (any provider – Gmail, Outlook, etc.)
- [ ] The relevant **workspace** already created in SharePoint
- [ ] Logged into the portal at `https://<your-portal-domain>` with your owner account

---

## Step 1 – Open the Workspace

1. Sign in to the portal.
2. From the **Workspaces** dashboard, click the workspace card for the project you want to share.
3. You will see the Workspace Detail page showing all documents in that SharePoint library.

---

## Step 2 – Upload the Relevant Documents (if not already there)

1. Click **Upload File** (top right of the Documents panel).
2. Select the file from your computer. Files up to 250 MB are supported.
3. The file is uploaded directly into the SharePoint Document Library for this workspace.
   It is **not** stored anywhere else.

---

## Step 3 – Generate a Secure Sharing Link for the Collaborator

1. In the Documents table, find the file you want to share.
2. Click the **Share** icon (🔗) on that row.
3. A dialog will appear. Enter the collaborator's email address.
4. Click **Generate Secure Link**.

What happens next (automatically):
- A time-limited (30-day) secure link is generated via Microsoft Graph.
- The collaborator receives an email invitation from Microsoft.
- **Gmail / other non-Microsoft users** will be asked for a One-Time Passcode sent to
  their inbox – no Microsoft account required.
- **Outlook / Microsoft users** simply click the link and sign in with their existing account.

---

## Step 4 – Share the Link with the Collaborator

After the link is generated:
1. Copy the link shown in the dialog.
2. Send it to the collaborator via your normal email (or the portal will do it automatically
   depending on your organisation's configuration).
3. Advise them to check their spam folder for the Microsoft invitation email.

---

## What the Collaborator Sees

When the collaborator clicks the link:
1. They land on a Microsoft-branded sign-in page (or a one-time passcode prompt).
2. After signing in they see the **My Documents** view – only the documents you
   have shared with them, nothing else.
3. They can **download** or **open** files directly in their browser.
   They cannot upload, delete, or see other collaborators' files.

---

## Revoking Access

To remove a collaborator's access:
1. Go to **SharePoint Admin Center** → find the relevant Document Library.
2. Click **Sharing** → remove the guest user.
3. Alternatively, in Entra ID → **External Identities** → delete the guest account.

---

## FAQ

**Q: Does the collaborator need to pay for a Microsoft 365 licence?**
A: No. External guest access is included in your organisation's existing Microsoft 365 subscription.

**Q: Is the link secure if the collaborator forwards it?**
A: The link requires sign-in with the specific email address the link was sent to.
   If a sensitivity label has been applied to the file, the file itself is also encrypted.

**Q: What if the collaborator's link expires?**
A: Links expire after 30 days. Simply generate a new link using the same steps above.

**Q: Can the collaborator see all our SharePoint files?**
A: No. The portal restricts collaborators to the specific files shared with them.
   They cannot browse your organisation's SharePoint site.
