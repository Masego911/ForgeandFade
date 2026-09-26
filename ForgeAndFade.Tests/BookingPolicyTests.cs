using System.Security.Claims;
using System.Text.Json;
using ForgeAndFade.Api.Controllers;
using ForgeAndFade.Api.Data;
using ForgeAndFade.Api.Enums;
using ForgeAndFade.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace ForgeAndFade.Tests;

// Uses a disposable, uniquely named SQL Server database, never the application database.
// Set FORGE_TEST_SQL to a SQL Server connection string on machines without LocalDB.
public sealed class BookingPolicyTests : IAsyncLifetime
{
    private readonly string database = "ForgeBookingTests_" + Guid.NewGuid().ToString("N");
    private DbContextOptions<ApplicationDbContext> options = null!;
    private readonly FixedClock clock = new();
    private int customer, other, barber, adult, junior;
    private static readonly DateTime Day = new(2030, 1, 7); // Monday
    public async Task InitializeAsync()
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("FORGE_TEST_SQL") ??
            @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true");
        connection.InitialCatalog = database;
        options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        var c = new Customer { Email = "self@test.invalid" };
        var o = new Customer { Email = "other@test.invalid" };
        var b = new Barber { FirstName = "Test" };
        var a = new Service { ServiceName = "Skin fade", DurationMinutes = 30 };
        var j = new Service { ServiceName = "Junior cut", DurationMinutes = 30 };
        db.AddRange(c, o, b, a, j);
        await db.SaveChangesAsync();
        (customer, other, barber, adult, junior) = (c.CustomerId, o.CustomerId, b.BarberId, a.ServiceId, j.ServiceId);
    }
    public async Task DisposeAsync()
    {
        // Only the generated test database can be deleted.
        Assert.StartsWith("ForgeBookingTests_", database);
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }
    private BookingsController Controller(ApplicationDbContext db, int? owner = null) => new(db, new ConfigurationBuilder().Build(), clock) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, (owner ?? customer).ToString()) }, "Test"))
        }}
    };
    private BookingInput Input(int hour = 14, int? service = null, bool child = false, string? notes = null) =>
        new(service ?? adult, barber, Day, TimeSpan.FromHours(hour), notes, child);
    private async Task<Booking> Existing(int hour = 12, bool child = false, int? owner = null, BookingStatus status = BookingStatus.Confirmed)
    {
        await using var db = new ApplicationDbContext(options);
        var b = new Booking { CustomerId = owner ?? customer, BarberId = barber, ServiceId = child ? junior : adult,
            BookingDate = Day, StartTime = TimeSpan.FromHours(hour), EndTime = TimeSpan.FromHours(hour).Add(TimeSpan.FromMinutes(30)),
            IsForChild = child, Status = status, CustomerNotes = "original" };
        db.Add(b); await db.SaveChangesAsync(); return b;
    }
    private static JsonElement Body(IActionResult result) => JsonSerializer.SerializeToElement(Assert.IsAssignableFrom<ObjectResult>(result).Value);
    private async Task AssertUnchanged(Booking before)
    {
        await using var db = new ApplicationDbContext(options);
        var after = await db.Bookings.SingleAsync(x => x.BookingId == before.BookingId);
        Assert.Equal(before.StartTime, after.StartTime);
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.CustomerNotes, after.CustomerNotes);
    }

    [Fact]
    public async Task RepeatSeedingAddsMissingNamesAndRetiresDuplicatesWithoutChangingBookings()
    {
        await using var db = new ApplicationDbContext(options);
        var duplicate = new Service { ServiceName = "  SKIN\t  FADE  ", Price = 999, DurationMinutes = 60 };
        var retired = new Service { ServiceName = " BEARD sculpt ", IsActive = false };
        db.AddRange(duplicate, retired);
        await db.SaveChangesAsync();
        await Existing();
        var trackedBooking = await db.Bookings.SingleAsync();
        trackedBooking.ServiceId = duplicate.ServiceId;
        await db.SaveChangesAsync();
        var beforeBooking = JsonSerializer.Serialize(db.Entry(trackedBooking).CurrentValues.ToObject());
        var beforeServices = await db.Services.CountAsync();

        // The API also guards against duplicates before catalogue maintenance runs.
        var catalogue = Body(await new ServicesController(db).All()).EnumerateArray().ToArray();
        Assert.Equal(2, catalogue.Length);
        Assert.Contains(catalogue, item => item.GetProperty("ServiceId").GetInt32() == adult);

        await DemoSeed.Apply(db);
        db.ChangeTracker.Clear();
        var first = await db.Services.OrderBy(service => service.ServiceId).ToListAsync();
        Assert.Equal(beforeServices + 8, first.Count);
        Assert.False(first.Single(service => service.ServiceId == duplicate.ServiceId).IsActive);
        Assert.True(first.Single(service => service.ServiceId == adult).IsActive);
        Assert.False(first.Single(service => service.ServiceId == retired.ServiceId).IsActive);
        Assert.Equal(999m, first.Single(service => service.ServiceId == duplicate.ServiceId).Price);
        Assert.All(first.Where(service => service.IsActive).GroupBy(service => ServiceCatalogue.NameKey(service.ServiceName)), group => Assert.Single(group));
        foreach (var expected in new[] { ("Cut and hair colouring", 750m, 120), ("Highlights", 650m, 120), ("Dread retwist and fade", 550m, 120), ("Hair twists and fade", 500m, 90) })
        {
            var service = Assert.Single(first, service => service.ServiceName == expected.Item1);
            Assert.True(service.IsActive);
            Assert.Equal(expected.Item2, service.Price);
            Assert.Equal(expected.Item3, service.DurationMinutes);
        }
        var snapshot = JsonSerializer.Serialize(first);
        await DemoSeed.Apply(db);
        db.ChangeTracker.Clear();
        Assert.Equal(snapshot, JsonSerializer.Serialize(await db.Services.OrderBy(service => service.ServiceId).ToListAsync()));
        Assert.Equal(beforeBooking, JsonSerializer.Serialize(db.Entry(await db.Bookings.SingleAsync()).CurrentValues.ToObject()));
        Assert.Equal(10, Body(await new ServicesController(db).All()).GetArrayLength());
    }

    [Fact]
    public async Task DuplicateSelfReturnsStructuredConflictAndDoesNotCancel()
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        var result = Assert.IsType<ConflictObjectResult>(await Controller(db).Create(Input()));
        Assert.Equal("existing_self_appointment", Body(result).GetProperty("code").GetString());
        Assert.Equal(original.BookingId, Body(result).GetProperty("currentAppointment").GetProperty("BookingId").GetInt32());
        await AssertUnchanged(original);
        Assert.Equal(1, await db.Bookings.CountAsync());
    }

    [Theory]
    [InlineData(true, true, null, 201)]
    [InlineData(true, false, null, 400)]
    [InlineData(false, true, "For my child", 409)]
    [InlineData(false, false, "Junior cut for child", 409)]
    public async Task ChildExceptionRequiresExplicitRecipientAndServerJuniorService(bool child, bool juniorService, string? notes, int expected)
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        var result = Assert.IsAssignableFrom<ObjectResult>(await Controller(db).Create(Input(service: juniorService ? junior : adult, child: child, notes: notes)));
        Assert.Equal(expected, result.StatusCode);
        await AssertUnchanged(original);
        if (expected == 201) Assert.True((await db.Bookings.OrderBy(x => x.BookingId).LastAsync()).IsForChild);
    }

    [Fact]
    public async Task ChildBookingDoesNotBlockFirstSelfBooking()
    {
        await Existing(child: true);
        await using var db = new ApplicationDbContext(options);
        Assert.IsType<CreatedResult>(await Controller(db).Create(Input()));
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.NoShow)]
    public async Task OnlyConfirmedBookingsBlockSelf(BookingStatus status)
    {
        await Existing(status: status);
        await using var db = new ApplicationDbContext(options);
        Assert.IsType<CreatedResult>(await Controller(db).Create(Input()));
    }

    [Fact]
    public async Task PastConfirmedBookingDoesNotBlockSelf()
    {
        var old = await Existing();
        await using var db = new ApplicationDbContext(options);
        (await db.Bookings.FindAsync(old.BookingId))!.BookingDate = Day.AddDays(-1);
        await db.SaveChangesAsync();
        Assert.IsType<CreatedResult>(await Controller(db).Create(Input()));
    }

    [Fact]
    public async Task RescheduleAndReadEnforceOwnershipAndRequireAuthentication()
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        Assert.IsType<NotFoundResult>(await Controller(db, other).Reschedule(original.BookingId, Input()));
        Assert.IsType<NotFoundResult>(await Controller(db, other).Get(original.BookingId));
        Assert.IsType<UnauthorizedResult>(await Controller(db, 0).Reschedule(original.BookingId, Input()));
        Assert.NotEmpty(typeof(BookingsController).GetMethod("Reschedule")!.GetCustomAttributes(typeof(AuthorizeAttribute), true));
        await AssertUnchanged(original);
    }

    [Theory]
    [InlineData(-1, 409)]
    [InlineData(0, 409)]
    [InlineData(1, 200)]
    public async Task ExactThreeHourBoundaryUsesSouthAfricanTime(int ticksBeforeBoundary, int expected)
    {
        var original = await Existing();
        // 12:00 SAST = 10:00 UTC; boundary is 07:00 UTC, not 09:00 UTC.
        clock.Utc = new DateTimeOffset(2030, 1, 7, 7, 0, 0, TimeSpan.Zero).AddTicks(-ticksBeforeBoundary);
        await using var db = new ApplicationDbContext(options);
        var result = Assert.IsAssignableFrom<ObjectResult>(await Controller(db).Reschedule(original.BookingId, Input()));
        Assert.Equal(expected, result.StatusCode);
        if (expected == 409) { Assert.Equal("reschedule_window_closed", Body(result).GetProperty("code").GetString()); await AssertUnchanged(original); }
        else {
            var updated = await db.Bookings.SingleAsync();
            Assert.Equal(original.BookingId, updated.BookingId);
            Assert.Equal(original.CreatedAt, updated.CreatedAt);
            Assert.Equal(TimeSpan.FromHours(14), updated.StartTime);
        }
    }

    [Fact]
    public async Task OccupiedSlotRejectsRescheduleAndRetainsOriginal()
    {
        var original = await Existing();
        await Existing(14, owner: other);
        await using var db = new ApplicationDbContext(options);
        var result = Assert.IsType<ConflictObjectResult>(await Controller(db).Reschedule(original.BookingId, Input()));
        Assert.Equal("slot_occupied", Body(result).GetProperty("code").GetString());
        await AssertUnchanged(original);
    }

    [Fact]
    public async Task ChildExceptionCannotBypassOccupiedSlot()
    {
        await Existing(14, owner: other);
        await using var db = new ApplicationDbContext(options);
        Assert.IsType<ConflictObjectResult>(await Controller(db).Create(Input(service: junior, child: true)));
    }

    [Fact]
    public async Task RescheduleCannotConvertSelfToChildOrUseInvalidWindow()
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        Assert.IsType<BadRequestObjectResult>(await Controller(db).Reschedule(original.BookingId, Input(service: junior, child: true)));
        Assert.IsType<BadRequestObjectResult>(await Controller(db).Reschedule(original.BookingId, Input(20)));
        await AssertUnchanged(original);
    }

    [Fact]
    public async Task RescheduleCanOverlapOwnOriginalSlot()
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        Assert.IsType<OkObjectResult>(await Controller(db).Reschedule(original.BookingId, Input(12) with { StartTime = new TimeSpan(12,15,0) }));
        Assert.Equal(1, await db.Bookings.CountAsync());
    }

    [Fact]
    public async Task ConcurrentSelfBookingsCommitOnlyOne()
    {
        await using var first = new ApplicationDbContext(options);
        await using var second = new ApplicationDbContext(options);
        var results = await Task.WhenAll(Controller(first).Create(Input(14)), Controller(second).Create(Input(15)));
        Assert.Single(results.OfType<CreatedResult>());
        Assert.Single(results.OfType<ConflictObjectResult>());
        Assert.Equal(1, await first.Bookings.CountAsync());
    }

    [Fact]
    public async Task ConcurrentRescheduleAndReservationCannotTakeSameSlot()
    {
        var original = await Existing();
        await using var first = new ApplicationDbContext(options);
        await using var second = new ApplicationDbContext(options);
        var results = await Task.WhenAll(Controller(first).Reschedule(original.BookingId, Input(14)), Controller(second, other).Create(Input(14)));
        Assert.Single(results.OfType<ConflictObjectResult>());
        Assert.Equal(1, await first.Bookings.CountAsync(x => x.StartTime == TimeSpan.FromHours(14)));
    }

    [Fact]
    public async Task AdditiveMigrationPreservesLegacyDataAndDefaultsRecipientToSelf()
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260924184600_InitialCreate");
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        var preserved = await db.Bookings.SingleAsync();
        Assert.False(preserved.IsForChild);
        Assert.Equal(original.BookingId, preserved.BookingId);
        await AssertUnchanged(original);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task AvailabilityExcludesOnlyOwnedRescheduleBooking()
    {
        var original = await Existing();
        await using var db = new ApplicationDbContext(options);
        var result = Assert.IsType<OkObjectResult>(await Controller(db).Availability(barber, adult, Day, original.BookingId));
        Assert.Contains("12:00", Assert.IsType<List<string>>(result.Value));
        Assert.IsType<NotFoundResult>(await Controller(db, other).Availability(barber, adult, Day, original.BookingId));
    }

    [Fact]
    public async Task RescheduleRechecksLegacyDuplicateSelfAppointments()
    {
        var original = await Existing();
        await Existing(13);
        await using var db = new ApplicationDbContext(options);
        var result = Assert.IsType<ConflictObjectResult>(await Controller(db).Reschedule(original.BookingId, Input()));
        Assert.Equal("existing_self_appointment", Body(result).GetProperty("code").GetString());
        await AssertUnchanged(original);
    }

    private sealed class FixedClock : TimeProvider
    {
        public DateTimeOffset Utc { get; set; } = new(2030, 1, 7, 6, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Utc;
    }
}
