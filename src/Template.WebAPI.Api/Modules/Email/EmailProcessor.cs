using System.Threading.Channels;
using MailKit.Net.Smtp;
using MimeKit;

namespace Template.WebAPI.Api.Modules.Email;

public class EmailProcessor(ILogger<EmailProcessor> logger, IOptionsMonitor<SmtpOptions> smtpOptions, Channel<EmailMessage> notificationChannel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await notificationChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            var notification = await notificationChannel.Reader.ReadAsync(stoppingToken);
            await SendEmail(notification, stoppingToken);
        }
    }

    private async Task SendEmail(EmailMessage notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.Email)) return;

        try
        {
            var options = smtpOptions.CurrentValue;
            using var email = new MimeMessage();
            email.From.Add(new MailboxAddress(options.FromName, options.FromEmail));
            email.To.Add(new MailboxAddress(notification.Email, notification.Email));
            email.Subject = notification.Subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = notification.Message
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port, options.UseSsl, cancellationToken);
            if (options.RequireAuthentication)
            {
                if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
                    throw new InvalidOperationException("Username or password missing when authentication is required.");

                await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
            }

            var response = await client.SendAsync(email, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        
            logger.LogInformation("Response: {Response} -- {Subject} email sent to {Email}", response, notification.Subject, notification.Email);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Subject} email to {Email}", notification.Subject, notification.Email);
        }
    }
}

/// <summary>
/// DTO representing the notification to be sent to a user.
/// </summary>
/// <param name="UserId">Id of the user to send to</param>
/// <param name="Message">Message to send</param>
/// <param name="Email">Optional e-mail address</param>
public record EmailMessage(string UserId, string Subject, string Message, string? Email = null);
