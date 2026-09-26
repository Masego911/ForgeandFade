# Forge & Fade

Forge & Fade is a barber booking website with a React/Vite frontend and an ASP.NET Core 10 API. The API uses Entity Framework Core and SQL Server. The frontend production build goes into `WebApplication7/wwwroot`, where ASP.NET Core serves it alongside the API.

> **Repository status:** This GitHub repository currently contains only this README and `.gitattributes`. The application source has not been pushed here yet. Cloning this repository alone will not build or run the website.

## Run locally on Windows

Install the .NET 10 SDK, Node.js and SQL Server LocalDB or SQL Server. Open PowerShell in the folder containing `WebApplication7.slnx`, then run these commands in order:

```powershell
Set-Location .\frontend # Enters the React project so npm reads its package files.
npm ci # Installs the dependency versions recorded in package-lock.json.
npm run build # Builds the frontend into WebApplication7/wwwroot.
Set-Location .. # Returns to the solution folder.
dotnet dev-certs https --trust # Trusts the local development HTTPS certificate.
dotnet run --project .\WebApplication7\ForgeAndFade.Api.csproj --launch-profile https # Starts the API and serves the built website.
```

Open the HTTPS address shown by the API. The development profile is configured for `https://localhost:7294`.

The API applies database migrations during startup. Its SQL Server connection must therefore work before the website can start. Local configuration uses LocalDB; set `ConnectionStrings__DefaultConnection` in the environment when using another SQL Server. Do not commit database passwords.

For frontend hot reload, leave the API running and open a second PowerShell window in the solution folder:

```powershell
Set-Location .\frontend # Enters the frontend project.
npm run dev # Starts Vite and proxies local /api requests to the HTTPS API.
```

## Azure deployment

The Azure resources created for this project are:

- App Service: `ForgeAndFadeApi20260926201256`
- Azure SQL database: `ForgeAndFade.Api_db`
- Azure SQL server: `forgeandfadeapi20260926sql.database.windows.net`

Visual Studio has an App Service publish profile and an Azure SQL service dependency. The App Service uses a system-assigned managed identity, and its database user has been created. **A working public deployment has not yet been verified.**

1. Run the frontend build above to put the latest React files into `WebApplication7/wwwroot`.
2. In Visual Studio, select the existing publish profile for `ForgeAndFade.Api`.
3. Check that the Azure SQL dependency supplies the connection setting read by `GetConnectionString("DefaultConnection")`.
4. Publish, then inspect **View → Output → Web Publish** for the result.
5. Open the App Service’s **Default domain** from the Azure portal and test the website. An address containing `.scm.` and `/api/zipdeploy` is a deployment address, not the public website.

The API calls `Database.MigrateAsync()` on startup. If the deployed site fails to start, inspect the App Service logs and database connection setting before changing or deleting database objects. Keep SQL credentials and any `Admin__CompletionKey` out of the repository.

## Features and verification

After deployment, test the homepage, services, account registration, sign-in, appointment booking, cancellation and calendar download using the public domain. The server checks booking times and overlaps. Operator completion and loyalty points require the separately configured admin key. Replace the fictional studio and contact details before using the site for a real business.
