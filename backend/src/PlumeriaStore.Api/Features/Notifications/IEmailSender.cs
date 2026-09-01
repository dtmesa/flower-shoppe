namespace PlumeriaStore.Api.Features.Notifications;

public interface IEmailSender
{
    /// <summary>
    /// textBody is a plain-text fallback for clients that don't render HTML - sent alongside
    /// htmlBody in the same message rather than as a separate call.
    /// </summary>
    Task SendAsync(string to, string subject, string htmlBody, string textBody);
}
