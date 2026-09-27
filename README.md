# Forge & Fade

[Open the live website](https://forgeandfadeapi20260926201256-a3gja7fwesarfycb.southafricanorth-01.azurewebsites.net/)

Forge & Fade is a barber booking website built with React, Vite, ASP.NET Core 10, Entity Framework Core and SQL Server. The ASP.NET Core application serves the production frontend and the API from the same website.

## Customer features

- Browse services with their descriptions, prices and durations, and view barber profiles.
- View the studio story, opening hours and contact page. The address links to Google Maps directions.
- Register and log in with a secure, HTTP-only authentication cookie. Registration validates names, email address, South African phone number and password length.
- Request a password reset email. Reset links expire after 30 minutes and can be used once.
- Choose a service, barber, date and available time. The server checks opening hours, service duration and existing appointments before confirming a booking.
- Keep one future confirmed appointment for yourself at a time. A separate appointment for a child is allowed when the Junior cut service is selected.
- If an appointment for yourself already exists, use the booking flow to cancel it or reschedule it. Rescheduling is allowed only more than three hours before the existing appointment begins, using South African time. A future confirmed appointment can be cancelled before it starts.
- Receive a booking confirmation email when Azure email delivery is configured. The email includes appointment details, calendar options and directions through Google Maps and Apple Maps. A saved booking remains valid if its email fails to send.
- View bookings, completed visits, lifetime spend and loyalty points in the customer account. Points are awarded when an operator marks a finished appointment as completed. Reward redemption is not implemented.
- Use the website's help chat for supported questions about services, prices, hours, bookings and cancellations. It uses predefined answers and catalogue data.
- Use the website on mobile, tablet and desktop. The landing page includes a video where motion preferences allow it.

The catalogue seeds five barber profiles and eleven services where those records are missing. Duplicate active service names are retired without deleting existing booking references. Prices and durations for the newer colouring, highlights, dread retwist and twist services are provisional and should be reviewed before public use.

## Technology

- Frontend: React, React Router and Vite.
- Backend: ASP.NET Core 10 Web API.
- Database: SQL Server with Entity Framework Core migrations.
- Email: Azure Communication Services.
- Authentication: ASP.NET Core cookie authentication and hashed passwords.

Database migrations and catalogue seeding run when the API starts. The frontend production build is written to `WebApplication7/wwwroot`.

## Run locally on Windows

Install the .NET 10 SDK, Node.js and SQL Server or SQL Server LocalDB. Configure a working `ConnectionStrings__DefaultConnection` value for the API. Then open PowerShell in the original project folder and run:

~~~powershell
Set-Location .\frontend # Enter the React project.
npm.cmd ci # Install the dependency versions in package-lock.json.
npm.cmd run build # Build the frontend into the ASP.NET Core wwwroot folder.
Set-Location .. # Return to the solution folder.
dotnet dev-certs https --trust # Trust the local development HTTPS certificate.
dotnet run --project .\WebApplication7\ForgeAndFade.Api.csproj --launch-profile https # Start the API and serve the built website.
~~~

For frontend hot reload, run `npm.cmd run dev` from `frontend` while the HTTPS API is running. The development proxy targets `https://localhost:7294`.

## Configuration

Store connection strings and keys in environment variables, Visual Studio user secrets or Azure App Service settings. Do not commit them to GitHub.

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | SQL Server connection used by Entity Framework Core. |
| `AzureCommunicationServices__ConnectionString` | Azure Communication Services email connection. |
| `AzureCommunicationServices__SenderAddress` | Authorised sender address for account and booking emails. |
| `PasswordReset__PageUrl` | HTTPS URL of the website's reset-password page. |
| `Booking__StudioAddress` | Address used in booking emails and map links. |
| `Admin__CompletionKey` | Operator key for the existing booking-completion API action. |

The completion key protects one operator API action; the website does not yet have a dedicated admin login or admin dashboard. The contact details shown in the application should be checked before using the site for a real business.

## Verification and deployment

On 27 September 2026, `dotnet build .\WebApplication7\ForgeAndFade.Api.csproj` and `npm.cmd run build` both completed successfully in the original Visual Studio project. These build checks do not prove every live database and email flow. Password reset and booking email delivery also depend on the Azure settings above.

The live website link is at the top of this README. Pushing source code to GitHub does not, by itself, publish the new build to Azure; publish the updated ASP.NET Core project separately when you are ready to update the live website.

## Planned work

These features are planned and are not yet available on the website:

### Admin access and operations

- Add a dedicated admin login with role-based authorisation, separate from customer accounts.
- Give authorised staff a dashboard to view appointments, customers, services and barber schedules.
- Allow authorised staff to mark appointments as completed or no-show through the admin interface. The current completion API uses an operator key, but there is no admin interface yet.
- Record who made an administrative change and when it happened.

### Revenue and productivity dashboard

- Show income from completed appointments, using the price recorded for each booking so later price changes do not alter historical reports.
- Show bookings, completed visits, cancellations and no-shows over a selected date range.
- Compare revenue and completed appointments by service and by barber.
- Show barber productivity using booked time, completed appointments and available working time.
- Add date filters and clear visual summaries so staff can inspect daily, weekly and monthly performance.

### Service reviews

- Let customers review a service after a completed appointment.
- Link each review to its booking to prevent reviews from people who did not receive the service.
- Give administrators a way to moderate reviews before they appear publicly.
