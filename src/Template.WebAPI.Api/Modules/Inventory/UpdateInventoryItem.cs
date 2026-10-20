using System.ComponentModel.DataAnnotations;

namespace Template.WebAPI.Api.Modules.Inventory;

public class UpdateInventoryItem : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id}", Handle)
            .WithSummary("Updates an inventory item")
            .WithRequestValidation<Request>();
    }
    
    public record Request(
        Guid? HomeId,
        Guid? RoomId,
        [property: MaxLength(50)] string? SerialNumber,
        [property: Required(AllowEmptyStrings = false), MaxLength(255)] string Name,
        [property: MaxLength(500)] string? Description,
        [property: Range(0d, int.MaxValue)] double? Price);
    
    public record Response(
        Guid Id,
        Guid HomeId,
        Guid? RoomId,
        string? SerialNumber,
        string Name,
        string? Description,
        double? Price);
    
    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id,
        Request request, 
        AppDbContext db, 
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var item = await db.InventoryItems
            .SingleOrDefaultAsync(
                i => i.Id == id 
                     && i.UserId == userId,
                cancellationToken);
        if (item is null) return TypedResults.NotFound();
        // ensure the user owns the home and room
        var homeId = request.HomeId ?? item.HomeId;
        if (!await db.Homes.AnyAsync(h => h.Id == homeId && h.UserId == userId, cancellationToken))
            return TypedResults.NotFound();

        if (request.RoomId is { } roomId && !await db.Rooms.AnyAsync(
                r => r.Id == roomId && r.HomeId == homeId && r.UserId == userId, cancellationToken))
            return TypedResults.NotFound();
        
        item.HomeId = homeId;
        item.RoomId = request.RoomId;
        item.SerialNumber = request.SerialNumber;
        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        
        await db.SaveChangesAsync(cancellationToken);
        
        //TODO: send an event out
        
        var response = new Response(
            item.Id,
            item.HomeId,
            item.RoomId,
            item.SerialNumber,
            item.Name,
            item.Description,
            item.Price);
        
        return TypedResults.Ok(response);
    }
}
