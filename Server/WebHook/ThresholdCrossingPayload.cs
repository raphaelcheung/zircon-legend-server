using System.Text.Json.Serialization;

namespace Server.WebHook
{
    /// <summary>
    /// 阈值穿越通知载荷
    /// </summary>
    public class ThresholdCrossingPayload : WebHookEventPayload
    {
        [JsonPropertyName("currentCount")]
        public int CurrentCount { get; set; }

        [JsonPropertyName("threshold")]
        public int Threshold { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = "";
    }
}