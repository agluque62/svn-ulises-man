using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using U5kManServer.WebAppServer;

namespace UnitTesting
{
    [TestClass]
    public class SyncServersTest
    {
        void PrepareTest(Action<MainStandbySyncServer, MainStandbySyncServer> execute)
        {
            using (var master=new MainStandbySyncServer())
                using(var slave=new MainStandbySyncServer())
            {
                master.Start("192.168.168.1", "224.100.10.1", 33333);
                slave.Start("192.168.168.2", "224.100.10.1", 33333);
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();

                execute(master, slave);
                master.Stop();
                slave.Stop();
            }
        }
        [TestMethod]
        public void TestMethod1()
        {
            PrepareTest((master, slave) =>
            {
                for (int loop=0; loop < 60; loop++)
                {
                    slave.Sync(U5kManServer.WebAppServer.cmdSync.Testing, "1234567");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
            });
        }
        [TestMethod]
        public void TestMethod2()
        {
            PrepareTest((master, slave) =>
            {
                master.QueryVersionData();
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();
            });
        }
    }
}
