using Library;
using Server.Envir;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Globalization;
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
            group.MapGet("/dates", GetLogDates);
        }

        /// <summary>
        /// Get system logs. Without "date" the in-memory buffer is returned;
        /// with "date=yyyy-MM-dd" the matching daily file is read.
        /// </summary>
        private static IResult GetSystemLogs(ClaimsPrincipal user, ServerDataService dataService,
            int count = 100, string? date = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (count < 1 || count > 500) count = 100;

            if (!string.IsNullOrWhiteSpace(date))
            {
                if (!TryParseDate(date, out var parsed))
                    return Results.BadRequest(new { message = "日期格式应为 yyyy-MM-dd" });

                var history = dataService.GetSystemLogsByDate(parsed, count);

                return Results.Ok(new
                {
                    total = history.Count,
                    logs = history,
                    date = parsed.ToString("yyyy-MM-dd"),
                    fromFile = true
                });
            }

            var logs = dataService.GetSystemLogs(count);
            return Results.Ok(new
            {
                total = logs.Count,
                logs,
                fromFile = false,
                writeError = SEnvir.LastLogError
            });
        }

        /// <summary>
        /// Get chat logs. Without "date" the in-memory buffer is returned;
        /// with "date=yyyy-MM-dd" the matching daily file is read.
        /// </summary>
        private static IResult GetChatLogs(ClaimsPrincipal user, ServerDataService dataService,
            int count = 100, string? date = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (count < 1 || count > 500) count = 100;

            if (!string.IsNullOrWhiteSpace(date))
            {
                if (!TryParseDate(date, out var parsed))
                    return Results.BadRequest(new { message = "日期格式应为 yyyy-MM-dd" });

                var history = dataService.GetChatLogsByDate(parsed, count);

                return Results.Ok(new
                {
                    total = history.Count,
                    logs = history,
                    date = parsed.ToString("yyyy-MM-dd"),
                    fromFile = true
                });
            }

            var logs = dataService.GetChatLogs(count);
            return Results.Ok(new
            {
                total = logs.Count,
                logs,
                fromFile = false,
                writeError = SEnvir.LastLogError
            });
        }

        /// <summary>
        /// List dates that have log files on disk
        /// </summary>
        private static IResult GetLogDates(ClaimsPrincipal user, ServerDataService dataService, string type = "system")
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var dates = string.Equals(type, "chat", StringComparison.OrdinalIgnoreCase)
                ? dataService.GetChatLogDates()
                : dataService.GetSystemLogDates();

            return Results.Ok(new
            {
                total = dates.Count,
                dates
            });
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);
        }
    }
}
