using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using U5kManServer;
namespace UnitTesting
{
    [TestClass]
    public class ConcurrentNtpInfoClassTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            Debug.WriteLine("Starting test");
            var data = string.Empty;
            var info = new NtpInfoClass();
            Task.Run(() =>
            {
                Debug.WriteLine("Start task-01");
                info.Actualize("Testing", (connected, ip) =>
                {
                    Debug.WriteLine($"NtpInfo actualized {connected}, {ip}");
                });
                Debug.WriteLine("End task-01");
            });

            Task.Run(async () =>
            {
                await Task.Delay(100);
                data = info.GlobalStatus;
                Debug.WriteLine($"Info Ntp => {data}");
            });

            Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            Debug.WriteLine($"Endtest test => {data}");
        }
    }
}
