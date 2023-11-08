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
        public void PbxServiceTest1()
        {
            PrepareTest((service, mock) =>
            {
                service.Start();
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                mock.InDb = true;
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                mock.Reachable = true;
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
