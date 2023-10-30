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
            PrepareTest((topP, mock) =>
            {
                Task.Delay(TimeSpan.FromSeconds(33)).Wait();
                mock.RaiseTrap();
                Task.Delay(TimeSpan.FromSeconds(21)).Wait();
            });
        }
        void PrepareTest(Action<TopSnmpExplorer, MockedTop> action)
        {
            var mockedTopData = new MockedTop();            
            var topProc = new TopSnmpExplorer(CambiaEstado, mockedTopData.ProcessData, mockedTopData.ProcessSnmp);

            topProc.Start();
            action(topProc, mockedTopData);
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
