using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using U5kManServer.WebAppServer;
using Utilities;

namespace UnitTesting
{
    [TestClass]
    public class WebServerTests
    {
        //const string RootDirectory = "c:\\Users\\arturo.garcia\\source\\repos\\nucleocc\\dev-branches\\ulises-ta\\Cd40\\Source\\NodeBox";
        const string RootDirectory = "..\\..\\..\\..\\U5kManServer";
        void PrepareTest(Action<WebAppServer> execute)
        {
            using (var server = new WebAppServer(RootDirectory))
            {
                server.SyncListenerSpvPeriod = 7;
                server.Start(1445, new Dictionary<string, WebAppServer.wasRestCallBack>());
                execute(server);
                server.Stop();
            }
        }
        [TestMethod]
        public void TestMethod1()
        {
            PrepareTest((server) =>
            {
                for (int loop=0; loop < 5; loop++)
                {
                    Task.Delay(TimeSpan.FromSeconds(23)).Wait();
                    DebuggingHelper.ThrowErrors.Program(DebuggingHelper.ThrowErrors.LaunchableErrors.WebListenerError);
                }
                Task.Delay(TimeSpan.FromSeconds(13)).Wait();
            });
        }
    }
}
