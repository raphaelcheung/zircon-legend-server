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
        private static IResult SaveConfig(SaveConfigRequest request, ClaimsPrincipal user, HttpContext context, ConfigService configService)
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
                WebApiLogger.Audit(context, "保存配置文件", null, $"内容长度={request.Content.Length}");
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
        private static IResult UpdateConfigValue(UpdateConfigValueRequest request, ClaimsPrincipal user, HttpContext context, ConfigService configService)
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
                WebApiLogger.Audit(context, "修改配置项", $"{request.Section ?? ""}/{request.Key}", $"值={request.Value ?? ""}");
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
        private static IResult UpdateRuntimeConfig(UpdateRuntimeConfigRequest request, ClaimsPrincipal user, HttpContext context, ConfigService configService)
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
                    {
                        var boolValue = request.Value?.ToLower() == "true";
                        Config.OnlyAdminLogin = boolValue;
                        configService.UpdateConfigValue("Control", "OnlyAdminLogin", boolValue.ToString());
                        WebApiLogger.Audit(context, "修改运行时配置", request.Key, $"值={Config.OnlyAdminLogin}");
                        return Results.Ok(new { message = $"OnlyAdminLogin set to {Config.OnlyAdminLogin}" });
                    }
                case "webhookenabled":
                    {
                        var boolVal = request.Value?.ToLower() == "true";
                        Config.WebHookEnabled = boolVal;
                        configService.UpdateConfigValue("WebHook", "WebHookEnabled", boolVal.ToString());
                        WebApiLogger.Audit(context, "修改运行时配置", request.Key, $"值={Config.WebHookEnabled}");
                        return Results.Ok(new { message = $"WebHookEnabled set to {Config.WebHookEnabled}" });
                    }
                case "webhookurl":
                    {
                        var strVal = request.Value ?? "";
                        Config.WebHookUrl = strVal;
                        configService.UpdateConfigValue("WebHook", "WebHookUrl", strVal);
                        WebApiLogger.Audit(context, "修改运行时配置", request.Key, $"值={strVal}");
                        return Results.Ok(new { message = $"WebHookUrl set to {strVal}" });
                    }
                case "webhookintervalminutes":
                    {
                        if (!int.TryParse(request.Value, out var intVal))
                            return Results.BadRequest(new { message = "Invalid integer value" });
                        Config.WebHookIntervalMinutes = intVal;
                        configService.UpdateConfigValue("WebHook", "WebHookIntervalMinutes", intVal.ToString());
                        WebApiLogger.Audit(context, "修改运行时配置", request.Key, $"值={intVal}");
                        return Results.Ok(new { message = $"WebHookIntervalMinutes set to {intVal}" });
                    }
                case "webhookhighthreshold":
                    {
                        if (!int.TryParse(request.Value, out var intVal))
                            return Results.BadRequest(new { message = "Invalid integer value" });
                        Config.WebHookHighThreshold = intVal;
                        configService.UpdateConfigValue("WebHook", "WebHookHighThreshold", intVal.ToString());
                        WebApiLogger.Audit(context, "修改运行时配置", request.Key, $"值={intVal}");
                        return Results.Ok(new { message = $"WebHookHighThreshold set to {intVal}" });
                    }
                case "webhooklowthreshold":
                    {
                        if (!int.TryParse(request.Value, out var intVal))
                            return Results.BadRequest(new { message = "Invalid integer value" });
                        Config.WebHookLowThreshold = intVal;
                        configService.UpdateConfigValue("WebHook", "WebHookLowThreshold", intVal.ToString());
                        WebApiLogger.Audit(context, "修改运行时配置", request.Key, $"值={intVal}");
                        return Results.Ok(new { message = $"WebHookLowThreshold set to {intVal}" });
                    }
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
