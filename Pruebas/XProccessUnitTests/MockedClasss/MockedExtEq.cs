using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

using Moq;

using U5kManServer;
using U5kManServer.Procesos;


namespace XProccessUnitTests.MockedClasss
{
    public class MockedExtEq
    {
        public IDataService ProcessData => ProcessDataMock.Object;
        public IPingService ProcessPing => ProcessPingMock.Object;
        public ICommSipService ProcessSip => ProcessSipMock.Object;

        public MockedExtEq(bool presente = true, bool sipagent=true) 
        {
            ProcessDataMock = new Mock<IDataService>();
            ProcessPingMock = new Mock<IPingService>();
            ProcessSipMock = new Mock<ICommSipService>();
            Presente = presente;
            SipAgent = sipagent;

            SetupProcessData();
            SetupProcessPing();
            SetupProcessSip();
        }
        void SetupProcessData()
        {
            ProcessDataMock.Setup(library => library.Data)
                .Returns(mockedData);
            ProcessDataMock.Setup(x => x.IsMaster)
                .Returns(true);
        }
        void SetupProcessPing()
        {
            ProcessPingMock
                .Setup(proc => proc.Ping(
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .Returns(Task.Run(() => Presente));
        }
        void SetupProcessSip()
        {
            ProcessSipMock
                .Setup(proc => proc.Ping(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>()))
                .Returns(Task.Run(() => new QueryServiceResult<string>(SipAgent, "OK")));
        }
        U5kManStdData mockedData => new U5kManStdData()
        {
            CFGEQS = new List<EquipoEurocae>()
            {
                new EquipoEurocae(){ Id="MK-EQ1", Ip1="127.0.0.1", sip_port=5060, sip_user="MK-RC01", Tipo=0},
                new EquipoEurocae(){ Id="MK-EQ1", Ip1="127.0.0.1", sip_port=5060, sip_user="MK-RC02", Tipo=1}
            }
        };
        readonly Mock<IDataService> ProcessDataMock;
        readonly Mock<IPingService> ProcessPingMock;
        readonly Mock<ICommSipService> ProcessSipMock;
        readonly bool Presente;
        readonly bool SipAgent;
    }
}
