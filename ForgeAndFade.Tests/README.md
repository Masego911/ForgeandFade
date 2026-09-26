# Booking policy tests

Run from the workspace root:

    dotnet build WebApplication7.slnx -c Release
    dotnet test ForgeAndFade.Tests/ForgeAndFade.Tests.csproj -c Release

Tests use SQL Server LocalDB by default and create/drop only uniquely named
ForgeBookingTests_<guid> databases. To use another test SQL Server, set
FORGE_TEST_SQL to a connection string with permission to create test databases.
The configured application database is never used.

Coverage includes structured duplicate responses, explicit Junior cut child
exceptions, unchanged bookings after rejection, ownership and authorization,
overlapping slots, concurrent reservations/rescheduling, the exact SAST
three-hour boundary, and additive migration preservation.
