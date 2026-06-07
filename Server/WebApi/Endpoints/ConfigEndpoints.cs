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
                // [后台修改系统配置] 详细记录管理员修改整个配置文件并保存的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台修改系统配置] 管理员={adminEmail}, 修改了完整的系统配置文件并已保存生效");

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

            // 【新增审计】在修改配置发生前，读取 Server.ini 中当前保存的旧值
            string oldValue = "未设定";
            var currentSections = configService.GetConfigSections();
            if (currentSections.TryGetValue(request.Section ?? "", out var keys) && keys.TryGetValue(request.Key, out var existingVal))
            {
                oldValue = existingVal;
            }

            var (success, message) = configService.UpdateConfigValue(request.Section ?? "", request.Key, request.Value ?? "");

            if (success)
            {
                // [后台更新单项配置] 详细记录管理员修改特定配置项的操作日志，实现完美的“旧值 -> 新值”审计可追溯
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台更新单项配置] 管理员={adminEmail}, 区域={request.Section ?? "默认"}, 配置项={request.Key}, 变更对比: 【{oldValue}】 -> 【{request.Value ?? "空"}】");

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

                    // [后台更新运行参数] 详细记录管理员修改运行时策略的操作日志
                    var adminEmail = JwtHelper.GetEmail(user);
                    Server.Envir.SEnvir.Log($"[后台更新运行参数] 管理员={adminEmail}, 配置项=OnlyAdminLogin (是否仅管理员登录), 新值={Config.OnlyAdminLogin}");

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
