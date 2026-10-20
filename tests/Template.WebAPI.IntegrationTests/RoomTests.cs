using System.Net;
using System.Net.Http.Json;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class RoomTests : ApiTest
{
    [Fact]
    public async Task Owner_can_create_list_update_and_delete_a_room()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var created = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Equal("Kitchen", Assert.Single(created.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());

        await ExpectAsync(await Client.PutAsJsonAsync($"{Api}/homes/{home}/rooms/{room}", new { name = "Office" }), HttpStatusCode.OK);
        var updated = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        var listed = Assert.Single(updated.GetProperty("items").EnumerateArray());
        Assert.Equal(room, listed.GetProperty("id").GetGuid());
        Assert.Equal("Office", listed.GetProperty("name").GetString());

        await ExpectAsync(await Client.DeleteAsync($"{Api}/homes/{home}/rooms/{room}"), HttpStatusCode.NoContent);
        var remaining = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Empty(remaining.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Deleting_a_room_preserves_inventory_and_clears_its_room_reference()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var item = await CreateInventoryAsync(Client, home, room);

        await ExpectAsync(await Client.DeleteAsync($"{Api}/homes/{home}/rooms/{room}"), HttpStatusCode.NoContent);

        var inventory = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal(home, inventory.GetProperty("homeId").GetGuid());
        Assert.Null(inventory.GetProperty("roomId").GetString());
        Assert.Equal("Coffee maker", inventory.GetProperty("name").GetString());
        Assert.Equal("SN-123", inventory.GetProperty("serialNumber").GetString());
        Assert.Equal(249.5, inventory.GetProperty("price").GetDouble());
        var rooms = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Empty(rooms.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Other_users_cannot_list_create_update_or_delete_rooms_in_a_home()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        using var other = Factory.CreateApiClient();
        await AuthenticateAsync(other);

        var hidden = await ReadAsync(await other.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Empty(hidden.GetProperty("items").EnumerateArray());
        await ExpectAsync(await other.PostAsJsonAsync($"{Api}/homes/{home}/rooms", new { name = "Stolen" }), HttpStatusCode.NotFound);
        await ExpectAsync(await other.PutAsJsonAsync($"{Api}/homes/{home}/rooms/{room}", new { name = "Stolen" }), HttpStatusCode.NotFound);
        await ExpectAsync(await other.DeleteAsync($"{Api}/homes/{home}/rooms/{room}"), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Equal("Kitchen", Assert.Single(original.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Room_routes_must_use_the_correct_home()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var otherHome = await CreateHomeAsync(Client, "Other home");

        await ExpectAsync(await Client.PutAsJsonAsync($"{Api}/homes/{otherHome}/rooms/{room}", new { name = "Moved" }), HttpStatusCode.NotFound);
        await ExpectAsync(await Client.DeleteAsync($"{Api}/homes/{otherHome}/rooms/{room}"), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Equal(room, Assert.Single(original.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(256)]
    public async Task Invalid_room_names_are_rejected_on_create_and_update(int length)
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var body = new { name = new string('a', length) };

        var createProblem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/homes/{home}/rooms", body), HttpStatusCode.BadRequest);
        var updateProblem = await ReadAsync(await Client.PutAsJsonAsync($"{Api}/homes/{home}/rooms/{room}", body), HttpStatusCode.BadRequest);

        Assert.True(createProblem.GetProperty("errors").TryGetProperty("Name", out _));
        Assert.True(updateProblem.GetProperty("errors").TryGetProperty("Name", out _));
        var original = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Equal("Kitchen", Assert.Single(original.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Room_list_filters_by_home_and_sorts_and_pages_results()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var otherHome = await CreateHomeAsync(Client, "Other home");
        await CreateRoomAsync(Client, home, "Office");
        await CreateRoomAsync(Client, home, "Bedroom");
        await CreateRoomAsync(Client, otherHome, "Garage");

        var page = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms?page=1&pageSize=1"));

        Assert.Equal("Bedroom", Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
        Assert.Equal(2, page.GetProperty("totalItemCount").GetInt32());
        Assert.True(page.GetProperty("hasNextPage").GetBoolean());
        Assert.False(page.GetProperty("hasPreviousPage").GetBoolean());
    }
}
