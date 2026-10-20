using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.OutputCaching;

namespace Template.WebAPI.Api.Modules.Pros;

public class AddPro : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", Handle)
            .WithSummary("Adds a new professional")
            .WithRequestValidation<Request>();
    }
    
    public record Request(
        [property: Required(AllowEmptyStrings = false), MaxLength(255)] string Name,
        [property: Required, NotEmptyGuid] Guid ProfessionalTypeId
        );

    public record Response(
        Guid Id,
        string Name,
        ProfessionalType? ProfessionalType);
    
    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        Request request, 
        AppDbContext db, 
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        // Ensure the pro type being added actually exists.
        var proType = await db.ProfessionalTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == request.ProfessionalTypeId, cancellationToken);
        if (proType is null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(Request.ProfessionalTypeId)] = ["The selected professional type does not exist."]
            });

        var pro = new Professional
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            UserId = claimsPrincipal.GetUserId(),
            ProfessionalTypeId = request.ProfessionalTypeId
        };
        
        await db.Professionals.AddAsync(pro, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        
        //TODO: send an event out
        var response = new Response(pro.Id, pro.Name, proType);
        return TypedResults.Created(response.Id.ToString(), response);
    }
}
