using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using Utilities;

namespace UnitTesting
{
    [TestClass]
    public class HourNotifierTests
    {
        [TestMethod]
        public  void  TestMethod1()
        {
            var when = DateTime.Now.AddMinutes(1).TimeOfDay;
            using (var notifier = new HourNotifier())
            {
                Debug.WriteLine($"Event Programing => {when}");
                notifier.Setup(when, () =>
                {
                    Debug.WriteLine($"HourNotify => {DateTime.Now.TimeOfDay}");
                });

                Task.Delay(TimeSpan.FromMinutes(10)).Wait();
            }
        }
    }
}
