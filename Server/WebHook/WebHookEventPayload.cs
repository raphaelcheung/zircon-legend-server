using System;
using System.Text.Json.Serialization;

namespace Server.WebHook
{
    /// <summary>
    /// webhook 事件载荷抽象基类
    /// </summary>
    public abstract class WebHookEventPayload
    {
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}