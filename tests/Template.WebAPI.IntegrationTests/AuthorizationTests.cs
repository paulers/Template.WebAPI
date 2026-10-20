using System.Net;
using System.Text;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class AuthorizationTests : ApiTest
{
    [Theory]
    [InlineData("GET", "/homes")]
    [InlineData("POST", "/homes")]
    [InlineData("GET", "/homes/{id}")]
    [InlineData("PUT", "/homes/{id}")]
    [InlineData("DELETE", "/homes/{id}")]
    [InlineData("GET", "/homes/{id}/rooms")]
    [InlineData("POST", "/homes/{id}/rooms")]
    [InlineData("PUT", "/homes/{id}/rooms/{id}")]
    [InlineData("DELETE", "/homes/{id}/rooms/{id}")]
    [InlineData("GET", "/inventory")]
    [InlineData("POST", "/inventory")]
    [InlineData("GET", "/inventory/{id}")]
    [InlineData("PUT", "/inventory/{id}")]
    [InlineData("DELETE", "/inventory/{id}")]
    [InlineData("GET", "/pros")]
    [InlineData("POST", "/pros")]
    [InlineData("PUT", "/pros/{id}")]
    [InlineData("DELETE", "/pros/{id}")]
    [InlineData("GET", "/pros/types")]
    [InlineData("GET", "/auth/manage/info")]
    [InlineData("POST", "/auth/manage/info")]
    [InlineData("POST", "/auth/manage/2fa")]
    public async Task Protected_endpoint_rejects_unauthenticated_requests(string method, string route)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), Api + route.Replace("{id}", Guid.NewGuid().ToString()));
        if (method is "POST" or "PUT")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        await ExpectAsync(await Client.SendAsync(request), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_bearer_token_cannot_access_protected_endpoints()
    {
        Client.DefaultRequestHeaders.Authorization = new("Bearer", "invalid-token");

        await ExpectAsync(await Client.GetAsync($"{Api}/homes"), HttpStatusCode.Unauthorized);
    }
}
