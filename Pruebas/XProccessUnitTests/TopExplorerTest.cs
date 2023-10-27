using System;
using System.Linq;
using Xunit;

using U5kManServer;
using U5kBaseDatos;

using XProccessUnitTests.MockedClass;
using System.Threading.Tasks;

namespace XProccessUnitTests
{
    public class TopExplorerTest
    {
        [Fact]
        public void TestMethod1()
        {
            PrepareTest((topP) =>
            {
                Task.Delay(TimeSpan.FromSeconds(120)).Wait();
            });
        }
        void PrepareTest(Action<TopSnmpExplorer> action)
        {
            var mockedTopData = new MockedTop();            
            var topProc = new TopSnmpExplorer(CambiaEstado, mockedTopData.ProcessData);

            topProc.Start();
            action(topProc);
            topProc.Stop(TimeSpan.FromSeconds(5));
        }
        std CambiaEstado(std antiguo, std nuevo, int scv, eIncidencias inci, eTiposInci thw, string idhw, params object[] parametros)
        {
            if (antiguo != nuevo)
            {
            }
            return nuevo;
        }
    }
}
