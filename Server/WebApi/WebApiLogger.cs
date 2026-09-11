using System.Security.Claims;
using Server.Envir;
using Server.WebApi.Auth;

namespace Server.WebApi
{
    /// <summary>
    /// WebApi 审计日志辅助类，统一记录关键操作的操作者、来源 IP 和操作内容。
    /// 所有日志通过 SEnvir.Log 输出，与游戏主日志合并显示。
    /// </summary>
    public static class WebApiLogger
    {
        /// <summary>
        /// 从 HttpContext 提取客户端 IP，优先取 X-Forwarded-For（反向代理场景）。
        /// </summary>
        public static string GetClientIp(HttpContext? context)
        {
            if (context == null) return "unknown";

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 记录审计日志（已登录场景），自动从 HttpContext 提取操作者和 IP。
        /// 用于：封禁、改配置、发物品等已认证的写操作。
        /// </summary>
        public static void Audit(HttpContext context, string action, string? target = null, string? detail = null)
        {
            var email = JwtHelper.GetEmail(context.User) ?? "anonymous";
            var ip = GetClientIp(context);
            Write("[WebApi审计]", email, ip, action, target, detail);
        }

        /// <summary>
        /// 记录审计日志（登录前场景，手动指定操作者和 IP）。
        /// 用于：登录成功（此时 ClaimsPrincipal 尚未建立）、初始化超管等。
        /// </summary>
        public static void Audit(string? email, string? ip, string action, string? target = null, string? detail = null)
        {
            Write("[WebApi审计]", email, ip, action, target, detail);
        }

        /// <summary>
        /// 记录安全事件日志（登录失败、权限拒绝、敏感操作被拒等）。
        /// </summary>
        public static void Security(string? email, string? ip, string action, string? detail = null)
        {
            Write("[WebApi安全]", email, ip, action, null, detail);
        }

        private static void Write(string tag, string? email, string? ip, string action, string? target, string? detail)
        {
            var msg = $"{tag} 操作者={email ?? "anonymous"} IP={ip ?? "unknown"} 操作={action}";
            if (!string.IsNullOrEmpty(target))
                msg += $" 目标={target}";
            if (!string.IsNullOrEmpty(detail))
                msg += $" 详情={detail}";
            SEnvir.Log(msg);
        }
    }
}