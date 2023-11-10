using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using U5kManServer;
using U5kBaseDatos;
using XProccessUnitTests.MockedClass;
using XProccessUnitTests.MockedClasss;

namespace XProccessUnitTests
{
    public class PbxServiceTests
    {
        [Fact]
        public void StartingAndChangeIpPbx()
        {
            PrepareTest((service, mock) =>
            {
                service.Start();
                mock.InDb = true;
                mock.Reachable = true;
                mock.Connected = true;
                mock.WithVersion = true;
                mock.Ip = "11.20.91.1";
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                mock.Ip = "11.20.91.2";
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                service.Stop(TimeSpan.FromSeconds(2));
            });
        }
        [Fact]
        public void ChangingSlaveToMasterToSlave()
        {
            PrepareTest((service, mock) =>
            {
                mock.Master = false;
                mock.InDb = true;
                mock.Reachable = true;
                mock.Connected = true;
                mock.WithVersion = true;
                service.Start();
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                mock.Master = true;
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                mock.Master = false;
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                service.Stop(TimeSpan.FromSeconds(2));
            });
        }
        void PrepareTest(Action<PabxItfService, MockedPbx> execute)
        {
            var mockedService = new MockedPbx();
            var mainService = new PabxItfService(
                mockedService.DataService,
                mockedService.PingService,
                mockedService.PbxWsService,
                mockedService.FtpService
                );
            execute(mainService, mockedService);
        }
    }
}
