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
            group.MapPut("/{email}/gold", ChangeGameGold);
            group.MapPut("/{email}/hunt-gold", ChangeHuntGold);
            group.MapPut("/{email}/normal-gold", ChangeNormalGold);
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
                // [后台封禁账号] 详细记录管理员从Web后台封禁玩家账号的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台封禁账号] 管理员={adminEmail}, 目标账号={email}, 原因={request.Reason ?? "无"}, 过期时间={(expiryDate.HasValue ? expiryDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "永久")}");

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
                // [后台解封账号] 详细记录管理员从Web后台解封玩家账号的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台解封账号] 管理员={adminEmail}, 目标账号={email}");

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
                // [后台调整权限] 详细记录管理员从Web后台修改玩家账号权限等级的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台调整权限] 管理员={adminEmail}, 目标账号={email}, 原权限={targetAccount.Identify}, 新权限={newIdentity}");

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
        /// <summary>
        /// 调整账号元宝数量（仅限 Admin 级以上账号）
        /// </summary>
        private static async Task<IResult> ChangeGameGold(string email, ChangeGameGoldRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            var currentIdentity = JwtHelper.GetIdentity(user);

            // 检查目标账号是否存在
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "未找到该账号" });
            }

            // 安全限制：防止同级或低级管理员修改高级/同级账号的钱包数据（超级管理员除外）
            if (targetAccount.Identify >= currentIdentity && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            // 【安全校验】防止大数值输入导致底层整数溢出清空玩家金币
            if (request.Amount < -2_000_000_000 || request.Amount > 2_000_000_000)
            {
                return Results.BadRequest(new { message = "调整金额超限，允许的范围是 ±2,000,000,000" });
            }

            var success = await dataService.AddGameGold(email, (int)request.Amount);
            if (success)
            {
                // [后台调整元宝] 记录管理员从Web后台调整玩家元宝的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台调整元宝] 管理员={adminEmail}, 目标账号={email}, 变动数量={request.Amount}");

                return Results.Ok(new { message = "元宝调整成功" });
            }

            return Results.Problem("调整元宝失败");
        }

        /// <summary>
        /// 调整账号猎币数量（仅限 Admin 级以上账号）
        /// </summary>
        private static async Task<IResult> ChangeHuntGold(string email, ChangeHuntGoldRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            var currentIdentity = JwtHelper.GetIdentity(user);

            // 检查目标账号是否存在
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "未找到该账号" });
            }

            // 安全限制：防止同级或低级管理员修改高级/同级账号的钱包数据（超级管理员除外）
            if (targetAccount.Identify >= currentIdentity && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            // 【安全校验】防止大数值输入导致底层整数溢出清空玩家金币
            if (request.Amount < -2_000_000_000 || request.Amount > 2_000_000_000)
            {
                return Results.BadRequest(new { message = "调整金额超限，允许的范围是 ±2,000,000,000" });
            }

            var success = await dataService.AddHuntGold(email, (int)request.Amount);
            if (success)
            {
                // [后台调整猎币] 记录管理员从Web后台调整玩家猎币的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台调整猎币] 管理员={adminEmail}, 目标账号={email}, 变动数量={request.Amount}");

                return Results.Ok(new { message = "猎币调整成功" });
            }

            return Results.Problem("调整猎币失败");
        }

        /// <summary>
        /// 调整账号普通金币数量（仅限 Admin 级以上账号）
        /// </summary>
        private static async Task<IResult> ChangeNormalGold(string email, ChangeNormalGoldRequest request, ClaimsPrincipal user, ServerDataService dataService)
        {
            if (!JwtHelper.HasMinimumIdentity(user, AccountIdentity.Admin))
            {
                return Results.Forbid();
            }

            var currentIdentity = JwtHelper.GetIdentity(user);

            // 检查目标账号是否存在
            var targetAccount = dataService.GetAccountByEmail(email);
            if (targetAccount == null)
            {
                return Results.NotFound(new { message = "未找到该账号" });
            }

            // 安全限制：防止同级或低级管理员修改高级/同级账号的钱包数据（超级管理员除外）
            if (targetAccount.Identify >= currentIdentity && currentIdentity < AccountIdentity.SuperAdmin)
            {
                return Results.Forbid();
            }

            var success = await dataService.AddNormalGold(email, request.Amount);
            if (success)
            {
                // [后台调整金币] 记录管理员从Web后台调整玩家金币的操作日志
                var adminEmail = JwtHelper.GetEmail(user);
                Server.Envir.SEnvir.Log($"[后台调整金币] 管理员={adminEmail}, 目标账号={email}, 变动数量={request.Amount}");

                return Results.Ok(new { message = "金币调整成功" });
            }

            return Results.Problem("调整金币失败");
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

    public class ChangeGameGoldRequest
    {
        public long Amount { get; set; }
    }

    public class ChangeHuntGoldRequest
    {
        public long Amount { get; set; }
    }

    public class ChangeNormalGoldRequest
    {
        public long Amount { get; set; }
    }

    #endregion
}
