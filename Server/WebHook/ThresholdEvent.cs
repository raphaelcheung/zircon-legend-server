namespace Server.WebHook
{
    /// <summary>
    /// 阈值穿越事件（ThresholdMonitor.Sample 的返回元素）
    /// </summary>
    public record ThresholdEvent(
        bool IsHigh,
        int CurrentCount,
        int Threshold,
        string Direction);
}