namespace Server.WebHook
{
    /// <summary>
    /// 后台登录结果状态（含成功与四类失败原因）
    /// </summary>
    public enum LoginResultStatus
    {
        Success,
        AccountNotFound,
        PasswordError,
        InsufficientPermission,
        AccountBanned
    }
}