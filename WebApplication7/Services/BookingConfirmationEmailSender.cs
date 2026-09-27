using System.Globalization; // Formats dates, times and prices consistently.
using System.Text; // Builds the HTML and calendar attachment.
using System.Text.Encodings.Web; // Encodes customer and URL values before inserting them into HTML.
using Azure; // Allows the email send operation to wait for completion.
using Azure.Communication.Email; // Sends the message and calendar attachment through Azure.
namespace ForgeAndFade.Api.Services; // Keeps this sender in the existing services namespace.

public sealed record BookingEmailDetails(int BookingId, string Email, string FirstName, string ServiceName, string BarberName, DateTime Date, TimeSpan StartTime, TimeSpan EndTime, decimal Price, bool IsForChild); // Carries confirmed booking details without passing an EF entity to the sender.
public interface IBookingConfirmationEmailSender // Defines the operation the booking controller will request.
{ // Starts the interface.
    Task SendAsync(BookingEmailDetails details, CancellationToken cancellationToken); // Sends one confirmation for a saved booking.
} // Ends the interface.
public sealed class AzureBookingConfirmationEmailSender(IConfiguration configuration) : IBookingConfirmationEmailSender // Reads the existing Azure email settings through dependency injection.
{ // Starts the sender.
    public async Task SendAsync(BookingEmailDetails details, CancellationToken cancellationToken) // Creates and sends the branded confirmation.
    { // Starts the send operation.
        var connectionString = configuration["AzureCommunicationServices:ConnectionString"]; // Reads the same Azure resource connection used by password reset.
        var senderAddress = configuration["AzureCommunicationServices:SenderAddress"]; // Reads the connected and authorised sender address.
        var address = configuration["Booking:StudioAddress"]; // Reads the studio location from configuration.
        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(senderAddress) || string.IsNullOrWhiteSpace(address)) throw new InvalidOperationException("Booking email configuration is incomplete."); // Prevents an email with missing sender or directions.
        var localStart = details.Date.Date.Add(details.StartTime); // Combines the stored appointment date and South African local start time.
        var localEnd = details.Date.Date.Add(details.EndTime); // Combines the stored appointment date and South African local end time.
        var utcStart = DateTime.SpecifyKind(localStart.AddHours(-2), DateTimeKind.Utc); // Converts South African standard time to UTC for calendars.
        var utcEnd = DateTime.SpecifyKind(localEnd.AddHours(-2), DateTimeKind.Utc); // Converts the appointment end to UTC as well.
        var culture = CultureInfo.GetCultureInfo("en-ZA"); // Uses South African date and price formatting.
        var dateLabel = localStart.ToString("dddd, d MMMM yyyy", culture); // Produces a readable appointment date.
        var timeLabel = $"{localStart:HH:mm}–{localEnd:HH:mm} SAST"; // Shows the customer the local appointment window.
        var priceLabel = $"R{details.Price.ToString("N2", culture)}"; // Shows the service price in rand.
        var recipientLabel = details.IsForChild ? "Your child" : "You"; // Makes junior bookings clear in the email.
        var directionsUrl = "https://www.google.com/maps/dir/?api=1&destination=" + Uri.EscapeDataString(address); // Opens Google Maps directions to the configured street address.
        var appleDirectionsUrl = "https://maps.apple.com/?daddr=" + Uri.EscapeDataString(address); // Creates an Apple Maps directions link to the same studio address.
        var title = $"Forge & Fade: {details.ServiceName}"; // Names the calendar event.
        var calendarUrl = $"https://calendar.google.com/calendar/render?action=TEMPLATE&text={Uri.EscapeDataString(title)}&dates={utcStart:yyyyMMddTHHmmssZ}/{utcEnd:yyyyMMddTHHmmssZ}&details={Uri.EscapeDataString($"Booking #{details.BookingId} with {details.BarberName}.")}&location={Uri.EscapeDataString(address)}"; // Lets Google Calendar users add the correct time and location.
        var safeName = HtmlEncoder.Default.Encode(details.FirstName); // Prevents a stored name from becoming HTML.
        var safeService = HtmlEncoder.Default.Encode(details.ServiceName); // Encodes the service name for the email.
        var safeBarber = HtmlEncoder.Default.Encode(details.BarberName); // Encodes the barber name for the email.
        var safeAddress = HtmlEncoder.Default.Encode(address); // Encodes the street address for display.
        var safeCalendarUrl = HtmlEncoder.Default.Encode(calendarUrl); // Encodes the calendar link for its HTML attribute.
        var safeDirectionsUrl = HtmlEncoder.Default.Encode(directionsUrl); // Encodes the directions link for its HTML attribute.
        var safeAppleDirectionsUrl = HtmlEncoder.Default.Encode(appleDirectionsUrl); // Encodes the Apple Maps URL before placing it in HTML.
        var html = new StringBuilder(); // Builds the email with inline styling supported by email clients.
        html.Append("<!doctype html><html><body style=\"margin:0;background:#f4f0ea;color:#191919;font-family:Arial,sans-serif;\">"); // Sets the cream background and readable type.
        html.Append("<table role=\"presentation\" style=\"width:100%;border-collapse:collapse;\"><tr><td align=\"center\" style=\"padding:32px 16px;\">"); // Centres the email content on desktop and mobile.
        html.Append("<table role=\"presentation\" style=\"width:100%;max-width:600px;border-collapse:collapse;background:#f4f0ea;\">"); // Keeps the email at a comfortable reading width.
        html.Append("<tr><td style=\"padding:28px;background:#101010;color:#fff;font-size:22px;font-weight:bold;letter-spacing:3px;\">FORGE <span style=\"color:#c6a57d;\">&amp;</span> FADE<br><span style=\"font-size:10px;letter-spacing:5px;\">BARBER STUDIO</span></td></tr>"); // Repeats the website's dark masthead.
        html.Append($"<tr><td style=\"padding:36px 28px 12px;\"><div style=\"color:#a47748;font-size:12px;letter-spacing:3px;font-weight:bold;\">BOOKING CONFIRMED</div><h1 style=\"font-size:32px;line-height:1.15;margin:18px 0;\">Your chair is reserved.</h1><p style=\"font-size:16px;line-height:1.6;\">Hi {safeName}, {recipientLabel.ToLowerInvariant()} are booked at Forge &amp; Fade.</p></td></tr>"); // Greets the customer and confirms the recipient.
        html.Append($"<tr><td style=\"padding:8px 28px 24px;\"><table role=\"presentation\" style=\"width:100%;border:1px solid #c9c0b4;border-collapse:collapse;\"><tr><td style=\"padding:22px;font-size:15px;line-height:1.9;\"><strong>Service:</strong> {safeService}<br><strong>Barber:</strong> {safeBarber}<br><strong>Date:</strong> {dateLabel}<br><strong>Time:</strong> {timeLabel}<br><strong>Price:</strong> {priceLabel}<br><strong>Booking:</strong> #{details.BookingId}<br><strong>Address:</strong> {safeAddress}</td></tr></table></td></tr>"); // Displays all appointment details in one rectangular card.
        html.Append($"<tr><td style=\"padding:0 28px 12px;\"><a href=\"{safeCalendarUrl}\" style=\"display:block;padding:18px;background:#c6a57d;color:#111;text-align:center;text-decoration:none;font-size:13px;font-weight:bold;letter-spacing:2px;\">ADD TO GOOGLE CALENDAR</a></td></tr>"); // Provides the calendar action.
        html.Append($"<tr><td style=\"padding:0 28px 24px;\"><a href=\"{safeDirectionsUrl}\" style=\"display:block;padding:18px;background:#101010;color:#fff;text-align:center;text-decoration:none;font-size:13px;font-weight:bold;letter-spacing:2px;\">GET DIRECTIONS</a></td></tr>"); // Provides navigation to the studio.
        html.Append($"<tr><td style=\"padding:0 28px 24px;\"><a href=\"{safeAppleDirectionsUrl}\" style=\"display:block;padding:18px;background:#101010;color:#fff;text-align:center;text-decoration:none;font-size:13px;font-weight:bold;letter-spacing:2px;\">APPLE MAPS DIRECTIONS</a></td></tr>"); // Adds a second rectangular directions button for Apple Maps.
        html.Append("<tr><td style=\"padding:0 28px 36px;color:#625e59;font-size:13px;line-height:1.6;\">An appointment.ics file is attached for Apple Calendar, Outlook and other calendar apps. You may reschedule online only until three hours before your appointment.</td></tr>"); // Explains the attachment and existing rescheduling condition.
        html.Append("</table></td></tr></table></body></html>"); // Closes the email layout.
        var plainText = $"Forge & Fade booking confirmed{Environment.NewLine}Booking #{details.BookingId}{Environment.NewLine}{details.ServiceName} with {details.BarberName}{Environment.NewLine}{dateLabel}, {timeLabel}{Environment.NewLine}{priceLabel}{Environment.NewLine}{address}{Environment.NewLine}Add to Google Calendar: {calendarUrl}{Environment.NewLine}Google Maps: {directionsUrl}{Environment.NewLine}Apple Maps: {appleDirectionsUrl}{Environment.NewLine}Rescheduling closes three hours before your appointment."; // Gives customers a complete text-only alternative.
        var calendar = string.Join("\r\n", new[] { // Builds an iCalendar file using CRLF line endings.
            "BEGIN:VCALENDAR", // Begins the calendar file.
            "VERSION:2.0", // Declares the iCalendar format version.
            "PRODID:-//Forge and Fade//Booking Confirmation//EN", // Identifies the application that created the event.
            "BEGIN:VEVENT", // Begins the appointment event.
            $"UID:forgeandfade-booking-{details.BookingId}@forgeandfade.example", // Gives this booking a stable event identifier.
            $"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}", // Records when the calendar file was generated.
            $"DTSTART:{utcStart:yyyyMMddTHHmmssZ}", // Gives calendar apps the UTC start time.
            $"DTEND:{utcEnd:yyyyMMddTHHmmssZ}", // Gives calendar apps the UTC end time.
            $"SUMMARY:{EscapeCalendar(title)}", // Adds the service to the calendar title.
            $"LOCATION:{EscapeCalendar(address)}", // Adds the studio address to the event.
            $"DESCRIPTION:{EscapeCalendar($"Booking #{details.BookingId} with {details.BarberName}. Directions: {directionsUrl}")}", // Includes booking details and directions.
            "END:VEVENT", // Ends the appointment event.
            "END:VCALENDAR" // Ends the calendar file.
        }) + "\r\n"; // Finishes the file with the required line ending.
        var content = new EmailContent($"Forge & Fade booking #{details.BookingId} confirmed") { Html = html.ToString(), PlainText = plainText }; // Supplies both styled and plain-text email versions.
        var message = new EmailMessage(senderAddress, details.Email, content); // Addresses the email to the customer.
        message.Attachments.Add(new EmailAttachment("appointment.ics", "text/calendar", BinaryData.FromBytes(Encoding.UTF8.GetBytes(calendar)))); // Attaches a calendar file for non-Google apps.
        var client = new EmailClient(connectionString); // Uses the working Azure Communication Services resource.
        await client.SendAsync(WaitUntil.Completed, message, cancellationToken); // Waits for Azure to accept or reject the message.
    } // Ends the send operation.
    private static string EscapeCalendar(string value) => value.Replace("\\", "\\\\").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace(",", "\\,").Replace(";", "\\;"); // Escapes reserved iCalendar characters in names and addresses.
} // Ends the sender.
