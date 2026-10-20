using System.Net;
using System.Net.Http.Json;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class RequestValidationTests : ApiTest
{
    [Theory]
    [InlineData("/homes", "page=0", "Page")]
    [InlineData("/homes", "pageSize=0", "PageSize")]
    [InlineData("/homes", "pageSize=101", "PageSize")]
    [InlineData("/homes/{id}/rooms", "page=0", "Page")]
    [InlineData("/homes/{id}/rooms", "pageSize=0", "PageSize")]
    [InlineData("/homes/{id}/rooms", "pageSize=101", "PageSize")]
    [InlineData("/inventory", "page=0", "Page")]
    [InlineData("/inventory", "pageSize=0", "PageSize")]
    [InlineData("/inventory", "pageSize=101", "PageSize")]
    [InlineData("/pros", "page=0", "Page")]
    [InlineData("/pros", "pageSize=0", "PageSize")]
    [InlineData("/pros", "pageSize=101", "PageSize")]
    public async Task Invalid_pagination_returns_a_validation_problem(string route, string query, string field)
    {
        await AuthenticateAsync();
        var path = route.Replace("{id}", Guid.NewGuid().ToString());

        var problem = await ReadAsync(await Client.GetAsync($"{Api}{path}?{query}"), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("GET", "/homes/{id}")]
    [InlineData("PUT", "/homes/{id}")]
    [InlineData("DELETE", "/homes/{id}")]
    [InlineData("POST", "/homes/{id}/rooms")]
    [InlineData("PUT", "/homes/{id}/rooms/{id}")]
    [InlineData("DELETE", "/homes/{id}/rooms/{id}")]
    [InlineData("GET", "/inventory/{id}")]
    [InlineData("PUT", "/inventory/{id}")]
    [InlineData("DELETE", "/inventory/{id}")]
    [InlineData("PUT", "/pros/{id}")]
    [InlineData("DELETE", "/pros/{id}")]
    public async Task Missing_resources_return_not_found(string method, string route)
    {
        await AuthenticateAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), Api + route.Replace("{id}", Guid.NewGuid().ToString()));
        if (method is "PUT" or "POST")
            request.Content = JsonContent.Create(new { name = "Missing resource" });

        await ExpectAsync(await Client.SendAsync(request), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Malformed_json_returns_bad_request()
    {
        await AuthenticateAsync();
        using var content = new StringContent("{invalid", System.Text.Encoding.UTF8, "application/json");

        await ExpectAsync(await Client.PostAsync($"{Api}/homes", content), HttpStatusCode.BadRequest);
    }
}
