using Azure; // Provides the option to wait for the email operation to complete.
using Azure.Communication.Email; // Provides the Azure Communication Services email client.
using System.Text.Encodings.Web; // Encodes a URL before placing it in HTML.
namespace ForgeAndFade.Api.Services; // Places the sender in the application's services namespace.
public interface IPasswordResetEmailSender // Preserves the interface used by the reset controller.
{ // Starts the interface.
    Task SendResetLinkAsync(string email, string resetUrl, CancellationToken cancellationToken); // Defines the reset email operation.
} // Ends the interface.
public sealed class AzurePasswordResetEmailSender(IConfiguration configuration) : IPasswordResetEmailSender // Receives settings through dependency injection.
{ // Starts the Azure sender.
    public async Task SendResetLinkAsync(string email, string resetUrl, CancellationToken cancellationToken) // Sends a reset link to one customer.
    { // Starts the method.
        var connectionString = configuration["AzureCommunicationServices:ConnectionString"]; // Reads the Azure connection string from configuration.
        var senderAddress = configuration["AzureCommunicationServices:SenderAddress"]; // Reads the verified Azure sender address.
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(senderAddress)) throw new InvalidOperationException("Azure email settings are missing."); // Stops if either setting is absent.
        var client = new EmailClient(connectionString); // Creates a client for your Azure Communication Services resource.
        var safeUrl = HtmlEncoder.Default.Encode(resetUrl); // Prevents the URL from breaking the HTML link.
        var html = $"<p>Use this link to reset your Forge &amp; Fade password within 30 minutes:</p><p><a href=\"{safeUrl}\">Reset your password</a></p><p>If you did not request this, you can ignore this email.</p>"; // Creates the clickable email content.
        var plainText = $"Use this link to reset your Forge & Fade password within 30 minutes:{Environment.NewLine}{resetUrl}{Environment.NewLine}If you did not request this, you can ignore this email."; // Creates a text version of the email.
        await client.SendAsync(WaitUntil.Completed, senderAddress, email, "Reset your Forge & Fade password", html, plainText, cancellationToken); // Sends the email through Azure.
    } // Ends the method.
} // Ends the sender.