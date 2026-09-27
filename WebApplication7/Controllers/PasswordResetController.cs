using System.ComponentModel.DataAnnotations; // Provides validation rules for incoming requests.
using System.Security.Cryptography; // Generates unpredictable tokens and hashes them.
using System.Text; // Converts token text into bytes for hashing.
using ForgeAndFade.Api.Data; // Provides the database context.
using ForgeAndFade.Api.Models; // Provides the reset token and customer models.
using ForgeAndFade.Api.Services; // Provides the email sender interface.
using Microsoft.AspNetCore.Identity; // Hashes the customer's new password.
using Microsoft.AspNetCore.Mvc; // Provides API routes and HTTP responses.
using Microsoft.AspNetCore.WebUtilities; // Encodes random bytes safely for a URL.
using Microsoft.EntityFrameworkCore; // Provides asynchronous database operations.

namespace ForgeAndFade.Api.Controllers; // Places these actions alongside the existing account controller.

public record ForgotPasswordInput( // Defines the request for a reset email.
    [Required][EmailAddress] string Email // Requires an email address with a valid format.
); // Ends the request definition.

public record ResetPasswordInput( // Defines the request to set a new password.
    [Required] string Token, // Requires the token from the email link.
    [Required][StringLength(128, MinimumLength = 15)] string NewPassword // Applies the current registration password length rule.
); // Ends the request definition.

[ApiController] // Enables automatic validation of request fields.
[Route("api/account")] // Groups these actions under the existing account API route.
public sealed class PasswordResetController(ApplicationDbContext db, IPasswordResetEmailSender emailSender, IConfiguration configuration, ILogger<PasswordResetController> logger) : ControllerBase // Receives its dependencies from Program.cs.
{ // Starts the controller.
    private const string RequestMessage = "If that email address has an account, a password reset link will be sent."; // Uses the same response for known and unknown addresses.

    [HttpPost("forgot-password")] // Exposes POST /api/account/forgot-password.
    public async Task<IActionResult> ForgotPassword(ForgotPasswordInput input, CancellationToken cancellationToken) // Handles a reset-link request.
    { // Starts the request action.
        var pageUrl = configuration["PasswordReset:PageUrl"]; // Reads the configured address of the future reset page.
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri) || pageUri.Scheme != Uri.UriSchemeHttps) return StatusCode(503, new { message = "Password reset is not configured yet." }); // Prevents unsafe or missing reset links.
        if (string.IsNullOrWhiteSpace(configuration["AzureCommunicationServices:ConnectionString"]) || string.IsNullOrWhiteSpace(configuration["AzureCommunicationServices:SenderAddress"])) return StatusCode(503, new { message = "Password reset email is not configured yet." }); // Avoids creating unusable tokens.
        var email = input.Email.Trim().ToLowerInvariant(); // Normalises the address in the same way as registration.
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken); // Finds the customer if one exists.
        if (customer is null) return Ok(new { message = RequestMessage }); // Avoids revealing whether the address is registered.
        var now = DateTime.UtcNow; // Uses one UTC time for the request.
        var recentRequest = await db.PasswordResetTokens.AnyAsync(x => x.CustomerId == customer.CustomerId && x.CreatedAtUtc > now.AddMinutes(-5), cancellationToken); // Limits repeat emails for this account.
        if (recentRequest) return Ok(new { message = RequestMessage }); // Returns the same response during the cooldown period.
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32)); // Creates a random URL-safe secret.
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))); // Stores a hash so a database leak does not expose the link.
        var record = new PasswordResetToken { CustomerId = customer.CustomerId, TokenHash = tokenHash, CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(30) }; // Creates a single-use record.
        db.PasswordResetTokens.Add(record); // Schedules the token record for insertion.
        await db.SaveChangesAsync(cancellationToken); // Saves the hash before sending the link.
        var resetUrl = pageUrl!.TrimEnd('/') + "#token=" + Uri.EscapeDataString(token); // Places the token in a URL fragment so it is not sent during the page request.
        try { await emailSender.SendResetLinkAsync(customer.Email, resetUrl, cancellationToken); } // Attempts to deliver the link.
        catch (Exception exception) // Handles an email provider failure.
        { // Starts failure handling.
            logger.LogError(exception, "Password reset email delivery failed."); // Records the failure without logging the token or address.
            db.PasswordResetTokens.Remove(record); // Removes the unusable token.
            await db.SaveChangesAsync(CancellationToken.None); // Persists the removal even if the request was cancelled.
            return StatusCode(503, new { message = "Email delivery is temporarily unavailable. Please try again later." }); // Gives a usable error without exposing the account.
        } // Ends failure handling.
        return Ok(new { message = RequestMessage }); // Confirms the request without exposing account existence.
    } // Ends the request action.

    [HttpPost("reset-password")] // Exposes POST /api/account/reset-password.
    public async Task<IActionResult> ResetPassword(ResetPasswordInput input, CancellationToken cancellationToken) // Exchanges a valid token for a new password.
    { // Starts the reset action.
        var now = DateTime.UtcNow; // Uses UTC for token expiry checks.
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Token))); // Hashes the submitted token for lookup.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken); // Keeps token use and password change together.
        var record = await db.PasswordResetTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.UsedAtUtc == null && x.ExpiresAtUtc > now, cancellationToken); // Finds an unused, unexpired token.
        if (record is null) return BadRequest(new { message = "This reset link is invalid or has expired. Request a new one." }); // Rejects a missing or expired token.
        var claimed = await db.PasswordResetTokens.Where(x => x.PasswordResetTokenId == record.PasswordResetTokenId && x.UsedAtUtc == null && x.ExpiresAtUtc > now).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAtUtc, (DateTime?)now), cancellationToken); // Claims the token only if it remains valid.
        if (claimed != 1) return BadRequest(new { message = "This reset link has already been used. Request a new one." }); // Prevents a second use.
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.CustomerId == record.CustomerId, cancellationToken); // Loads the account linked to the token.
        if (customer is null) return BadRequest(new { message = "This reset link is invalid. Request a new one." }); // Handles a deleted account.
        customer.PasswordHash = new PasswordHasher<Customer>().HashPassword(customer, input.NewPassword); // Saves a password hash rather than the password itself.
        await db.PasswordResetTokens.Where(x => x.CustomerId == customer.CustomerId && x.UsedAtUtc == null).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.UsedAtUtc, (DateTime?)now), cancellationToken); // Invalidates any other outstanding reset links.
        await db.SaveChangesAsync(cancellationToken); // Persists the new password hash.
        await transaction.CommitAsync(cancellationToken); // Commits the password change and token use together.
        return Ok(new { message = "Your password has been reset. You can now sign in." }); // Confirms the completed reset.
    } // Ends the reset action.
} // Ends the controller.
