using System.Collections.Generic;
using Server.Envir;

namespace Server.WebHook
{
    /// <summary>
    /// 阈值穿越防抖状态机（纯内存，重启后标记重置为 false）
    /// </summary>
    public class ThresholdMonitor
    {
        private bool _已超过;
        private bool _已低于;

        public ThresholdMonitor()
        {
            _已超过 = false;
            _已低于 = false;
        }

        /// <summary>
        /// 输入当前在线人数，返回本次采样产出的事件列表（0/1/2 个）
        /// </summary>
        public List<ThresholdEvent> Sample(int currentCount)
        {
            var events = new List<ThresholdEvent>();

            var highThreshold = Config.WebHookHighThreshold;
            var lowThreshold = Config.WebHookLowThreshold;

            if (highThreshold > 0)
            {
                if (currentCount > highThreshold)
                {
                    if (!_已超过)
                    {
                        _已超过 = true;
                        events.Add(new ThresholdEvent(true, currentCount, highThreshold, "up"));
                    }
                }
                else
                {
                    if (_已超过)
                        _已超过 = false;
                }
            }

            if (lowThreshold > 0)
            {
                if (currentCount < lowThreshold)
                {
                    if (!_已低于)
                    {
                        _已低于 = true;
                        events.Add(new ThresholdEvent(false, currentCount, lowThreshold, "down"));
                    }
                }
                else
                {
                    if (_已低于)
                        _已低于 = false;
                }
            }

            return events;
        }
    }
}