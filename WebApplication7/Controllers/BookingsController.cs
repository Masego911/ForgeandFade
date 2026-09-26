using System.Data; // Selects serialisable transaction isolation.
using System.Security.Claims; // Reads the signed customer identifier.
using ForgeAndFade.Api.Data; // Accesses the database.
using ForgeAndFade.Api.Enums; // Uses booking states.
using ForgeAndFade.Api.Models; // Uses booking and loyalty entities.
using Microsoft.AspNetCore.Authorization; // Protects private booking routes.
using Microsoft.AspNetCore.Mvc; // Defines API responses.
using Microsoft.EntityFrameworkCore; // Executes SQL Server queries.
namespace ForgeAndFade.Api.Controllers; // Groups booking routes.
public record BookingInput(int ServiceId, int BarberId, DateTime BookingDate, TimeSpan StartTime, string? CustomerNotes, bool IsForChild = false); // Accepts only customer-controlled booking fields.
[ApiController, Route("api/bookings")] public class BookingsController(ApplicationDbContext db, IConfiguration config, TimeProvider clock) : ControllerBase // Manages availability, reservations and completion.
{
    private int CustomerId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0; // Resolves the signed-in customer.
    private DateTime LocalNow => clock.GetUtcNow().ToOffset(TimeSpan.FromHours(2)).DateTime; // Uses South African Standard Time, UTC+02:00 year round.
    private static (TimeSpan open, TimeSpan close) Hours(DayOfWeek day) => day switch { DayOfWeek.Sunday => (TimeSpan.Zero, TimeSpan.Zero), DayOfWeek.Saturday => (new TimeSpan(9,0,0), new TimeSpan(16,0,0)), _ => (new TimeSpan(9,0,0), new TimeSpan(18,0,0)) }; // Defines public booking hours.
    private bool ValidWindow(DateTime date, TimeSpan start, int minutes) { var (open, close) = Hours(date.DayOfWeek); return date.Date >= LocalNow.Date && date.Date <= LocalNow.Date.AddDays(90) && start >= open && start.Add(TimeSpan.FromMinutes(minutes)) <= close && start.Minutes % 15 == 0 && start.Ticks % TimeSpan.TicksPerMinute == 0 && minutes > 0 && (date.Date > LocalNow.Date || start > LocalNow.TimeOfDay.Add(TimeSpan.FromMinutes(30))); } // Rejects closed, past, overly distant and off-grid requests.
    [HttpGet("availability")] public async Task<IActionResult> Availability([FromQuery] int barberId, [FromQuery] int serviceId, [FromQuery] DateTime date, [FromQuery] int? rescheduleId = null) // Lists realistic free starts for a specific barber and service.
    {
        if (rescheduleId.HasValue && (CustomerId <= 0 || !await db.Bookings.AnyAsync(x => x.BookingId == rescheduleId && x.CustomerId == CustomerId))) return NotFound();
        var barber = await db.Barbers.FindAsync(barberId); var service = await db.Services.FindAsync(serviceId); // Retrieves both chosen catalogue records.
        if (barber is null || !barber.IsActive || service is null || !service.IsActive) return BadRequest(new { message = "Choose an available barber and service." }); // Rejects unavailable catalogue items.
        if (date.Date < LocalNow.Date || date.Date > LocalNow.Date.AddDays(90)) return BadRequest(new { message = "Choose a date within the next 90 days." }); // Limits the planning range.
        var (open, close) = Hours(date.DayOfWeek); // Gets the selected day's business hours.
        var reservations = await db.Bookings.AsNoTracking().Where(x => (!rescheduleId.HasValue || x.BookingId != rescheduleId.Value) && x.BarberId == barberId && x.BookingDate == date.Date && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow).Select(x => new { x.StartTime, x.EndTime }).ToListAsync(); // Finds active reservations.
        var slots = new List<string>(); // Collects available 15-minute starts.
        for (var start = open; start.Add(TimeSpan.FromMinutes(service.DurationMinutes)) <= close; start = start.Add(TimeSpan.FromMinutes(15))) if (ValidWindow(date, start, service.DurationMinutes) && !reservations.Any(x => start < x.EndTime && start.Add(TimeSpan.FromMinutes(service.DurationMinutes)) > x.StartTime)) slots.Add(start.ToString(@"hh\:mm")); // Filters overlaps by service duration.
        return Ok(slots); // Returns empty slots for closed or fully booked dates.
    } // Ends availability.
    private object Appointment(Booking b) => new {
        b.BookingId, b.ServiceId, b.BarberId, b.BookingDate, b.StartTime, b.EndTime,
        b.Status, b.IsForChild, b.CustomerNotes, b.Service.ServiceName, b.Service.Price,
        b.Barber.FirstName, b.Barber.LastName,
        canReschedule = b.Status == BookingStatus.Confirmed && b.BookingDate.Date.Add(b.StartTime) > LocalNow.AddHours(3)
    };

    [Authorize, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var booking = await db.Bookings.AsNoTracking().Include(x => x.Service).Include(x => x.Barber)
            .FirstOrDefaultAsync(x => x.BookingId == id && x.CustomerId == CustomerId);
        return booking is null ? NotFound() : Ok(Appointment(booking));
    }

    [Authorize, HttpPost]
    public Task<IActionResult> Create(BookingInput input) => SaveAppointment(input, null);

    [Authorize, HttpPut("{id:int}/reschedule")]
    public Task<IActionResult> Reschedule(int id, BookingInput input) => SaveAppointment(input, id);

    private async Task<IActionResult> SaveAppointment(BookingInput input, int? id)
    {
        if (CustomerId <= 0) return Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        // A transaction-owned database lock serializes policy checks across API instances.
        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'ForgeAndFade.BookingPolicy', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 51000, 'Booking lock unavailable. Please retry.', 1;");
        var now = LocalNow;
        Booking? booking = null;
        if (id.HasValue)
        {
            booking = await db.Bookings.FirstOrDefaultAsync(x => x.BookingId == id && x.CustomerId == CustomerId);
            if (booking is null) return NotFound();
            if (booking.Status != BookingStatus.Confirmed || booking.BookingDate.Date.Add(booking.StartTime) <= now.AddHours(3))
                return Conflict(new { code = "reschedule_window_closed", message = "Rescheduling is allowed only more than three hours before your existing appointment starts (South African time)." });
            if (booking.IsForChild != input.IsForChild)
                return BadRequest(new { message = "Rescheduling must keep the same appointment recipient." });
        }
        var service = await db.Services.FindAsync(input.ServiceId);
        var barber = await db.Barbers.FindAsync(input.BarberId);
        if (service is null || !service.IsActive || barber is null || !barber.IsActive)
            return BadRequest(new { message = "This service or barber is unavailable." });
        if (input.IsForChild && !string.Equals(service.ServiceName.Trim(), "Junior cut", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { code = "invalid_child_service", message = "An appointment for a child must use the Junior cut service." });
        var date = input.BookingDate.Date;
        var end = input.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes));
        if (!ValidWindow(date, input.StartTime, service.DurationMinutes))
            return BadRequest(new { message = "Choose a future appointment within opening hours." });
        if (input.CustomerNotes?.Length > 500)
            return BadRequest(new { message = "Please shorten your appointment notes." });
        if (!input.IsForChild)
        {
            var current = await db.Bookings.Include(x => x.Service).Include(x => x.Barber)
                .Where(x => x.CustomerId == CustomerId && !x.IsForChild && x.Status == BookingStatus.Confirmed
                    && (!id.HasValue || x.BookingId != id.Value)
                    && (x.BookingDate > now.Date || (x.BookingDate == now.Date && x.StartTime > now.TimeOfDay)))
                .OrderBy(x => x.BookingDate).ThenBy(x => x.StartTime).FirstOrDefaultAsync();
            if (current is not null)
                return Conflict(new { code = "existing_self_appointment", message = "You already have a future confirmed appointment for yourself.", currentAppointment = Appointment(current) });
        }
        if (await db.Bookings.AnyAsync(x => (!id.HasValue || x.BookingId != id.Value)
            && x.BarberId == input.BarberId && x.BookingDate == date
            && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow
            && input.StartTime < x.EndTime && end > x.StartTime))
            return Conflict(new { code = "slot_occupied", message = "That time has just been taken. Choose another slot." });
        if (booking is null)
        {
            booking = new Booking { CustomerId = CustomerId, Status = BookingStatus.Confirmed, CreatedAt = clock.GetUtcNow().UtcDateTime };
            db.Bookings.Add(booking);
        }
        booking.ServiceId = input.ServiceId;
        booking.BarberId = input.BarberId;
        booking.Service = service;
        booking.Barber = barber;
        booking.BookingDate = date;
        booking.StartTime = input.StartTime;
        booking.EndTime = end;
        booking.IsForChild = input.IsForChild;
        booking.CustomerNotes = input.CustomerNotes?.Trim() ?? "";
        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return id.HasValue ? Ok(Appointment(booking)) : Created($"/api/bookings/{booking.BookingId}", Appointment(booking));
        }
        catch (DbUpdateException)
        {
            return Conflict(new { code = "booking_conflict", message = "The appointment could not be saved. Please refresh availability and retry." });
        }
    }
    [Authorize, HttpDelete("{id:int}")] public async Task<IActionResult> Cancel(int id) // Cancels only the current customer's future appointment.
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(x => x.BookingId == id && x.CustomerId == CustomerId); // Checks ownership.
        if (booking is null) return NotFound(); // Hides other customers' bookings.
        if (booking.Status != BookingStatus.Confirmed || booking.BookingDate.Date.Add(booking.StartTime) <= LocalNow) return Conflict(new { message = "This appointment can no longer be cancelled online." }); // Restricts cancellation to future confirmed bookings.
        booking.Status = BookingStatus.Cancelled; await db.SaveChangesAsync(); return NoContent(); // Releases the slot while retaining history.
    } // Ends cancellation.
    [HttpPost("{id:int}/complete")] public async Task<IActionResult> Complete(int id, [FromHeader(Name="X-Admin-Key")] string? suppliedKey) // Supports a minimal protected completion action.
    {
        var expected = config["Admin:CompletionKey"]; // Reads the configured operator secret.
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(suppliedKey) || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expected)), System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(suppliedKey)))) return Unauthorized(); // Disables the endpoint unless a matching secret is configured.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); // Makes completion and points atomic.
        var booking = await db.Bookings.Include(x => x.Service).FirstOrDefaultAsync(x => x.BookingId == id); // Retrieves the completed service price.
        if (booking is null) return NotFound(); // Reports missing appointments.
        if (booking.Status != BookingStatus.Confirmed || booking.BookingDate.Date.Add(booking.EndTime) > LocalNow) return Conflict(new { message = "Only finished, confirmed appointments can be completed." }); // Blocks premature or repeated awards.
        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(x => x.CustomerId == booking.CustomerId); // Finds the customer's points account.
        if (account is null) return Conflict(new { message = "The customer has no loyalty account." }); // Prevents untracked points.
        var points = (int)decimal.Floor(booking.Service.Price); // Awards one point per full rand.
        booking.Status = BookingStatus.Completed; account.PointsBalance += points; account.UpdatedAt = DateTime.UtcNow; // Updates both business records.
        db.LoyaltyTransactions.Add(new LoyaltyTransaction { LoyaltyAccountId=account.LoyaltyAccountId, BookingId=id, Points=points, TransactionType="Earned", Description="Completed appointment", CreatedAt=DateTime.UtcNow, LoyaltyAccount=null! }); // Records the audit trail.
        await db.SaveChangesAsync(); await transaction.CommitAsync(); return Ok(new { pointsAwarded=points, account.PointsBalance }); // Commits points exactly once.
    } // Ends completion.
} // Ends booking routes.
