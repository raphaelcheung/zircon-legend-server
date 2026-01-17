using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Player management API endpoints
    /// </summary>
    public static class PlayerEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/players")
                .RequireAuthorization();

            group.MapGet("/online", GetOnlinePlayers);
            group.MapPost("/{name}/kick", KickPlayer);
        }

        /// <summary>
        /// Get online players list with optional search
        /// </summary>
        private static IResult GetOnlinePlayers(ClaimsPrincipal user, ServerDataService dataService, string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var players = dataService.GetOnlinePlayers();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                players = players.Where(p =>
                    (p.CharacterName?.ToLower().Contains(searchLower) ?? false) ||
                    (p.AccountEmail?.ToLower().Contains(searchLower) ?? false) ||
                    (p.Class?.ToLower().Contains(searchLower) ?? false) ||
                    GetClassNameChinese(p.Class).Contains(searchLower)
                ).ToList();
            }

            return Results.Ok(new
            {
                total = players.Count,
                players
            });
        }

        /// <summary>
        /// Get Chinese class name for search matching
        /// </summary>
        private static string GetClassNameChinese(string? classEn)
        {
            return classEn switch
            {
                "Warrior" => "战士",
                "Wizard" => "法师",
                "Taoist" => "道士",
                "Assassin" => "刺客",
                _ => ""
            };
        }

        /// <summary>
        /// Kick a player by character name
        /// </summary>
        private static IResult KickPlayer(string name, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            var success = dataService.KickPlayer(name);
            if (success)
            {
                return Results.Ok(new { message = $"Player '{name}' has been kicked" });
            }

            return Results.NotFound(new { message = $"Player '{name}' not found or not online" });
        }
    }
}
