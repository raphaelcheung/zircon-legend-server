using System;
using System.Text.Json.Serialization;

namespace Server.WebHook
{
    /// <summary>
    /// 定时在线人数通知载荷
    /// </summary>
    public class OnlineCountPeriodicPayload : WebHookEventPayload
    {
        public OnlineCountPeriodicPayload()
        {
            EventType = "online_count_periodic";
        }

        [JsonPropertyName("onlineCount")]
        public int OnlineCount { get; set; }
    }
}