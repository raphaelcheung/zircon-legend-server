using System.Text.Json.Serialization;

namespace Server.WebHook
{
    /// <summary>
    /// 后台登录结果通知载荷
    /// </summary>
    public class LoginResultPayload : WebHookEventPayload
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";

        [JsonPropertyName("ip")]
        public string Ip { get; set; } = "";

        /// <summary>
        /// 失败原因（snake_case），仅在 eventType 为 admin_login_fail 时存在
        /// </summary>
        [JsonPropertyName("failReason")]
        public string? FailReason { get; set; }
    }
}