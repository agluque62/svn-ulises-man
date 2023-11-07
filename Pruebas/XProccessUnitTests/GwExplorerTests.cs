using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using XProccessUnitTests.MockedClass;
using U5kManServer;
using XProccessUnitTests.MockedClasss;

namespace XProccessUnitTests
{
    public class GwExplorerTests
    {
        void PrepareTest(Action<GwExplorer, MockedGw> action)
        {
            var mock = new MockedGw();
            using (var service = new GwExplorer(mock.DataService, mock.PingService, mock.SipService, mock.SnmpService, mock.HttpService))
            {
                action(service, mock);
            }
        }
        [Fact]
        public void Test01()
        {
            PrepareTest((service, mock) =>
            {
                service.Start();
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                mock.RaiseTrap();
                Task.Delay(TimeSpan.FromSeconds(25)).Wait();
                service.Stop(TimeSpan.FromSeconds(5));
            });
        }
    }
}
