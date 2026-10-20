using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Template.WebAPI.Data.Models;

namespace Template.WebAPI.IntegrationTests.Infrastructure;

public sealed class TestEmailSender : IEmailSender<AppUser>
{
    private readonly ConcurrentDictionary<string, string> _confirmationLinks = new();
    private readonly ConcurrentDictionary<string, string> _resetCodes = new();

    public string TakeConfirmationLink(string email) => _confirmationLinks.TryRemove(email, out var link)
        ? link : throw new InvalidOperationException($"No confirmation email was sent to {email}.");

    public string TakeResetCode(string email) => _resetCodes.TryRemove(email, out var code)
        ? code : throw new InvalidOperationException($"No password reset email was sent to {email}.");

    public Task SendConfirmationLinkAsync(AppUser user, string email, string confirmationLink)
    {
        _confirmationLinks[email] = WebUtility.HtmlDecode(confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(AppUser user, string email, string resetCode)
    {
        _resetCodes[email] = WebUtility.HtmlDecode(resetCode);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(AppUser user, string email, string resetLink) =>
        throw new NotSupportedException("The API uses password reset codes.");
}
