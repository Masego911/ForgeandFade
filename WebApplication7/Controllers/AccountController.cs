using System.ComponentModel.DataAnnotations; // Provides attributes that validate incoming account fields.
using System.Security.Claims; // Provides claims for the signed-in customer's identity.
using ForgeAndFade.Api.Data; // Provides the application's database context.
using ForgeAndFade.Api.Models; // Provides the customer and loyalty account models.
using Microsoft.AspNetCore.Authentication; // Provides methods for signing customers in and out.
using Microsoft.AspNetCore.Authentication.Cookies; // Identifies the cookie authentication scheme.
using Microsoft.AspNetCore.Authorization; // Provides the attribute that protects account routes.
using Microsoft.AspNetCore.Identity; // Provides the password hashing and verification functions.
using Microsoft.AspNetCore.Mvc; // Provides API controller and HTTP response types.
using Microsoft.EntityFrameworkCore; // Provides asynchronous database queries.

namespace ForgeAndFade.Api.Controllers; // Places the controller and request records in the controllers namespace.

public record RegisterInput( // Defines the information accepted when a customer registers.
    [Required(ErrorMessage = "Enter your first name.")][StringLength(80, ErrorMessage = "First name must be 80 characters or fewer.")] string FirstName, // Requires a first name of no more than 80 characters.
    [Required(ErrorMessage = "Enter your surname.")][StringLength(80, ErrorMessage = "Surname must be 80 characters or fewer.")] string LastName, // Requires a surname of no more than 80 characters.
    [Required(ErrorMessage = "Enter your email address.")][EmailAddress(ErrorMessage = "Enter a valid email address.")] string Email, // Checks that the email address has a valid format.
    [Required(ErrorMessage = "Enter your phone number.")][RegularExpression(@"^(?:0[1-8]\d{8}|\+27[1-8]\d{8})$", ErrorMessage = "Enter a South African number such as 0821234567 or +27821234567.")] string PhoneNumber, // Accepts a ten-digit South African contact number or its +27 form.
    [Required(ErrorMessage = "Enter a password.")][StringLength(128, MinimumLength = 15, ErrorMessage = "Use a password between 15 and 128 characters.")] string Password // Requires a longer password while allowing passphrases and spaces.
); // Ends the registration request definition.

public record LoginInput( // Defines the information accepted when a customer logs in.
    [Required(ErrorMessage = "Enter your email address.")][EmailAddress(ErrorMessage = "Enter a valid email address.")] string Email, // Requires a correctly formatted email address.
    [Required(ErrorMessage = "Enter your password.")] string Password // Requires a password without applying new registration rules to existing accounts.
); // Ends the login request definition.

[ApiController, Route("api/account")] // Enables API validation and sets the account route.
public class AccountController(ApplicationDbContext db) : ControllerBase // Receives the database context through dependency injection.
{ // Begins the account controller.
    private int CustomerId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0; // Reads the customer ID from the signed-in cookie.

