using System;
using Server.Envir;

namespace Server.WebHook
{
    /// <summary>
    /// 定时采样调度器（封装下次采样时间，首次触发在完整间隔后）
    /// </summary>
    public class WebHookScheduler
    {
        private DateTime _nextSampleTime;

        public WebHookScheduler()
        {
            _nextSampleTime = DateTime.Now + GetEffectiveInterval();
        }

        /// <summary>
        /// 每秒由主循环调用；到期返回 true 并推进下次采样时间，否则返回 false
        /// </summary>
        public bool TryTick(DateTime now)
        {
            if (now < _nextSampleTime)
                return false;

            _nextSampleTime = now + GetEffectiveInterval();
            return true;
        }

        private static TimeSpan GetEffectiveInterval()
        {
            var minutes = Config.WebHookIntervalMinutes;
            if (minutes <= 0)
                minutes = 1;
            return TimeSpan.FromMinutes(minutes);
        }
    }
}