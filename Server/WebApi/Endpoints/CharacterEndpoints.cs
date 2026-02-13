using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Character management API endpoints
    /// </summary>
    public static class CharacterEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/characters")
                .RequireAuthorization();

            group.MapGet("/", GetCharacters);
            group.MapGet("/{name}", GetCharacterDetail);
            group.MapPut("/{name}/level", SetLevel);
        }

        /// <summary>
        /// Get characters list with pagination
        /// </summary>
        private static IResult GetCharacters(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int page = 1,
            int pageSize = 20,
            string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (characters, total) = dataService.GetCharacters(page, pageSize, search);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                characters
            });
        }

        /// <summary>
        /// Get character details
        /// </summary>
        private static IResult GetCharacterDetail(string name, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var character = dataService.GetCharacterDetail(name);
            if (character == null)
            {
                return Results.NotFound(new { message = "Character not found" });
            }

            return Results.Ok(character);
        }

        /// <summary>
        /// Set character level
        /// </summary>
        private static IResult SetLevel(string name, SetLevelRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            if (request.Level < 1 || request.Level > 500)
            {
                return Results.BadRequest(new { message = "Level must be between 1 and 500" });
            }

            var success = dataService.SetCharacterLevel(name, request.Level);
            if (success)
            {
                return Results.Ok(new { message = $"Character level set to {request.Level}" });
            }

            return Results.NotFound(new { message = "Character not found" });
        }
    }

    public class SetLevelRequest
    {
        public int Level { get; set; }
    }
}
