using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Utilities;

namespace UnitTesting
{
    [TestClass]
    public class TinyMessengerTest
    {
        public class MyMessage : ITinyMessage
        {
            /// <summary>
            /// The sender of the message, or null if not supported by the message implementation.
            /// </summary>
            public object Sender { get; private set; }
            public string Message { get; set; }
        }
        [TestMethod]
        public void TestMethod1()
        {
            var end = new ManualResetEvent(false);
            var messageHub = new TinyMessengerHub();
            Task.Run(() =>
            {
                var subt = messageHub.Subscribe<MyMessage>((msg) =>
                {
                    Debug.WriteLine($"{DateTime.Now}: {msg.Message}");
                });
                end.WaitOne();

                messageHub.Unsubscribe<MyMessage>(subt);
            });

            Task.Delay(500).Wait();
            messageHub.Publish<MyMessage>(new MyMessage() { Message = "hola mundo..." });
            Task.Delay(500).Wait();
            messageHub.Publish<MyMessage>(new MyMessage() { Message = "Que tal estas..." });
            Task.Delay(500).Wait();
            end.Set();
        }

    //    [TestMethod]
    //    public void TestMethod2()
    //    {
    //        var end = new ManualResetEvent(false);
    //        EventBus.GlobalEvents.Publish(EventBus.GlobalEventsIds.Standby);
    //        Task.Run(() =>
    //        {
    //            var subt = EventBus.GlobalEvents.Subscribe((msg) =>
    //            {
    //                Debug.WriteLine($"{DateTime.Now}: {msg}");
    //            });
    //            Debug.Assert(subt != null);
    //            end.WaitOne();

    //            EventBus.GlobalEvents.Unsubscribe(subt);
    //        });

    //        Task.Delay(500).Wait();
    //        EventBus.GlobalEvents.Publish(EventBus.GlobalEventsIds.Main);
    //        Task.Delay(500).Wait();
    //        EventBus.GlobalEvents.Publish(EventBus.GlobalEventsIds.CfgLoad);
    //        Task.Delay(500).Wait();
    //        end.Set();
    //    }
    }
}
