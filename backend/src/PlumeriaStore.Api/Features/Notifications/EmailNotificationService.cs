using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Reservations;

namespace PlumeriaStore.Api.Features.Notifications;

public partial class EmailNotificationService
{
    private static readonly TimeZoneInfo PacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailSender emailSender,
        IOptions<EmailOptions> options,
        ILogger<EmailNotificationService> logger)
    {
        _emailSender = emailSender;
        _options = options.Value;
        _logger = logger;
    }

    // SES is still in sandbox mode, so both sender and recipient have to be verified addresses -
    // notifying "myself" means sending FromAddress to itself. Failures are logged and swallowed
    // rather than thrown: a pickup request that saved successfully shouldn't fail for the customer
    // just because the admin notification didn't send.
    public async Task NotifyNewPickupRequestAsync(PickupRequestResponse request)
    {
        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogWarning("Skipping new-pickup-request email: EMAIL_FROM_ADDRESS is not configured");
            return;
        }

        try
        {
            await _emailSender.SendAsync(
                _options.FromAddress,
                $"New pickup request from {request.CustomerName}",
                BuildHtmlBody(request),
                BuildTextBody(request));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send new-pickup-request notification email");
        }
    }

    // Mirrors the admin "View Request" modal's layout (a card floating on the site's canvas),
    // but with the two surfaces swapped - white page behind a blue card - since a light-on-blue
    // headline read better than dark text on white here. Inlined styles throughout since email
    // clients don't reliably support external/embedded stylesheets.
    private static string BuildHtmlBody(PickupRequestResponse request)
    {
        var itemRows = string.Join("\n", request.Items.Select(item => $"""
                          <tr>
                            <td style="padding:0.7rem 0.9rem;border-bottom:1px solid rgba(244,249,251,0.25);font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.9rem;color:#f4f9fb;">{Esc(item.ItemSnapshot)}</td>
                            <td align="right" style="padding:0.7rem 0.9rem;border-bottom:1px solid rgba(244,249,251,0.25);font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.9rem;color:#f4f9fb;">{item.QuantityRequested}</td>
                          </tr>
            """));

        var requestedAt = FormatPacificTime(request.CreatedAt);
        var phone = FormatPhone(request.CustomerPhone);

        return $"""
            <!doctype html>
            <html>
              <body style="margin:0;padding:32px 16px;background-color:#ffffff;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#ffffff;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:520px;">
                        <tr>
                          <td align="center" style="padding-bottom:20px;">
                            <h1 style="margin:0;font-family:'Palatino Linotype','Book Antiqua',Georgia,serif;font-style:italic;letter-spacing:0.3px;font-size:26px;line-height:1.3;font-weight:600;color:#1c3444;text-align:center;">
                              New pickup request from {Esc(request.CustomerName)}
                            </h1>
                          </td>
                        </tr>
                        <tr>
                          <td style="background-color:#436c85;border-radius:12px;padding:24px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                              <tr><td style="font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.9rem;color:#f4f9fb;padding-bottom:6px;"><strong>Requested:</strong> {requestedAt}</td></tr>
                              <tr><td style="font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.9rem;color:#f4f9fb;padding-bottom:6px;"><strong>Phone:</strong> {Esc(phone)}</td></tr>
                              <tr><td style="font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.9rem;color:#f4f9fb;padding-bottom:18px;"><strong>Email:</strong> {Esc(request.CustomerEmail)}</td></tr>
                            </table>
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;margin-bottom:18px;">
                              <thead>
                                <tr>
                                  <th align="left" style="padding:0.6rem 0.9rem;border-bottom:1px solid rgba(244,249,251,0.25);font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.8rem;color:rgba(244,249,251,0.82);">Item</th>
                                  <th align="right" style="padding:0.6rem 0.9rem;border-bottom:1px solid rgba(244,249,251,0.25);font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.8rem;color:rgba(244,249,251,0.82);">Qty</th>
                                </tr>
                              </thead>
                              <tbody>
            {itemRows}
                              </tbody>
                            </table>
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                              <tr><td style="font-family:'Montserrat','Segoe UI',sans-serif;font-size:0.9rem;color:#f4f9fb;"><strong>Notes:</strong> {Esc(request.Notes)}</td></tr>
                            </table>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;
    }

    private static string BuildTextBody(PickupRequestResponse request)
    {
        var itemLines = request.Items.Select(item => $"- {item.QuantityRequested} x {item.ItemSnapshot}");

        return $"""
            New pickup request from {request.CustomerName}

            Requested: {FormatPacificTime(request.CreatedAt)}
            Phone: {FormatPhone(request.CustomerPhone) ?? "—"}
            Email: {request.CustomerEmail ?? "—"}

            Items:
            {string.Join("\n", itemLines)}

            Notes: {request.Notes ?? "—"}
            """;
    }

    // Backend validation guarantees exactly 10 digits when a phone is present (see
    // ReservationService.CreateAsync) - mirrors the frontend's "(xxx) xxx-xxxx" display format.
    private static string? FormatPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = DigitsOnly().Replace(phone, "");
        return digits.Length == 10 ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}" : phone;
    }

    private static string FormatPacificTime(DateTime utc)
    {
        var pacific = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), PacificTimeZone);
        var abbreviation = PacificTimeZone.IsDaylightSavingTime(pacific) ? "PDT" : "PST";
        return $"{pacific:MMM d, yyyy 'at' h:mm tt} {abbreviation}";
    }

    private static string Esc(string? value) => WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "—" : value);

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
