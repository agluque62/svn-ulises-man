using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Utilities;

namespace U5kManServer
{
    public class EventBus 
    {
        public enum GlobalEventsIds { Main, Standby, CfgLoad }

        public class GlobalEvents
        {
            class GlobalEventMessage : ITinyMessage
            {
                /// <summary>
                /// The sender of the message, or null if not supported by the message implementation.
                /// </summary>
                public object Sender { get; private set; }
                public GlobalEventsIds SEvent { get; set; }
            }
            public static object Subscribe(Action<GlobalEventsIds> notify)
            {
                return SystemEventHub.Subscribe<GlobalEventMessage>((msg) => notify(msg.SEvent));
            }
            public static void Unsubscribe(object tk)
            {
                SystemEventHub.Unsubscribe<GlobalEventMessage>(tk as TinyMessageSubscriptionToken);
            }
            public static void Publish(GlobalEventsIds sevent)
            {
                SystemEventHub.Publish<GlobalEventMessage>(new GlobalEventMessage() { SEvent = sevent });
            }
            static TinyMessengerHub SystemEventHub = new TinyMessengerHub();
        }

    }
}
