using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class HourNotifier : IDisposable
    {
        private System.Threading.Timer Timer { get; set; } = null;
        private CancellationToken Cancel { get; set; }
        private DateTime AwakeTime { get; set; }
        public HourNotifier(TimeSpan when, Action action)
        {
            Setup(when, action);
        }
        public void Dispose()
        {
        }
        private void Setup(TimeSpan when, Action action)
        {
            DateTime current = DateTime.Now;
            TimeSpan timeToGo = when - current.TimeOfDay;
            if (timeToGo < TimeSpan.Zero)
            {   // time already passed
                timeToGo = new TimeSpan(24, 0, 0) + timeToGo;
            }
            AwakeTime = current + timeToGo;

            Timer = new System.Threading.Timer(x =>
            {
                WaitAwakeTime();
                action?.Invoke();
#if DEBUG1
                when += TimeSpan.FromMinutes(1);
#endif
                Setup(when, action);
            }, null, timeToGo, System.Threading.Timeout.InfiniteTimeSpan);
        }

        private void WaitAwakeTime()
        {
            var now = DateTime.Now;
            if (AwakeTime > now)
            {
                Task.Delay(AwakeTime - now).Wait();
            }
        }
    }
}
