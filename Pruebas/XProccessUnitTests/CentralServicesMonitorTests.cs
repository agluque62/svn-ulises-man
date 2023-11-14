using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using U5kManServer;
using U5kBaseDatos;
using XProccessUnitTests.MockedClasses;
using U5kManServer.Services;

namespace XProccessUnitTests
{
    public class CentralServicesMonitorTests
    {
        void PrepareTest(Action<CentralServicesMonitor, MockedNbx> action)
        {
            using (var mock = new MockedNbx())
            using(var service = new CentralServicesMonitor(mock.DataService, mock.UdpService, mock.HtppService, 1022))
            {
                action(service, mock);
            }
        }
        [Fact]
        public void CentralServicesMonitorTest_01()
        {
            PrepareTest((service, mock) =>
            {
                service.Start();
                Task.Delay(TimeSpan.FromSeconds(20)).Wait();
                mock.IsActive = false;
                Task.Delay(TimeSpan.FromSeconds(20)).Wait();
                mock.IsActive = true;
                Task.Delay(TimeSpan.FromSeconds(20)).Wait();
            });
        }

        [Fact]
        public void CentralServicesMonitorTest_02()
        {
            PrepareTest((service, mock) =>
            {
                service.Start();
                Task.Delay(TimeSpan.FromSeconds(60)).Wait();
                mock.IsMaster = false;
                Task.Delay(TimeSpan.FromSeconds(60)).Wait();
                mock.IsMaster = true;
                Task.Delay(TimeSpan.FromSeconds(60)).Wait();
            });
        }
    }
}
