# Forge & Fade

[Open the live website](https://forgeandfadeapi20260926201256-a3gja7fwesarfycb.southafricanorth-01.azurewebsites.net/)

This repository includes password recovery and booking confirmation emails with calendar and map links. Service reviews, a dedicated admin login and an analytics dashboard are planned for the next phase.


A fictional premium barber studio assessment project. The existing .NET 10, Entity Framework Core, SQL Server models and initial migration from the supplied starter are preserved. A React/Vite customer application lives in `frontend/`; its production build is copied into `WebApplication7/wwwroot/` and served by ASP.NET Core.

## Run on Windows

1. Install the .NET 10 SDK, Node.js 20.19+ or 22.12+, and SQL Server LocalDB or SQL Server.
2. In PowerShell, run `cd frontend`, `npm ci`, then `npm run build`.
3. Set `ConnectionStrings__DefaultConnection` to your SQL Server connection string if you do not use LocalDB. The included `appsettings.json` uses LocalDB for local development only.
4. From the project root, run `dotnet run --project WebApplication7/ForgeAndFade.Api.csproj --launch-profile https`. Open the HTTPS address printed by ASP.NET Core. The application applies the included initial migration and seeds five barbers and seven services when the catalogue is empty.
5. For hot reload, run `npm run dev` in `frontend/` while the HTTPS API is running. The Vite development proxy expects `https://localhost:7294` as configured in `frontend/vite.config.js`.

No API password, database credential or admin key is shipped. Configure deployment settings using environment variables or your hosting service's secret manager. Use a publicly reachable SQL Server instance or managed SQL Server with an encrypted connection. Provide `ConnectionStrings__DefaultConnection` and, if operator completion is needed, `Admin__CompletionKey` as a long random secret. Serve the customer site and API from one HTTPS origin so the signed HTTP-only session cookie works. The supplied build in `wwwroot` can be regenerated with `npm ci` and `npm run build`.

## Booking and loyalty

The server calculates end time from service duration, checks business hours, limits advance booking to 90 days, and checks overlap in a serialisable SQL transaction. Customers can cancel their own future confirmed appointments. Completion is an operator action through `POST /api/bookings/{id}/complete` with the configured `X-Admin-Key` header after the visit ends. That action awards one point per full rand and writes a loyalty transaction atomically. It is disabled if `Admin__CompletionKey` is not set. Reward thresholds are displayed as illustrative; online redemption is not implemented.

Accounts use ASP.NET Core's salted password hasher and a signed, HTTP-only, secure cookie. Public catalogue API routes are read-only. Booking records and customer details are scoped to the signed-in user. Contact details and the studio are fictional and should be replaced before a real business launch.

## Checks

Run `npm run build` in `frontend/` and `dotnet build WebApplication7.slnx` at the root. On a machine with SQL Server, verify registration, duplicate email rejection, sign-in, availability, booking, conflicting booking, cancellation, calendar exports and operator completion. The build environment used to package this archive had Node.js but no .NET SDK or SQL Server, so the backend build and live database flows require verification on a .NET-capable machine before submission. The assessment PDF requires a live public URL as the sole final submission; this ZIP is the requested development deliverable, not that final submission URL.
