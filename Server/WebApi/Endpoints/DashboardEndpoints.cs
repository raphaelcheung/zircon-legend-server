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
            group.MapPost("/save-users", SaveUserDatas);
            group.MapPost("/save-system", SaveSystem);
            group.MapPost("/kick-all", KickAllPlayers);
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

        /// <summary>
        /// Save user data to database
        /// </summary>
        private static IResult SaveUserDatas(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            try
            {
                dataService.SaveUserDatas();
                return Results.Ok(new { message = "用户数据保存成功" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"保存失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// Save system data to database
        /// </summary>
        private static IResult SaveSystem(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            try
            {
                dataService.SaveSystem();
                return Results.Ok(new { message = "系统数据保存成功" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"保存失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// Kick all online players
        /// </summary>
        private static IResult KickAllPlayers(ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            try
            {
                int count = dataService.KickAllPlayers();
                return Results.Ok(new { message = $"已踢出 {count} 个在线玩家" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = $"操作失败: {ex.Message}" });
            }
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
