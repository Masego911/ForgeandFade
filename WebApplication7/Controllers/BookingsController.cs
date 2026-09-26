using System.Data; // Selects serialisable transaction isolation.
using System.Security.Claims; // Reads the signed customer identifier.
using ForgeAndFade.Api.Data; // Accesses the database.
using ForgeAndFade.Api.Enums; // Uses booking states.
using ForgeAndFade.Api.Models; // Uses booking and loyalty entities.
using Microsoft.AspNetCore.Authorization; // Protects private booking routes.
using Microsoft.AspNetCore.Mvc; // Defines API responses.
using Microsoft.EntityFrameworkCore; // Executes SQL Server queries.
namespace ForgeAndFade.Api.Controllers; // Groups booking routes.
public record BookingInput(int ServiceId, int BarberId, DateTime BookingDate, TimeSpan StartTime, string? CustomerNotes); // Accepts only customer-controlled booking fields.
[ApiController, Route("api/bookings")] public class BookingsController(ApplicationDbContext db, IConfiguration config) : ControllerBase // Manages availability, reservations and completion.
{
    private int CustomerId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0; // Resolves the signed-in customer.
    private static DateTime LocalNow => DateTime.UtcNow.AddHours(2); // Uses South African Standard Time, UTC+02:00 year round.
    private static (TimeSpan open, TimeSpan close) Hours(DayOfWeek day) => day switch { DayOfWeek.Sunday => (TimeSpan.Zero, TimeSpan.Zero), DayOfWeek.Saturday => (new TimeSpan(9,0,0), new TimeSpan(16,0,0)), _ => (new TimeSpan(9,0,0), new TimeSpan(18,0,0)) }; // Defines public booking hours.
    private static bool ValidWindow(DateTime date, TimeSpan start, int minutes) { var (open, close) = Hours(date.DayOfWeek); return date.Date >= LocalNow.Date && date.Date <= LocalNow.Date.AddDays(90) && start >= open && start.Add(TimeSpan.FromMinutes(minutes)) <= close && start.Minutes % 15 == 0 && start.Seconds == 0 && (date.Date > LocalNow.Date || start > LocalNow.TimeOfDay.Add(TimeSpan.FromMinutes(30))); } // Rejects closed, past, overly distant and off-grid requests.
    [HttpGet("availability")] public async Task<IActionResult> Availability([FromQuery] int barberId, [FromQuery] int serviceId, [FromQuery] DateTime date) // Lists realistic free starts for a specific barber and service.
    {
        var barber = await db.Barbers.FindAsync(barberId); var service = await db.Services.FindAsync(serviceId); // Retrieves both chosen catalogue records.
        if (barber is null || !barber.IsActive || service is null || !service.IsActive) return BadRequest(new { message = "Choose an available barber and service." }); // Rejects unavailable catalogue items.
        if (date.Date < LocalNow.Date || date.Date > LocalNow.Date.AddDays(90)) return BadRequest(new { message = "Choose a date within the next 90 days." }); // Limits the planning range.
        var (open, close) = Hours(date.DayOfWeek); // Gets the selected day's business hours.
        var reservations = await db.Bookings.AsNoTracking().Where(x => x.BarberId == barberId && x.BookingDate == date.Date && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow).Select(x => new { x.StartTime, x.EndTime }).ToListAsync(); // Finds active reservations.
        var slots = new List<string>(); // Collects available 15-minute starts.
        for (var start = open; start.Add(TimeSpan.FromMinutes(service.DurationMinutes)) <= close; start = start.Add(TimeSpan.FromMinutes(15))) if (ValidWindow(date, start, service.DurationMinutes) && !reservations.Any(x => start < x.EndTime && start.Add(TimeSpan.FromMinutes(service.DurationMinutes)) > x.StartTime)) slots.Add(start.ToString(@"hh\:mm")); // Filters overlaps by service duration.
        return Ok(slots); // Returns empty slots for closed or fully booked dates.
    } // Ends availability.
    [Authorize, HttpPost] public async Task<IActionResult> Create(BookingInput input) // Reserves a selected appointment for the signed-in customer.
    {
        var service = await db.Services.FindAsync(input.ServiceId); var barber = await db.Barbers.FindAsync(input.BarberId); // Resolves current catalogue state.
        if (service is null || !service.IsActive || barber is null || !barber.IsActive) return BadRequest(new { message = "This service or barber is unavailable." }); // Rejects stale selection.
        var date = input.BookingDate.Date; var end = input.StartTime.Add(TimeSpan.FromMinutes(service.DurationMinutes)); // Calculates the end on the server.
        if (!ValidWindow(date, input.StartTime, service.DurationMinutes)) return BadRequest(new { message = "Choose a future appointment within opening hours." }); // Enforces booking rules.
        if (input.CustomerNotes?.Length > 500) return BadRequest(new { message = "Please shorten your appointment notes." }); // Limits stored notes.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable); // Serialises competing reservations for one slot.
        try { // Keeps conflict detection and insertion in one transaction.
            if (await db.Bookings.AnyAsync(x => x.BarberId == input.BarberId && x.BookingDate == date && x.Status != BookingStatus.Cancelled && x.Status != BookingStatus.NoShow && input.StartTime < x.EndTime && end > x.StartTime)) return Conflict(new { message = "That time has just been taken. Choose another slot." }); // Prevents overlapping active bookings.
            var booking = new Booking { CustomerId=CustomerId, BarberId=input.BarberId, ServiceId=input.ServiceId, BookingDate=date, StartTime=input.StartTime, EndTime=end, Status=BookingStatus.Confirmed, CustomerNotes=input.CustomerNotes?.Trim() ?? "", CreatedAt=DateTime.UtcNow, Customer=null!, Barber=null!, Service=null! }; // Ignores client-supplied price, end and status.
            db.Bookings.Add(booking); await db.SaveChangesAsync(); await transaction.CommitAsync(); // Saves and commits the reservation.
            return Created($"/api/bookings/{booking.BookingId}", new { booking.BookingId, booking.BookingDate, booking.StartTime, booking.EndTime, booking.Status, service.ServiceName, service.Price, barber.FirstName, barber.LastName }); // Returns safe confirmation data.
        } catch (DbUpdateException) { return Conflict(new { message = "That time is no longer available. Choose another slot." }); } // Handles database write conflicts gracefully.
    } // Ends booking creation.
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
