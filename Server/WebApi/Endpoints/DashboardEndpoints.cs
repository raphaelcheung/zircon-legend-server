using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Dashboard API endpoints
    /// </summary>
    public static class DashboardEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/dashboard")
                .RequireAuthorization();

            group.MapGet("/stats", GetStats);
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        private static IResult GetStats(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var uptime = dataService.GetUptime();
            var onlinePlayers = dataService.GetOnlinePlayers();

            return Results.Ok(new DashboardStats
            {
                OnlinePlayers = dataService.GetOnlinePlayerCount(),
                TotalAccounts = dataService.GetTotalAccountCount(),
                TotalCharacters = dataService.GetTotalCharacterCount(),
                Started = dataService.IsServerRunning(),
                UptimeSeconds = (long)uptime.TotalSeconds,
                Uptime = FormatUptime(uptime),
                RecentPlayers = onlinePlayers.Take(5).Select(p => new RecentPlayerDto
                {
                    Name = p.CharacterName,
                    Level = p.Level,
                    Class = p.Class,
                    Map = p.MapName
                }).ToList()
            });
        }

        private static string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalDays >= 1)
            {
                return $"{(int)uptime.TotalDays}天 {uptime.Hours}小时 {uptime.Minutes}分钟";
            }
            if (uptime.TotalHours >= 1)
            {
                return $"{(int)uptime.TotalHours}小时 {uptime.Minutes}分钟";
            }
            return $"{uptime.Minutes}分钟 {uptime.Seconds}秒";
        }
    }

    public class DashboardStats
    {
        public int OnlinePlayers { get; set; }
        public int TotalAccounts { get; set; }
        public int TotalCharacters { get; set; }
        public bool Started { get; set; }
        public long UptimeSeconds { get; set; }
        public string Uptime { get; set; } = "";
        public List<RecentPlayerDto> RecentPlayers { get; set; } = new();
    }

    public class RecentPlayerDto
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string Class { get; set; } = "";
        public string Map { get; set; } = "";
    }
}
