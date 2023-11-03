using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

using Moq;
using Lextm.SharpSnmpLib;

using U5kManServer;
using U5kManServer.Procesos;

namespace XProccessUnitTests.MockedClass
{
    public class MockedTop
    {
        public IProcessData ProcessData => ProcessDataMock.Object;
        public IProcessSnmp ProcessSnmp => ProcessSnmpMock.Object;

        public MockedTop()
        {
            ProcessDataMock = new Mock<IProcessData>();
            ProcessSnmpMock = new Mock<IProcessSnmp>();
            SetupProcessData();
            SetupProcessSndmp();
        }
        public void RaiseTrap()
        {
            ProcessSnmpMock.Raise(x => x.TrapReceived += null,
                new TrapBus.TrapEventArgs()
                {
                    From = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 161),
                    TrapOid = "none",
                    VarOid = ".1.3.6.1.4.1.7916.8.2.3.1.0",
                    VarData = new OctetString("hola")
                });
        }
        void SetupProcessData()
        {
            ProcessDataMock.Setup(library => library.Data)
                .Returns(mockedData);
            ProcessDataMock.Setup(x => x.IsMaster)
                .Returns(true);
        }
        void SetupProcessSndmp()
        {
            ProcessSnmpMock.Setup(x => x.GetData(It.IsAny<object>(), It.IsAny<IList<Variable>>()))
                .Returns<object, IList<Variable>>((from, input) => SimulatedSnmpData(input));
        }
        U5kManStdData mockedData => new U5kManStdData()
        {
            CFGTOPS = new List<stdPos>()
            {
                new stdPos(){name = "MOCKEDTOP01", ip = "127.0.0.1", SectorOnPos = "**FS**", snmpport = 161, uris = new List<string>(){ "uri1" } }
            }
        };
        Task<IList<Variable>> SimulatedSnmpData(IList<Variable> input)
        {
            return Task.Run(() =>(IList<Variable>)new List<Variable>()
            {
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.2.2.1.0"), new Integer32(1)),
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.2.2.2.0"), new Integer32(0)),
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.2.2.3.0"), new Integer32(1))
            });
            //throw new NotImplementedException("Simulacion de Lectura no implementada");
        }
        readonly Mock<IProcessData> ProcessDataMock;
        readonly Mock<IProcessSnmp> ProcessSnmpMock;
    }
}