    [HttpPost("register")] // Makes this action available at POST /api/account/register.
    public async Task<IActionResult> Register(RegisterInput input) // Registers a validated customer.
    { // Begins registration.
        var email = input.Email.Trim().ToLowerInvariant(); // Normalises the address before checking or saving it.
        if (await db.Customers.AnyAsync(x => x.Email == email)) return Conflict(new { message = "This email address is already registered." }); // Stops an address from being registered twice.
        var customer = new Customer { FirstName = input.FirstName.Trim(), LastName = input.LastName.Trim(), Email = email, PhoneNumber = input.PhoneNumber.Trim(), CreatedAt = DateTime.UtcNow, PasswordHash = "" }; // Creates a customer from the allowed input fields.
        customer.PasswordHash = new PasswordHasher<Customer>().HashPassword(customer, input.Password); // Hashes the password instead of storing the original text.
        customer.LoyaltyAccount = new LoyaltyAccount { PointsBalance = 0, UpdatedAt = DateTime.UtcNow }; // Creates the customer's empty loyalty account.
        db.Customers.Add(customer); // Marks the customer and linked loyalty account for insertion.
        try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Conflict(new { message = "This email address is already registered." }); } // Handles a duplicate registration that occurs during the database save.
        await SignIn(customer); // Creates the customer's signed-in session.
        return Ok(new { customer.CustomerId, customer.FirstName, customer.LastName, customer.Email, customer.PhoneNumber }); // Returns account details without returning the password hash.
    } // Ends registration.

    [HttpPost("login")] // Makes this action available at POST /api/account/login.
    public async Task<IActionResult> Login(LoginInput input) // Checks an existing customer's credentials.
    { // Begins login.
        var email = input.Email.Trim().ToLowerInvariant(); // Normalises the email address for lookup.
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Email == email); // Retrieves the matching customer if one exists.
        if (customer is null || new PasswordHasher<Customer>().VerifyHashedPassword(customer, customer.PasswordHash, input.Password) == PasswordVerificationResult.Failed) return Unauthorized(new { message = "Email or password is incorrect." }); // Rejects incorrect credentials without revealing which field was wrong.
        await SignIn(customer); // Creates a signed-in session after successful verification.
        return Ok(new { customer.CustomerId, customer.FirstName, customer.LastName, customer.Email, customer.PhoneNumber }); // Returns the customer's safe account details.
    } // Ends login.

    [Authorize, HttpPost("logout")] // Requires a signed-in customer and exposes POST /api/account/logout.
    public async Task<IActionResult> Logout() // Ends the current customer's session.
    { // Begins logout.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Removes the authentication cookie.
        return NoContent(); // Confirms logout without returning a response body.
    } // Ends logout.

    [Authorize, HttpGet("me")] // Requires a signed-in customer and exposes GET /api/account/me.
    public async Task<IActionResult> Me() // Returns the current customer's account dashboard.
    { // Begins the dashboard action.
        var customer = await db.Customers.AsNoTracking().Where(x => x.CustomerId == CustomerId).Select(x => new { x.CustomerId, x.FirstName, x.LastName, x.Email, x.PhoneNumber, x.CreatedAt, x.PreferredBarberId }).FirstOrDefaultAsync(); // Retrieves only safe profile fields for this customer.
        if (customer is null) return Unauthorized(); // Rejects a session whose customer record no longer exists.
        var bookings = await db.Bookings.AsNoTracking().Where(x => x.CustomerId == CustomerId).OrderByDescending(x => x.BookingDate).ThenByDescending(x => x.StartTime).Select(x => new { x.BookingId, x.BookingDate, x.StartTime, x.EndTime, x.Status, Service = x.Service.ServiceName, x.Service.Price, Barber = x.Barber.FirstName + " " + x.Barber.LastName }).ToListAsync(); // Retrieves this customer's bookings and their display details.
        var loyalty = await db.LoyaltyAccounts.AsNoTracking().Where(x => x.CustomerId == CustomerId).Select(x => new { x.PointsBalance, Transactions = x.Transactions.OrderByDescending(t => t.CreatedAt).Select(t => new { t.Points, t.Description, t.CreatedAt }).ToList() }).FirstOrDefaultAsync(); // Retrieves the customer's points and transaction history.
        return Ok(new { customer, bookings, loyalty, completedVisits = bookings.Count(x => x.Status == ForgeAndFade.Api.Enums.BookingStatus.Completed), lifetimeSpend = bookings.Where(x => x.Status == ForgeAndFade.Api.Enums.BookingStatus.Completed).Sum(x => x.Price) }); // Returns dashboard totals calculated from completed bookings.
    } // Ends the dashboard action.

    private async Task SignIn(Customer customer) // Creates a protected session for the supplied customer.
    { // Begins session creation.
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()), new Claim(ClaimTypes.Name, customer.FirstName) }; // Stores the minimum identity information needed in the cookie.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme))); // Issues the authentication cookie.
    } // Ends session creation.
} // Ends the account controller.