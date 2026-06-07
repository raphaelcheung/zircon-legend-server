using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Log viewing API endpoints
    /// </summary>
    public static class LogEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/logs")
                .RequireAuthorization();

            group.MapGet("/system", GetSystemLogs);
            group.MapGet("/chat", GetChatLogs);
        }

        /// <summary>
        /// Get system logs
        /// </summary>
        private static IResult GetSystemLogs(ClaimsPrincipal user, ServerDataService dataService, int count = 100, string level = "all", string search = "")
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (count < 1 || count > 1000) count = 100;
            if (string.IsNullOrEmpty(level)) level = "all";
            if (search == null) search = "";

            var logs = dataService.GetSystemLogs(count, level, search);
            return Results.Ok(new
            {
                total = logs.Count,
                logs
            });
        }

        /// <summary>
        /// Get chat logs
        /// </summary>
        private static IResult GetChatLogs(ClaimsPrincipal user, ServerDataService dataService, int count = 100, string search = "")
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (count < 1 || count > 1000) count = 100;
            if (search == null) search = "";

            var logs = dataService.GetChatLogs(count, search);
            return Results.Ok(new
            {
                total = logs.Count,
                logs
            });
        }
    }
}
