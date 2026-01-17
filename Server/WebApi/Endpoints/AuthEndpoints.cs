using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;

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
        private static IResult Login(LoginRequest request, JwtHelper jwtHelper, ServerDataService dataService)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { message = "Email and password are required" });
            }

            var account = dataService.Authenticate(request.Email, request.Password);
            if (account == null)
            {
                return Results.Unauthorized();
            }

            // Check if account has minimum required permission (Supervisor or higher)
            if (account.Identify < AccountIdentity.Supervisor)
            {
                return Results.Json(new { message = "Insufficient permissions. Supervisor or higher required." }, statusCode: 403);
            }

            // Check if account is banned
            if (account.Banned)
            {
                return Results.Json(new { message = "Account is banned" }, statusCode: 403);
            }

            var token = jwtHelper.GenerateToken(account.EMailAddress, account.Identify);
            var refreshToken = jwtHelper.GenerateRefreshToken();

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
        private static IResult Refresh(RefreshRequest request, JwtHelper jwtHelper, ServerDataService dataService)
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
                return Results.Unauthorized();
            }

            var newToken = jwtHelper.GenerateToken(account.EMailAddress, account.Identify);
            var refreshToken = jwtHelper.GenerateRefreshToken();

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
        private static IResult InitAdmin(ServerDataService dataService)
        {
            if (dataService.HasSuperAdmin())
            {
                return Results.BadRequest(new { message = "Super admin already exists" });
            }

            var success = dataService.InitializeSuperAdmin();
            if (success)
            {
                return Results.Ok(new { message = "Super admin initialized successfully" });
            }

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
