using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Envir;

namespace Server.WebHook
{
    /// <summary>
    /// webhook 外送核心组件（静态类，HttpClient 进程级单例复用）
    /// </summary>
    public static class WebHookSender
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// 通用发送方法：校验开关/URL → 异步 POST → 写日志
        /// 所有异常内部吞掉，绝不向调用方抛出
        /// </summary>
        public static Task SendAsync(WebHookEventPayload payload)
        {
            if (!Config.WebHookEnabled)
                return Task.CompletedTask;

            var url = Config.WebHookUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                SEnvir.Log("[WebHook] URL 未配置");
                return Task.CompletedTask;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                SEnvir.Log("[WebHook] URL 格式非法");
                return Task.CompletedTask;
            }

            var eventType = payload.EventType;

            _ = Task.Run(async () =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(payload, payload.GetType(), _jsonOptions);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(uri, content)
                        .WaitAsync(TimeSpan.FromSeconds(5));

                    if (response.IsSuccessStatusCode)
                        SEnvir.Log($"[WebHook] 发送成功 类型={eventType} 状态={(int)response.StatusCode}");
                    else
                        SEnvir.Log($"[WebHook] 发送失败 类型={eventType} 状态={(int)response.StatusCode}");
                }
                catch (TimeoutException)
                {
                    SEnvir.Log($"[WebHook] 发送超时 类型={eventType}");
                }
                catch (Exception ex)
                {
                    SEnvir.Log($"[WebHook] 发送异常 类型={eventType} err={ex.Message}");
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// 定时在线人数通知
        /// </summary>
        public static Task SendOnlineCountPeriodicAsync(int onlineCount)
        {
            var payload = new OnlineCountPeriodicPayload
            {
                Timestamp = DateTime.UtcNow,
                OnlineCount = onlineCount
            };
            return SendAsync(payload);
        }

        /// <summary>
        /// 阈值穿越通知
        /// </summary>
        public static Task SendThresholdEventAsync(bool isHigh, int currentCount, int threshold, string direction)
        {
            var payload = new ThresholdCrossingPayload
            {
                EventType = isHigh ? "online_threshold_high" : "online_threshold_low",
                Timestamp = DateTime.UtcNow,
                CurrentCount = currentCount,
                Threshold = threshold,
                Direction = direction
            };
            return SendAsync(payload);
        }

        /// <summary>
        /// 后台登录成功通知
        /// </summary>
        public static Task SendLoginSuccessAsync(string email, string ip)
        {
            var payload = new LoginResultPayload
            {
                EventType = "admin_login_success",
                Timestamp = DateTime.UtcNow,
                Email = email,
                Ip = ip,
                FailReason = null
            };
            return SendAsync(payload);
        }

        /// <summary>
        /// 后台登录失败通知
        /// </summary>
        public static Task SendLoginFailAsync(string email, string ip, LoginResultStatus reason)
        {
            var payload = new LoginResultPayload
            {
                EventType = "admin_login_fail",
                Timestamp = DateTime.UtcNow,
                Email = email,
                Ip = ip,
                FailReason = MapFailReason(reason)
            };
            return SendAsync(payload);
        }

        private static string MapFailReason(LoginResultStatus reason)
        {
            return reason switch
            {
                LoginResultStatus.AccountNotFound => "account_not_found",
                LoginResultStatus.PasswordError => "password_error",
                LoginResultStatus.InsufficientPermission => "insufficient_permission",
                LoginResultStatus.AccountBanned => "account_banned",
                _ => "unknown"
            };
        }
    }
}