using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Template.WebAPI.IntegrationTests.Infrastructure;

public abstract class ApiTest : IAsyncLifetime
{
    protected const string Api = "/api/v1";
    protected const string Password = "Integration!234";
    protected ApiFactory Factory { get; } = new();
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Factory.InitializeDatabaseAsync();
        Client = Factory.CreateApiClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }

    protected async Task<string> RegisterAsync(HttpClient client, bool confirm = true)
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        await ExpectAsync(await client.PostAsJsonAsync($"{Api}/auth/register", new { email, password = Password }), HttpStatusCode.OK);
        if (confirm)
            await ExpectAsync(await client.GetAsync(Factory.Emails.TakeConfirmationLink(email)), HttpStatusCode.OK);
        return email;
    }

    protected static async Task<JsonElement> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var tokens = await ReadAsync(await client.PostAsJsonAsync($"{Api}/auth/login", new { email, password }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        return tokens;
    }

    protected async Task<string> AuthenticateAsync(HttpClient? client = null)
    {
        client ??= Client;
        var email = await RegisterAsync(client);
        await LoginAsync(client, email);
        return email;
    }

    protected static async Task ExpectAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        using (response)
            Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    protected static async Task<JsonElement> ReadAsync(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        using (response)
        {
            Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
    }

    protected static async Task<Guid> CreateHomeAsync(HttpClient client, string name = "My home")
    {
        var home = await ReadAsync(await client.PostAsJsonAsync($"{Api}/homes", new { name }), HttpStatusCode.Created);
        Assert.Equal(name, home.GetProperty("name").GetString());
        return home.GetProperty("id").GetGuid();
    }

    protected static async Task<Guid> CreateRoomAsync(HttpClient client, Guid homeId, string name = "Kitchen")
    {
        var room = await ReadAsync(await client.PostAsJsonAsync($"{Api}/homes/{homeId}/rooms", new { name }), HttpStatusCode.Created);
        Assert.Equal(homeId, room.GetProperty("homeId").GetGuid());
        return room.GetProperty("id").GetGuid();
    }

    protected static async Task<Guid> CreateInventoryAsync(HttpClient client, Guid homeId, Guid? roomId = null, string name = "Coffee maker")
    {
        var item = await ReadAsync(await client.PostAsJsonAsync($"{Api}/inventory", new
        {
            name, homeId, roomId, description = "Espresso machine", serialNumber = "SN-123", price = 249.5
        }), HttpStatusCode.Created);
        Assert.Equal(name, item.GetProperty("name").GetString());
        return item.GetProperty("id").GetGuid();
    }
}
