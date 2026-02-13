using Library;
using Server.Envir;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Configuration management API endpoints
    /// </summary>
    public static class ConfigEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/config")
                .RequireAuthorization();

            group.MapGet("/", GetConfig);
            group.MapPut("/", SaveConfig);
            group.MapGet("/sections", GetConfigSections);
            group.MapPut("/value", UpdateConfigValue);

            // 运行时配置 API
            group.MapGet("/runtime", GetRuntimeConfig);
            group.MapPut("/runtime", UpdateRuntimeConfig);
        }

        /// <summary>
        /// Get Server.ini content
        /// </summary>
        private static IResult GetConfig(ClaimsPrincipal user, ConfigService configService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            var content = configService.GetConfigContent();
            return Results.Ok(new { content });
        }

        /// <summary>
        /// Save Server.ini content
        /// </summary>
        private static IResult SaveConfig(SaveConfigRequest request, ClaimsPrincipal user, ConfigService configService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrEmpty(request.Content))
            {
                return Results.BadRequest(new { message = "Content is required" });
            }

            var (success, message) = configService.SaveConfigContent(request.Content);

            if (success)
            {
                return Results.Ok(new { message });
            }

            return Results.Problem(message);
        }

        /// <summary>
        /// Get configuration as sections
        /// </summary>
        private static IResult GetConfigSections(ClaimsPrincipal user, ConfigService configService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            var sections = configService.GetConfigSections();
            return Results.Ok(new { sections });
        }

        /// <summary>
        /// Update a specific configuration value
        /// </summary>
        private static IResult UpdateConfigValue(UpdateConfigValueRequest request, ClaimsPrincipal user, ConfigService configService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrEmpty(request.Key))
            {
                return Results.BadRequest(new { message = "Key is required" });
            }

            var (success, message) = configService.UpdateConfigValue(request.Section ?? "", request.Key, request.Value ?? "");

            if (success)
            {
                return Results.Ok(new { message });
            }

            return Results.BadRequest(new { message });
        }

        /// <summary>
        /// Get runtime configuration values
        /// </summary>
        private static IResult GetRuntimeConfig(ClaimsPrincipal user)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            return Results.Ok(new
            {
                onlyAdminLogin = Config.OnlyAdminLogin
            });
        }

        /// <summary>
        /// Update runtime configuration value (updates both memory and INI file)
        /// </summary>
        private static IResult UpdateRuntimeConfig(UpdateRuntimeConfigRequest request, ClaimsPrincipal user, ConfigService configService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.SuperAdmin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrEmpty(request.Key))
            {
                return Results.BadRequest(new { message = "Key is required" });
            }

            switch (request.Key.ToLower())
            {
                case "onlyadminlogin":
                    var boolValue = request.Value?.ToLower() == "true";
                    Config.OnlyAdminLogin = boolValue;
                    // 同时保存到 INI 文件
                    configService.UpdateConfigValue("Control", "OnlyAdminLogin", boolValue.ToString());
                    return Results.Ok(new { message = $"OnlyAdminLogin set to {Config.OnlyAdminLogin}" });
                default:
                    return Results.BadRequest(new { message = $"Unknown runtime config key: {request.Key}" });
            }
        }
    }

    #region Request Models

    public class SaveConfigRequest
    {
        public string Content { get; set; } = "";
    }

    public class UpdateConfigValueRequest
    {
        public string? Section { get; set; }
        public string Key { get; set; } = "";
        public string? Value { get; set; }
    }

    public class UpdateRuntimeConfigRequest
    {
        public string Key { get; set; } = "";
        public string? Value { get; set; }
    }

    #endregion
}
