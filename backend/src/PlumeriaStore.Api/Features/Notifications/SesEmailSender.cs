using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Notifications;

public class SesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly EmailOptions _options;

    public SesEmailSender(IAmazonSimpleEmailServiceV2 sesClient, IOptions<EmailOptions> options)
    {
        _sesClient = sesClient;
        _options = options.Value;
    }

    public Task SendAsync(string to, string subject, string htmlBody, string textBody)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = _options.FromAddress,
            Destination = new Destination { ToAddresses = [to] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Data = htmlBody },
                        Text = new Content { Data = textBody },
                    },
                },
            },
        };

        return _sesClient.SendEmailAsync(request);
    }
}
