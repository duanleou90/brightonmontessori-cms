# brightonmontessori-cms

Umbraco CMS running in Docker, ready for Azure Web App for Containers.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET SDK](https://dotnet.microsoft.com/download) (for generating dev certificates)

---

## Quick Start (Local Development)

### 1. Build the Docker image

```powershell
docker build -t brightonmontessori-cms:local .
```

### 2. Generate HTTPS certificate (one-time)

Umbraco 13+ requires HTTPS for the backoffice login (OpenIddict). Run this once:

```powershell
mkdir $env:USERPROFILE\.aspnet\https -Force
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p devcert
dotnet dev-certs https --trust
```

### 3. Run the container

**PowerShell:**
```powershell
docker run --rm -it -p 8443:8443 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -v $env:USERPROFILE\.aspnet\https:/https:ro `
  brightonmontessori-cms:local
```

**CMD:**
```cmd
docker run --rm -it -p 8443:8443 ^
  -e ASPNETCORE_ENVIRONMENT=Development ^
  -v %USERPROFILE%\.aspnet\https:/https:ro ^
  brightonmontessori-cms:local
```

### 4. Access the backoffice

Open **https://localhost:8443/umbraco** and complete the Umbraco setup wizard.

---

## Running with Docker Desktop UI

1. Go to **Images** → find `brightonmontessori-cms:local` → click **Run**
2. Expand **Optional settings** and configure:

| Setting | Value |
|---------|-------|
| **Container name** | `brightonmontessori-cms` |
| **Host port** | `8443` |
| **Container port** | `8443` |

**Environment variables:**

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Development` |

**Volumes:**

| Host path | Container path |
|-----------|----------------|
| `C:\Users\<your-username>\.aspnet\https` | `/https` |

3. Click **Run**, then open **https://localhost:8443/umbraco**

---

## Running with SQL Server (Docker Compose)

For a complete local environment with SQL Server:

```powershell
docker compose up --build
```

Then open **https://localhost:8443/umbraco**

Default SQL Server credentials (in `docker-compose.yml`):
- **Server:** `sql,1433`
- **User:** `sa`
- **Password:** `Your_password123`

---

## Connecting to Azure SQL + Blob Storage

To test with your Azure resources locally, add these environment variables:

```powershell
docker run --rm -it -p 8443:8443 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e "ConnectionStrings__umbracoDbDSN=Server=tcp:YOUR-SERVER.database.windows.net,1433;Database=YOUR-DB;User ID=YOUR-USER;Password=YOUR-PASSWORD;Encrypt=True;TrustServerCertificate=False" `
  -e "ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.SqlClient" `
  -e "Umbraco__Storage__AzureBlob__Media__ConnectionString=DefaultEndpointsProtocol=https;AccountName=YOUR-ACCOUNT;AccountKey=YOUR-KEY;EndpointSuffix=core.windows.net" `
  -e "Umbraco__Storage__AzureBlob__Media__ContainerName=media" `
  -v $env:USERPROFILE\.aspnet\https:/https:ro `
  brightonmontessori-cms:local
```

> **Important:** Do NOT wrap values in quotes when setting environment variables. The value should start directly with the content (e.g., `Server=tcp:...` not `"Server=tcp:..."`).

---

## Azure Web App for Containers Deployment

### Required App Settings

| Setting | Value |
|---------|-------|
| `WEBSITES_PORT` | `8080` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |
| `ConnectionStrings__umbracoDbDSN` | Your Azure SQL connection string |
| `ConnectionStrings__umbracoDbDSN_ProviderName` | `Microsoft.Data.SqlClient` |
| `Umbraco__Storage__AzureBlob__Media__ConnectionString` | Your Azure Blob connection string |
| `Umbraco__Storage__AzureBlob__Media__ContainerName` | `media` |

### Notes

- Azure handles TLS termination, so the container uses **HTTP on port 8080** in production
- The `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` setting ensures the app recognizes HTTPS from Azure's load balancer
- Azure Blob storage is **optional** for local dev but **recommended** for Azure deployment

---

## Port Reference

| Port | Protocol | Usage |
|------|----------|-------|
| `8443` | HTTPS | Local development (backoffice requires HTTPS) |
| `8080` | HTTP | Azure Web App (TLS terminated by Azure) |

---

## Troubleshooting

### "This server only accepts HTTPS requests" (ID2083)

This error occurs when accessing `/umbraco` over HTTP. Solutions:
- Use `https://localhost:8443/umbraco` (not http)
- Ensure the certificate volume is mounted correctly
- Verify `ASPNETCORE_ENVIRONMENT=Development` is set

### "No valid combination of account information found"

The Azure Blob connection string has extra characters (usually quotes). Ensure:
- No `"` at the start/end of the value
- No spaces around `=` in environment variable definitions

### Container exits immediately

Check the logs in Docker Desktop or run with `-it` flag to see console output.
