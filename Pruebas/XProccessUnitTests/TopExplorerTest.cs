using System;
using System.Linq;
using Xunit;

using U5kManServer;
using U5kBaseDatos;

namespace XProccessUnitTests
{
    public class TopExplorerTest
    {
        [Fact]
        public void TestMethod1()
        {
            PrepareTest((topP) =>
            {

            });
        }
        void PrepareTest(Action<TopSnmpExplorer> action)
        {
            var topProc = new TopSnmpExplorer(CambiaEstado);
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
