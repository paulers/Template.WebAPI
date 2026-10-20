using System.Net;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class HealthTests : ApiTest
{
    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Development_openapi_endpoint_describes_the_api()
    {
        await using var development = new ApiFactory("Development");
        using var client = development.CreateApiClient();

        var document = await ReadAsync(await client.GetAsync("/openapi/v1.json"));

        Assert.StartsWith("3.", document.GetProperty("openapi").GetString());
        Assert.Contains(document.GetProperty("paths").EnumerateObject(), path => path.Name.EndsWith("/auth/login", StringComparison.Ordinal));
        Assert.Contains(document.GetProperty("paths").EnumerateObject(), path => path.Name.Contains("/inventory", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Openapi_is_unavailable_outside_development()
    {
        await ExpectAsync(await Client.GetAsync("/openapi/v1.json"), HttpStatusCode.NotFound);
    }
}
