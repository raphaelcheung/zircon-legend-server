using Server.DBModels;

namespace Server.WebHook
{
    /// <summary>
    /// 认证结果（含离散失败原因）
    /// </summary>
    public class AuthenticateResult
    {
        public AccountInfo? Account { get; set; }
        public LoginResultStatus Status { get; set; }
    }
}
