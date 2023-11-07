using System;
using System.Linq;
using Xunit;

using U5kManServer;
using U5kBaseDatos;

using XProccessUnitTests.MockedClass;
using System.Threading.Tasks;
using NucleoGeneric;
using XProccessUnitTests.MockedClasss;

namespace XProccessUnitTests
{
    public class ExtEquSpvTests
    {
        [Fact]
        public void Test1()
        {
            PrepareTest((proc, mock) =>
            {
                Task.Delay(TimeSpan.FromSeconds(33)).Wait();
            });
        }
        void PrepareTest(Action<ExtEquSpv, MockedExtEq> cont)
        {
            var mockedData = new MockedExtEq(true, true);
            var proc = new ExtEquSpv(mockedData.DataService, mockedData.PingService, mockedData.SipService);
            proc.Start();
            cont(proc, mockedData);
            proc.Stop(TimeSpan.FromSeconds(5));
        }
    }
}
