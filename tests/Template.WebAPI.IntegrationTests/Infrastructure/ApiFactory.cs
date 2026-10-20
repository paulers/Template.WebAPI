using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Template.WebAPI.Api.Modules.Email;
using Template.WebAPI.Data;
using Template.WebAPI.Data.Models;

namespace Template.WebAPI.IntegrationTests.Infrastructure;

public sealed class ApiFactory(string environment = "Testing") : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
    public TestEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused;Database=unused");
        foreach (var section in new[] { "Development", "Production" })
        {
            builder.UseSetting($"SmtpOptions:{section}:Host", "localhost");
            builder.UseSetting($"SmtpOptions:{section}:FromEmail", "tests@example.com");
            builder.UseSetting($"SmtpOptions:{section}:FromName", "Integration tests");
        }

        builder.ConfigureTestServices(services =>
        {
            _connection.Open();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            // Replace only the external email boundary. Identity and its token providers stay real.
            services.RemoveAll<IEmailSender<AppUser>>();
            services.AddSingleton<IEmailSender<AppUser>>(Emails);
            var emailProcessor = services.Single(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(EmailProcessor));
            services.Remove(emailProcessor);
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await _connection.DisposeAsync();
        }
    }
}
