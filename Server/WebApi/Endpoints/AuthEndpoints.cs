using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using Server.WebHook;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Authentication API endpoints
    /// </summary>
    public static class AuthEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/auth");

            group.MapPost("/login", Login);
            group.MapPost("/refresh", Refresh);
            group.MapGet("/check-admin", CheckAdmin);
            group.MapPost("/init-admin", InitAdmin);
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        private static IResult Login(LoginRequest request, HttpContext context, JwtHelper jwtHelper, ServerDataService dataService)
        {
            var ip = WebApiLogger.GetClientIp(context);

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { message = "Email and password are required" });
            }

            var authResult = dataService.AuthenticateWithReason(request.Email, request.Password);
            if (authResult.Status == LoginResultStatus.AccountNotFound)
            {
                _ = WebHookSender.SendLoginFailAsync(request.Email, ip, LoginResultStatus.AccountNotFound);
                WebApiLogger.Security(request.Email, ip, "登录失败", "账号不存在");
                return Results.Unauthorized();
            }

            if (authResult.Status == LoginResultStatus.PasswordError)
            {
                _ = WebHookSender.SendLoginFailAsync(request.Email, ip, LoginResultStatus.PasswordError);
                WebApiLogger.Security(request.Email, ip, "登录失败", "密码错误");
                return Results.Unauthorized();
            }

            var account = authResult.Account!;

            // Check if account has minimum required permission (Supervisor or higher)
            if (account.Identify < AccountIdentity.Supervisor)
            {
                _ = WebHookSender.SendLoginFailAsync(account.EMailAddress, ip, LoginResultStatus.InsufficientPermission);
                WebApiLogger.Security(account.EMailAddress, ip, "登录被拒", "权限不足");
                return Results.Json(new { message = "Insufficient permissions. Supervisor or higher required." }, statusCode: 403);
            }

            // Check if account is banned
            if (account.Banned)
            {
                _ = WebHookSender.SendLoginFailAsync(account.EMailAddress, ip, LoginResultStatus.AccountBanned);
                WebApiLogger.Security(account.EMailAddress, ip, "登录被拒", "账号已封禁");
                return Results.Json(new { message = "Account is banned" }, statusCode: 403);
            }

            var token = jwtHelper.GenerateToken(account.EMailAddress, account.Identify);
            var refreshToken = jwtHelper.GenerateRefreshToken();

            _ = WebHookSender.SendLoginSuccessAsync(account.EMailAddress, ip);
            WebApiLogger.Audit(account.EMailAddress, ip, "登录成功");
            return Results.Ok(new LoginResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                Email = account.EMailAddress,
                Identity = account.Identify.ToString(),
                ExpiresIn = Envir.Config.WebApiJwtExpiration * 60
            });
        }

        /// <summary>
        /// Refresh JWT token
        /// </summary>
        private static IResult Refresh(RefreshRequest request, HttpContext context, JwtHelper jwtHelper, ServerDataService dataService)
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return Results.BadRequest(new { message = "Token is required" });
            }

            var principal = jwtHelper.ValidateToken(request.Token);
            var email = JwtHelper.GetEmail(principal);

            if (string.IsNullOrEmpty(email))
            {
                return Results.Unauthorized();
            }

            var account = dataService.GetAccountByEmail(email);
            if (account == null || account.Banned || account.Identify < AccountIdentity.Supervisor)
            {
                WebApiLogger.Security(email, WebApiLogger.GetClientIp(context), "刷新令牌失败", "账号无效、已封禁或权限不足");
                return Results.Unauthorized();
            }

            var newToken = jwtHelper.GenerateToken(account.EMailAddress, account.Identify);
            var refreshToken = jwtHelper.GenerateRefreshToken();

            WebApiLogger.Audit(context, "刷新令牌");
            return Results.Ok(new LoginResponse
            {
                Token = newToken,
                RefreshToken = refreshToken,
                Email = account.EMailAddress,
                Identity = account.Identify.ToString(),
                ExpiresIn = Envir.Config.WebApiJwtExpiration * 60
            });
        }

        /// <summary>
        /// Check if super admin exists
        /// </summary>
        private static IResult CheckAdmin(ServerDataService dataService)
        {
            var hasSuperAdmin = dataService.HasSuperAdmin();
            return Results.Ok(new { hasSuperAdmin });
        }

        /// <summary>
        /// Initialize super admin account
        /// </summary>
        private static IResult InitAdmin(HttpContext context, ServerDataService dataService)
        {
            var ip = WebApiLogger.GetClientIp(context);

            if (dataService.HasSuperAdmin())
            {
                WebApiLogger.Security(null, ip, "初始化超管被拒", "超管已存在");
                return Results.BadRequest(new { message = "Super admin already exists" });
            }

            var success = dataService.InitializeSuperAdmin();
            if (success)
            {
                WebApiLogger.Audit(null, ip, "初始化超管", "创建默认超管账号");
                return Results.Ok(new { message = "Super admin initialized successfully" });
            }

            WebApiLogger.Security(null, ip, "初始化超管失败");
            return Results.Problem("Failed to initialize super admin");
        }
    }

    #region Request/Response Models

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RefreshRequest
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }

    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string Email { get; set; } = "";
        public string Identity { get; set; } = "";
        public int ExpiresIn { get; set; }
    }

    #endregion
}
