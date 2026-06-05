using Library;
using Server.WebApi.Auth;
using Server.WebApi.Services;
using System.Security.Claims;

namespace Server.WebApi.Endpoints
{
    /// <summary>
    /// Account management API endpoints
    /// </summary>
    public static class AccountEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/api/accounts")
                .RequireAuthorization();

            group.MapGet("/", GetAccounts);
            group.MapGet("/{email}", GetAccountDetail);
            group.MapPost("/", CreateAccount);
            group.MapPut("/{email}/ban", BanAccount);
            group.MapPut("/{email}/unban", UnbanAccount);
            group.MapPut("/{email}/identity", ChangeIdentity);
            group.MapPut("/{email}/reset-password", ResetPassword);
            group.MapPut("/{email}/gold", UpdateGold);
        }

        /// <summary>
        /// Get accounts list with pagination
        /// </summary>
        private static IResult GetAccounts(
            ClaimsPrincipal user,
            ServerDataService dataService,
            int page = 1,
            int pageSize = 20,
            string? search = null)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var (accounts, total) = dataService.GetAccounts(page, pageSize, search);

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)total / pageSize),
                accounts
            });
        }

        /// <summary>
        /// Get account details with characters
        /// </summary>
        private static IResult GetAccountDetail(string email, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Supervisor))
            {
                return Results.Forbid();
            }

            var account = dataService.GetAccountDetail(email);
            if (account == null)
            {
                return Results.NotFound(new { message = "Account not found" });
            }

            return Results.Ok(account);
        }

        /// <summary>
        /// Create new account
        /// </summary>
        private static IResult CreateAccount(CreateAccountRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { message = "Email and password are required" });
            }

            var identity = AccountIdentity.Normal;
            if (!string.IsNullOrEmpty(request.Identity))
            {
                if (!Enum.TryParse<AccountIdentity>(request.Identity, out identity))
                {
                    return Results.BadRequest(new { message = "Invalid identity value" });
                }
            }

            // Only SuperAdmin can create Admin or SuperAdmin accounts
            var currentIdentity = JwtHelper.GetIdentity(user);
            if (identity >= AccountIdentity.Admin && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            var (success, message) = dataService.CreateAccount(request.Email, request.Password, identity);

            if (success)
            {
                return Results.Ok(new { message });
            }

            return Results.BadRequest(new { message });
        }

        /// <summary>
        /// Ban account
        /// </summary>
        private static IResult BanAccount(string email, BanRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            // Check target account's identity
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "Account not found" });
            }

            // Cannot ban accounts with higher or equal identity
            var currentIdentity = JwtHelper.GetIdentity(user);
            if (targetAccount.Identify >= currentIdentity)
            {
                return Results.Forbid();
            }

            DateTime? expiryDate = null;
            if (request.ExpiryDate.HasValue)
            {
                expiryDate = request.ExpiryDate.Value;
            }

            var success = dataService.BanAccount(email, request.Reason ?? "Banned by admin", expiryDate);
            if (success)
            {
                return Results.Ok(new { message = "Account banned successfully" });
            }

            return Results.Problem("Failed to ban account");
        }

        /// <summary>
        /// Unban account
        /// </summary>
        private static IResult UnbanAccount(string email, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            var success = dataService.UnbanAccount(email);
            if (success)
            {
                return Results.Ok(new { message = "Account unbanned successfully" });
            }

            return Results.NotFound(new { message = "Account not found" });
        }

        /// <summary>
        /// Change account identity level
        /// </summary>
        private static IResult ChangeIdentity(string email, ChangeIdentityRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            if (!Enum.TryParse<AccountIdentity>(request.Identity, out var newIdentity))
            {
                return Results.BadRequest(new { message = "Invalid identity value" });
            }

            var currentIdentity = JwtHelper.GetIdentity(user);

            // Only SuperAdmin can set Admin or SuperAdmin
            if (newIdentity >= AccountIdentity.Admin && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            // Check target account
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "Account not found" });
            }

            // Cannot modify accounts with higher or equal identity (unless SuperAdmin)
            if (targetAccount.Identify >= currentIdentity && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            var success = dataService.ChangeAccountIdentity(email, newIdentity);
            if (success)
            {
                return Results.Ok(new { message = "Account identity changed successfully" });
            }

            return Results.Problem("Failed to change account identity");
        }

        /// <summary>
        /// Reset account password
        /// </summary>
        private static IResult ResetPassword(string email, ResetPasswordRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { message = "Password must be at least 6 characters" });
            }

            var currentIdentity = JwtHelper.GetIdentity(user);

            // Check target account
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "Account not found" });
            }

            // Cannot reset password for accounts with higher or equal identity (unless SuperAdmin)
            if (targetAccount.Identify >= currentIdentity && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            var success = dataService.ResetPassword(email, request.NewPassword);
            if (success)
            {
                return Results.Ok(new { message = "Password reset successfully" });
            }

            return Results.Problem("Failed to reset password");
        }

        /// <summary>
        /// Update account gold and game gold
        /// </summary>
        private static IResult UpdateGold(string email, UpdateGoldRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Operator))
            {
                return Results.Forbid();
            }

            if (request.GameGold < 0 || request.HuntGold < 0)
            {
                return Results.BadRequest(new { message = "Gold values cannot be negative" });
            }

            var currentIdentity = JwtHelper.GetIdentity(user);

            // Check target account
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "Account not found" });
            }

            // Cannot modify gold for accounts with higher or equal identity (unless SuperAdmin)
            if (targetAccount.Identify >= currentIdentity && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            var success = dataService.UpdateAccountGold(email, request.GameGold, request.HuntGold);
            if (success)
            {
                return Results.Ok(new { message = "Gold updated successfully" });
            }

            return Results.Problem("Failed to update gold");
        }
    }

    #region Request Models

    public class CreateAccountRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? Identity { get; set; }
    }

    public class BanRequest
    {
        public string? Reason { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class ChangeIdentityRequest
    {
        public string Identity { get; set; } = "";
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }

    public class UpdateGoldRequest
    {
        public int GameGold { get; set; }
        public int HuntGold { get; set; }
    }

    #endregion
}
