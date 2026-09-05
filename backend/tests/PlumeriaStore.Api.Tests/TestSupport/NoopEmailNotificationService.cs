using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Notifications;

namespace PlumeriaStore.Api.Tests.TestSupport;

file class NoopEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, string textBody) => Task.CompletedTask;
}

/// <summary>
/// A real EmailNotificationService wired to a no-op sender, so ReservationService tests don't
/// need real AWS credentials or a network call - EMAIL_FROM_ADDRESS is left blank, which the service
/// already treats as "notifications disabled" and skips without erroring.
/// </summary>
public static class NoopEmailNotificationService
{
    public static EmailNotificationService Create() =>
        new(new NoopEmailSender(), Options.Create(new EmailOptions()), NullLogger<EmailNotificationService>.Instance);
}
