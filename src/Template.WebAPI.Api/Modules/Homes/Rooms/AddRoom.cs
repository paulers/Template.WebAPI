using System.ComponentModel.DataAnnotations;

namespace Template.WebAPI.Api.Modules.Homes.Rooms;

public class AddRoom : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/{homeId}/rooms", Handle)
            .WithSummary("Adds a new room to a home")
            .WithRequestValidation<Request>();
    }
    
    public record Request(
        [property: Required(AllowEmptyStrings = false), MaxLength(255)]string Name);

    public record Response(
        Guid Id,
        Guid HomeId,
        string Name);
    
    private static async Task<Results<Created<Response>, NotFound>> Handle(
        Guid homeId,
        Request request, 
        AppDbContext db, 
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        // Verify the user owns the home they're trying to add a room to
        var userId = claimsPrincipal.GetUserId();
        if (!await db.Homes.AnyAsync(h => h.Id == homeId && h.UserId == userId, cancellationToken))
            return TypedResults.NotFound();

        var room = new Room
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            HomeId = homeId,
            UserId = userId
        };
        
        await db.Rooms.AddAsync(room, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        
        //TODO: send an event out
        
        var response = new Response(room.Id, room.HomeId, room.Name);
        return TypedResults.Created(response.Id.ToString(), response);
    }
}
