using System.Net;
using System.Net.Http.Json;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class HomeTests : ApiTest
{
    [Fact]
    public async Task Owner_can_create_read_update_and_delete_a_home()
    {
        await AuthenticateAsync();
        var id = await CreateHomeAsync(Client, "First home");
        var created = await ReadAsync(await Client.GetAsync($"{Api}/homes/{id}"));
        Assert.Equal("First home", created.GetProperty("name").GetString());

        await ExpectAsync(await Client.PutAsJsonAsync($"{Api}/homes/{id}", new
        {
            name = "Updated home", streetAddress = "123 Main St", addressLine2 = "Unit 2",
            city = "Seattle", stateProvince = "WA", country = "USA", postalCode = "98101"
        }), HttpStatusCode.OK);

        var updated = await ReadAsync(await Client.GetAsync($"{Api}/homes/{id}"));
        Assert.Equal("Updated home", updated.GetProperty("name").GetString());
        Assert.Equal("123 Main St", updated.GetProperty("streetAddress").GetString());
        Assert.Equal("Unit 2", updated.GetProperty("addressLine2").GetString());
        Assert.Equal("Seattle", updated.GetProperty("city").GetString());
        Assert.Equal("WA", updated.GetProperty("stateProvince").GetString());
        Assert.Equal("USA", updated.GetProperty("country").GetString());
        Assert.Equal("98101", updated.GetProperty("postalCode").GetString());
        var listed = await ReadAsync(await Client.GetAsync($"{Api}/homes"));
        Assert.Equal(id, Assert.Single(listed.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        await ExpectAsync(await Client.DeleteAsync($"{Api}/homes/{id}"), HttpStatusCode.NoContent);
        await ExpectAsync(await Client.GetAsync($"{Api}/homes/{id}"), HttpStatusCode.NotFound);
        var remaining = await ReadAsync(await Client.GetAsync($"{Api}/homes"));
        Assert.Empty(remaining.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Homes_are_sorted_and_paginated()
    {
        await AuthenticateAsync();
        await CreateHomeAsync(Client, "Charlie");
        await CreateHomeAsync(Client, "Alpha");
        await CreateHomeAsync(Client, "Bravo");

        var page = await ReadAsync(await Client.GetAsync($"{Api}/homes?page=2&pageSize=1"));

        Assert.Equal("Bravo", Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
        Assert.Equal(2, page.GetProperty("page").GetInt32());
        Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, page.GetProperty("totalItemCount").GetInt32());
        Assert.True(page.GetProperty("hasPreviousPage").GetBoolean());
        Assert.True(page.GetProperty("hasNextPage").GetBoolean());
    }

    [Fact]
    public async Task Other_users_cannot_list_read_update_or_delete_a_home()
    {
        await AuthenticateAsync();
        var id = await CreateHomeAsync(Client, "Private home");
        using var other = Factory.CreateApiClient();
        await AuthenticateAsync(other);

        var list = await ReadAsync(await other.GetAsync($"{Api}/homes"));
        Assert.Empty(list.GetProperty("items").EnumerateArray());
        await ExpectAsync(await other.GetAsync($"{Api}/homes/{id}"), HttpStatusCode.NotFound);
        await ExpectAsync(await other.PutAsJsonAsync($"{Api}/homes/{id}", new { name = "Stolen" }), HttpStatusCode.NotFound);
        await ExpectAsync(await other.DeleteAsync($"{Api}/homes/{id}"), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/homes/{id}"));
        Assert.Equal("Private home", original.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(256)]
    public async Task Invalid_home_names_are_rejected_without_saving(int length)
    {
        await AuthenticateAsync();

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/homes", new { name = new string('a', length) }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("Name", out _));
        var list = await ReadAsync(await Client.GetAsync($"{Api}/homes"));
        Assert.Empty(list.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Invalid_home_update_preserves_existing_values()
    {
        await AuthenticateAsync();
        var id = await CreateHomeAsync(Client, "Original");

        var problem = await ReadAsync(await Client.PutAsJsonAsync($"{Api}/homes/{id}", new { name = "Changed", postalCode = new string('1', 21) }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("PostalCode", out _));
        var home = await ReadAsync(await Client.GetAsync($"{Api}/homes/{id}"));
        Assert.Equal("Original", home.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Deleting_a_home_also_removes_its_rooms_and_inventory()
    {
        await AuthenticateAsync();
        var home = await CreateHomeAsync(Client);
        var room = await CreateRoomAsync(Client, home);
        var item = await CreateInventoryAsync(Client, home, room);

        await ExpectAsync(await Client.DeleteAsync($"{Api}/homes/{home}"), HttpStatusCode.NoContent);

        await ExpectAsync(await Client.GetAsync($"{Api}/inventory/{item}"), HttpStatusCode.NotFound);
        var rooms = await ReadAsync(await Client.GetAsync($"{Api}/homes/{home}/rooms"));
        Assert.Empty(rooms.GetProperty("items").EnumerateArray());
        var inventory = await ReadAsync(await Client.GetAsync($"{Api}/inventory"));
        Assert.Empty(inventory.GetProperty("items").EnumerateArray());
    }
}
