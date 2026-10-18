# Parts Inventory Search Connector

This project is a .NET console utility that provisions and maintains a Microsoft Graph external connection for searching appliance parts. It authenticates with Microsoft Entra ID (Azure AD) using client credentials, synchronizes parts data sourced from a local SQLite database (seeded from `ApplianceParts.csv`), and exposes that data to Microsoft 365 search by pushing items through the Microsoft Graph External Connectors API.

## Key elements

- **Interactive command-line workflow** (`Program.cs`): Presents a menu to create, select, delete, or manage an external connection, register the external item schema, and push data updates (all or incremental) to Microsoft Graph.
- **Settings management** (`Settings/PartsInventoryConnector.cs` & `appsettings.json`): Reads client ID, client secret, and tenant ID required for client credential authentication.
- **Graph integration layer** (`Graph/GraphHelper.cs`): Wraps Microsoft Graph SDK calls for connection lifecycle, schema registration (including async operation polling), and external item CRUD operations.
- **Data access layer** (`Data/ApplianceDbContext.cs` & related classes): Uses Entity Framework Core with SQLite to persist and query appliance parts, including soft deletion, last-updated tracking, and CSV bootstrap loading (`CsvDataLoader` / `AppliancePartMap`).
- **Data contracts** (`Data/AppliancePart.cs`): Maps appliance part fields to Graph external item properties with validation for required values and custom handling for string collections.
- **Change tracking helpers** (`lastuploadtime.bin`): Records the timestamp of the last successful upload so incremental syncs only send modifications since the prior run.

## Prerequisites

- .NET 8 SDK (or later) installed locally.
- A Microsoft Entra ID application with:
  - `Application` (client) ID
  - Client secret
  - `ExternalConnection.ReadWrite.All` and related permissions granted via application consent.
- The Microsoft Graph External Connectors feature enabled for your tenant with appropriate licensing.

## Configuration

1. Copy `appsettings.json` (or create a user secrets entry) and populate the `Settings` section with your tenant-specific values:
   ```json
   {
     "Settings": {
       "ClientId": "<application-id-guid>",
       "ClientSecret": "<client-secret>",
       "TenantId": "<tenant-id-guid>"
     }
   }
   ```
2. Ensure `ApplianceParts.csv` is present in the working directory. On first run, the app seeds `parts.db` with records from this file.
3. If needed, delete `parts.db` to force reseeding from the CSV.

## Running the connector

Use PowerShell in the project directory:

```powershell
# Restore dependencies and build
 dotnet restore
 dotnet build

# Run the interactive console
 dotnet run
```

When the menu appears, the options perform the following:

1. **Create a connection** – prompts for ID, name, and description; creates the Microsoft Graph external connection.
2. **Select an existing connection** – lists available connections and stores the selection for subsequent actions.
3. **Delete current connection** – removes the selected external connection from Microsoft Graph.
4. **Register schema** – posts the external item schema (fields such as `partNumber`, `name`, etc.) and waits for the asynchronous operation to complete.
5. **View schema** – retrieves and prints the current schema definition.
6. **Push updated items** – uploads only records changed since the last successful run, using `lastuploadtime.bin` to track the timestamp.
7. **Push all items** – uploads the full dataset and removes items flagged as deleted in the local database.

## Data flow highlights

1. `ApplianceDbContext` ensures the SQLite database exists, seeding from `ApplianceParts.csv` if empty.
2. When pushing data, the app queries Entity Framework for added/updated records and soft-deleted records (tracked via shadow properties) to determine uploads and deletions.
3. External items are serialized with searchable/queryable properties that align with the Graph schema, and access control lists grant tenant-wide visibility.

## Deployment and infrastructure

This sample does **not** include automated infrastructure-as-code or deployment scripts. You must:

- Manually register the Microsoft Entra ID application (or manage it via your own IaC tooling).
- Manually create the Microsoft Graph external connection through the app or via Graph API/PowerShell.

If desired, you can integrate this project into a CI/CD pipeline or author IaC artifacts (e.g., Bicep/Terraform) to provision the Entra ID application and any required secrets, but such assets are outside the scope of the repository.

## Troubleshooting tips

- Ensure the client secret remains valid; expired secrets result in authentication failures during Graph operations.
- If schema registration hangs, check the connection operation status in the Microsoft 365 admin center or via `GET /external/connections/{id}/operations`.
- Delete `lastuploadtime.bin` to force a full upload on the next run without using menu option 7.
- Remove `parts.db` when you need to reseed from the CSV or reset the local cache.
