using System.Net;
using System.Net.Http.Json;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class InventoryTests : ApiTest
{
    [Fact]
    public async Task Owner_can_create_read_update_list_and_delete_inventory()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var item = await CreateInventoryAsync(Client, home, room);
        var created = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal(room, created.GetProperty("roomId").GetGuid());
        Assert.Equal("Espresso machine", created.GetProperty("description").GetString());

        var otherHome = await CreateHomeAsync(Client, "Other home");
        var otherRoom = await CreateRoomAsync(Client, otherHome, "Office");
        var updated = await ReadAsync(await Client.PutAsJsonAsync($"{Api}/inventory/{item}", new
        {
            homeId = otherHome, roomId = otherRoom, name = "Printer", description = "Laser printer", serialNumber = "SN-456", price = 125.75
        }));
        Assert.Equal(item, updated.GetProperty("id").GetGuid());
        var persisted = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal(otherHome, persisted.GetProperty("homeId").GetGuid());
        Assert.Equal(otherRoom, persisted.GetProperty("roomId").GetGuid());
        Assert.Equal("Printer", persisted.GetProperty("name").GetString());
        Assert.Equal("Laser printer", persisted.GetProperty("description").GetString());
        Assert.Equal("SN-456", persisted.GetProperty("serialNumber").GetString());
        Assert.Equal(125.75, persisted.GetProperty("price").GetDouble());
        var list = await ReadAsync(await Client.GetAsync($"{Api}/inventory"));
        Assert.Equal(item, Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        await ExpectAsync(await Client.DeleteAsync($"{Api}/inventory/{item}"), HttpStatusCode.NoContent);
        await ExpectAsync(await Client.GetAsync($"{Api}/inventory/{item}"), HttpStatusCode.NotFound);
        var remaining = await ReadAsync(await Client.GetAsync($"{Api}/inventory"));
        Assert.Empty(remaining.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Inventory_can_be_created_without_a_room_and_unassigned_from_a_room()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var withoutRoom = await CreateInventoryAsync(Client, home);
        var unassigned = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{withoutRoom}"));
        Assert.Null(unassigned.GetProperty("roomId").GetString());
        var room = await CreateRoomAsync(Client, home);
        var item = await CreateInventoryAsync(Client, home, room);

        await ReadAsync(await Client.PutAsJsonAsync($"{Api}/inventory/{item}", new { name = "Unassigned item", roomId = (Guid?)null }));

        var persisted = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal(home, persisted.GetProperty("homeId").GetGuid());
        Assert.Null(persisted.GetProperty("roomId").GetString());
    }

    [Fact]
    public async Task Inventory_list_filters_by_home_and_room_and_paginates()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var inRoom = await CreateInventoryAsync(Client, home, room);
        await CreateInventoryAsync(Client, home, name: "Unassigned item");
        var otherHome = await CreateHomeAsync(Client, "Other home");
        await CreateInventoryAsync(Client, otherHome, name: "Other item");

        var byRoom = await ReadAsync(await Client.GetAsync($"{Api}/inventory?homeId={home}&roomId={room}"));
        Assert.Equal(inRoom, Assert.Single(byRoom.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        var byHome = await ReadAsync(await Client.GetAsync($"{Api}/inventory?homeId={home}&pageSize=1"));
        Assert.Single(byHome.GetProperty("items").EnumerateArray());
        Assert.Equal(2, byHome.GetProperty("totalItemCount").GetInt32());
        Assert.True(byHome.GetProperty("hasNextPage").GetBoolean());
        var mismatched = await ReadAsync(await Client.GetAsync($"{Api}/inventory?homeId={otherHome}&roomId={room}"));
        Assert.Empty(mismatched.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Other_users_cannot_list_read_update_or_delete_inventory()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var item = await CreateInventoryAsync(Client, home);
        using var other = Factory.CreateApiClient();
        await AuthenticateAsync(other);

        var hidden = await ReadAsync(await other.GetAsync($"{Api}/inventory?homeId={home}"));
        Assert.Empty(hidden.GetProperty("items").EnumerateArray());
        await ExpectAsync(await other.GetAsync($"{Api}/inventory/{item}"), HttpStatusCode.NotFound);
        await ExpectAsync(await other.PutAsJsonAsync($"{Api}/inventory/{item}", new { name = "Stolen" }), HttpStatusCode.NotFound);
        await ExpectAsync(await other.DeleteAsync($"{Api}/inventory/{item}"), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal("Coffee maker", original.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Inventory_cannot_be_created_or_moved_into_another_users_home()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var item = await CreateInventoryAsync(Client, home);
        using var other = Factory.CreateApiClient();
        await AuthenticateAsync(other);
        var otherHome = await CreateHomeAsync(other);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/inventory", new { name = "Invalid", homeId = otherHome }), HttpStatusCode.NotFound);
        await ExpectAsync(await Client.PutAsJsonAsync($"{Api}/inventory/{item}", new { name = "Invalid", homeId = otherHome }), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal(home, original.GetProperty("homeId").GetGuid());
        Assert.Equal("Coffee maker", original.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Inventory_room_must_belong_to_the_selected_home_on_create_and_update()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var item = await CreateInventoryAsync(Client, home);
        var otherHome = await CreateHomeAsync(Client, "Other home");
        var otherRoom = await CreateRoomAsync(Client, otherHome);

        await ExpectAsync(await Client.PostAsJsonAsync($"{Api}/inventory", new { name = "Invalid", homeId = home, roomId = otherRoom }), HttpStatusCode.NotFound);
        await ExpectAsync(await Client.PutAsJsonAsync($"{Api}/inventory/{item}", new { name = "Invalid", roomId = otherRoom }), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Null(original.GetProperty("roomId").GetString());
        Assert.Equal("Coffee maker", original.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Invalid_inventory_values_are_rejected_on_create_and_update()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var item = await CreateInventoryAsync(Client, home);
        var body = new { name = "", homeId = Guid.Empty, price = -1, description = new string('a', 501), serialNumber = new string('a', 51) };

        var createProblem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/inventory", body), HttpStatusCode.BadRequest);
        var updateProblem = await ReadAsync(await Client.PutAsJsonAsync($"{Api}/inventory/{item}", body), HttpStatusCode.BadRequest);

        foreach (var field in new[] { "Name", "Price", "Description", "SerialNumber" })
        {
            Assert.True(createProblem.GetProperty("errors").TryGetProperty(field, out _));
            Assert.True(updateProblem.GetProperty("errors").TryGetProperty(field, out _));
        }
        Assert.True(createProblem.GetProperty("errors").TryGetProperty("HomeId", out _));
        var original = await ReadAsync(await Client.GetAsync($"{Api}/inventory/{item}"));
        Assert.Equal("Coffee maker", original.GetProperty("name").GetString());
        var all = await ReadAsync(await Client.GetAsync($"{Api}/inventory"));
        Assert.Single(all.GetProperty("items").EnumerateArray());
    }
}
