using System.Net;
using System.Net.Http.Json;
using Template.WebAPI.IntegrationTests.Infrastructure;

namespace Template.WebAPI.IntegrationTests;

public class ProfessionalTests : ApiTest
{
    private async Task<Guid> CreateProfessionalAsync()
    {
        var types = await ReadAsync(await Client.GetAsync($"{Api}/pros/types"));
        var plumber = types.EnumerateArray().Single(type => type.GetProperty("name").GetString() == "Plumber");
        var pro = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/pros", new
        {
            name = "Pat Plumber", professionalTypeId = plumber.GetProperty("id").GetGuid()
        }), HttpStatusCode.Created);
        Assert.Equal("Plumber", pro.GetProperty("professionalType").GetProperty("name").GetString());
        return pro.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Seeded_professional_types_are_available()
    {
        await AuthenticateAsync();

        var types = await ReadAsync(await Client.GetAsync($"{Api}/pros/types"));

        Assert.Equal(new[] { "Electrician", "General contractor", "HVAC technician", "Plumber", "Roofer" },
            types.EnumerateArray().Select(type => type.GetProperty("name").GetString()).Order(StringComparer.Ordinal));
        Assert.Equal(5, types.EnumerateArray().Select(type => type.GetProperty("id").GetGuid()).Distinct().Count());
        Assert.All(types.EnumerateArray(), type => Assert.NotEqual(Guid.Empty, type.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Owner_can_create_list_update_and_delete_a_professional()
    {
        await AuthenticateAsync();
        var id = await CreateProfessionalAsync();
        var created = await ReadAsync(await Client.GetAsync($"{Api}/pros"));
        Assert.Equal("Pat Plumber", Assert.Single(created.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());

        var updated = await ReadAsync(await Client.PutAsJsonAsync($"{Api}/pros/{id}", new
        {
            name = "Pat Smith", businessName = "Plumbing Co", businessPhoneNumber = "555-0100", personalPhoneNumber = "555-0101",
            emailAddress = "pat@example.com", postalCode = "98101", city = "Seattle", country = "USA", stateProvince = "WA",
            streetAddress = "123 Main St", websiteUrl = "https://example.com"
        }));
        Assert.Equal(id, updated.GetProperty("id").GetGuid());
        var persisted = await ReadAsync(await Client.GetAsync($"{Api}/pros"));
        var pro = Assert.Single(persisted.GetProperty("items").EnumerateArray());
        Assert.Equal("Pat Smith", pro.GetProperty("name").GetString());
        Assert.Equal("Plumbing Co", pro.GetProperty("businessName").GetString());
        Assert.Equal("555-0100", pro.GetProperty("businessPhoneNumber").GetString());
        Assert.Equal("555-0101", pro.GetProperty("personalPhoneNumber").GetString());
        Assert.Equal("pat@example.com", pro.GetProperty("emailAddress").GetString());
        Assert.Equal("98101", pro.GetProperty("postalCode").GetString());
        Assert.Equal("Seattle", pro.GetProperty("city").GetString());
        Assert.Equal("USA", pro.GetProperty("country").GetString());
        Assert.Equal("WA", pro.GetProperty("stateProvince").GetString());
        Assert.Equal("123 Main St", pro.GetProperty("streetAddress").GetString());
        Assert.Equal("https://example.com", pro.GetProperty("websiteUrl").GetString());
        Assert.Equal("Plumber", pro.GetProperty("type").GetProperty("name").GetString());

        await ExpectAsync(await Client.DeleteAsync($"{Api}/pros/{id}"), HttpStatusCode.NoContent);
        var remaining = await ReadAsync(await Client.GetAsync($"{Api}/pros"));
        Assert.Empty(remaining.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Other_users_cannot_list_update_or_delete_a_professional()
    {
        await AuthenticateAsync();
        var id = await CreateProfessionalAsync();
        using var other = Factory.CreateApiClient();
        await AuthenticateAsync(other);

        var hidden = await ReadAsync(await other.GetAsync($"{Api}/pros"));
        Assert.Empty(hidden.GetProperty("items").EnumerateArray());
        await ExpectAsync(await other.PutAsJsonAsync($"{Api}/pros/{id}", new { name = "Stolen" }), HttpStatusCode.NotFound);
        await ExpectAsync(await other.DeleteAsync($"{Api}/pros/{id}"), HttpStatusCode.NotFound);

        var original = await ReadAsync(await Client.GetAsync($"{Api}/pros"));
        Assert.Equal("Pat Plumber", Assert.Single(original.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Unknown_professional_type_is_rejected_without_saving()
    {
        await AuthenticateAsync();

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/pros", new
        {
            name = "Pat Plumber", professionalTypeId = Guid.NewGuid()
        }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("ProfessionalTypeId", out _));
        var all = await ReadAsync(await Client.GetAsync($"{Api}/pros"));
        Assert.Empty(all.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Empty_professional_name_and_type_are_rejected()
    {
        await AuthenticateAsync();

        var problem = await ReadAsync(await Client.PostAsJsonAsync($"{Api}/pros", new
        {
            name = "", professionalTypeId = Guid.Empty
        }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("Name", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("ProfessionalTypeId", out _));
    }

    [Fact]
    public async Task Invalid_professional_update_preserves_existing_values()
    {
        await AuthenticateAsync();
        var id = await CreateProfessionalAsync();

        var problem = await ReadAsync(await Client.PutAsJsonAsync($"{Api}/pros/{id}", new
        {
            name = "Changed", businessPhoneNumber = new string('1', 51), postalCode = new string('1', 21)
        }), HttpStatusCode.BadRequest);

        Assert.True(problem.GetProperty("errors").TryGetProperty("BusinessPhoneNumber", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("PostalCode", out _));
        var original = await ReadAsync(await Client.GetAsync($"{Api}/pros"));
        Assert.Equal("Pat Plumber", Assert.Single(original.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Professional_list_is_paginated()
    {
        await AuthenticateAsync();
        await CreateProfessionalAsync();
        await CreateProfessionalAsync();

        var page = await ReadAsync(await Client.GetAsync($"{Api}/pros?page=2&pageSize=1"));

        Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(2, page.GetProperty("totalItemCount").GetInt32());
        Assert.True(page.GetProperty("hasPreviousPage").GetBoolean());
        Assert.False(page.GetProperty("hasNextPage").GetBoolean());
    }
}
