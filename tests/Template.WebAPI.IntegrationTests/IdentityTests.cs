using System.Net;
using System.Net.Http.Json;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class IdentityTests : ApiTest
{
    [Fact]
    public async Task Registration_requires_email_confirmation_before_login()
    {
        var email = await RegisterAsync(Client, confirm: false);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = Password }), HttpStatusCode.Unauthorized);
        await ExpectAsync(await Client.GetAsync(Factory.Emails.TakeConfirmationLink(email)), HttpStatusCode.OK);
        var tokens = await LoginAsync(Client, email);
        var info = await ReadAsync(await Client.GetAsync($"{Api}/auth/manage/info"));

        Assert.Equal("Bearer", tokens.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrEmpty(tokens.GetProperty("refreshToken").GetString()));
        Assert.Equal(email, info.GetProperty("email").GetString());
        Assert.True(info.GetProperty("isEmailConfirmed").GetBoolean());
    }

    [Theory]
    [InlineData("invalid-email", "Integration!234", "InvalidEmail")]
    [InlineData("valid@example.com", "weak", "PasswordTooShort")]
    public async Task Invalid_registration_returns_validation_errors(string email, string password, string error)
    {
        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/register", new { email, password }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty(error, out _));
    }

    [Fact]
    public async Task Duplicate_registration_is_rejected()
    {
        var email = await RegisterAsync(Client);

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/register", new { email, password = Password }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("DuplicateUserName", out _));
    }

    [Fact]
    public async Task Wrong_password_cannot_log_in()
    {
        var email = await RegisterAsync(Client);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = "Wrong!234" }), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_token_issues_a_usable_access_token()
    {
        var email = await RegisterAsync(Client);
        var login = await LoginAsync(Client, email);
        Client.DefaultRequestHeaders.Authorization = null;

        var refreshed = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken = login.GetProperty("refreshToken").GetString() }));
        Client.DefaultRequestHeaders.Authorization = new("Bearer", refreshed.GetProperty("accessToken").GetString());
        var info = await ReadAsync(await Client.GetAsync($"{Api}/auth/manage/info"));

        Assert.Equal(email, info.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Invalid_refresh_token_is_rejected()
    {
        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken = "invalid" }), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_confirmation_link_is_rejected()
    {
        await ExpectAsync(await Client.GetAsync($"{Api}/auth/confirmEmail?userId=missing&code=invalid"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resent_confirmation_email_can_confirm_an_account()
    {
        var email = await RegisterAsync(Client, confirm: false);
        Factory.Emails.TakeConfirmationLink(email);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/resendConfirmationEmail", new { email }), HttpStatusCode.OK);
        await ExpectAsync(await Client.GetAsync(Factory.Emails.TakeConfirmationLink(email)), HttpStatusCode.OK);
        await LoginAsync(Client, email);
        var info = await ReadAsync(await Client.GetAsync($"{Api}/auth/manage/info"));

        Assert.True(info.GetProperty("isEmailConfirmed").GetBoolean());
    }

    [Theory]
    [InlineData("resendConfirmationEmail")]
    [InlineData("forgotPassword")]
    public async Task Email_endpoints_do_not_disclose_unknown_accounts(string endpoint)
    {
        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/{endpoint}", new { email = "missing@example.com" }), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Password_reset_code_changes_the_password()
    {
        var email = await RegisterAsync(Client);
        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/forgotPassword", new { email }), HttpStatusCode.OK);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/resetPassword", new
        {
            email, resetCode = Factory.Emails.TakeResetCode(email), newPassword = "Replacement!234"
        }), HttpStatusCode.OK);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = Password }), HttpStatusCode.Unauthorized);
        await LoginAsync(Client, email, "Replacement!234");
        var info = await ReadAsync(await Client.GetAsync($"{Api}/auth/manage/info"));
        Assert.Equal(email, info.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Invalid_password_reset_code_does_not_change_the_password()
    {
        var email = await RegisterAsync(Client);

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/resetPassword", new
        {
            email, resetCode = "invalid!", newPassword = "Replacement!234"
        }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("InvalidToken", out _));
        await LoginAsync(Client, email);
    }

    [Fact]
    public async Task Account_owner_can_change_password()
    {
        var email = await AuthenticateAsync();

        var info = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/manage/info", new
        {
            oldPassword = Password, newPassword = "Changed!234"
        }));

        Assert.Equal(email, info.GetProperty("email").GetString());
        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/auth/login", new { email, password = Password }), HttpStatusCode.Unauthorized);
        await LoginAsync(Client, email, "Changed!234");
    }

    [Fact]
    public async Task Account_email_changes_only_after_confirmation()
    {
        var email = await AuthenticateAsync();
        const string newEmail = "changed@example.com";

        var pending = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/manage/info", new { newEmail }));
        Assert.Equal(email, pending.GetProperty("email").GetString());
        await ExpectAsync(await Client.GetAsync(Factory.Emails.TakeConfirmationLink(newEmail)), HttpStatusCode.OK);
        var confirmed = await ReadAsync(await Client.GetAsync($"{Api}/auth/manage/info"));

        Assert.Equal(newEmail, confirmed.GetProperty("email").GetString());
        await LoginAsync(Client, newEmail);
    }

    [Fact]
    public async Task Changing_password_requires_the_current_password()
    {
        await AuthenticateAsync();

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/manage/info", new { newPassword = "Changed!234" }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("OldPasswordRequired", out _));
    }

    [Fact]
    public async Task Account_owner_can_get_and_reset_two_factor_configuration()
    {
        await AuthenticateAsync();
        var initial = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/manage/2fa", new { }));

        var reset = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/manage/2fa", new
        {
            resetSharedKey = true, resetRecoveryCodes = true, forgetMachine = true
        }));

        Assert.False(reset.GetProperty("isTwoFactorEnabled").GetBoolean());
        Assert.False(reset.GetProperty("isMachineRemembered").GetBoolean());
        Assert.NotEqual(initial.GetProperty("sharedKey").GetString(), reset.GetProperty("sharedKey").GetString());
        Assert.Equal(10, reset.GetProperty("recoveryCodes").GetArrayLength());
        Assert.Equal(10, reset.GetProperty("recoveryCodesLeft").GetInt32());
    }

    [Fact]
    public async Task Enabling_two_factor_authentication_requires_a_valid_code()
    {
        await AuthenticateAsync();

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/auth/manage/2fa", new { enable = true, twoFactorCode = "invalid" }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("InvalidTwoFactorCode", out _));
    }
}
