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
        public IProcessData ProcessData => ProcessDataMock.Object;
        public IProcessPing ProcessPing => ProcessPingMock.Object;
        public IProcessSip ProcessSip => ProcessSipMock.Object;

        public MockedExtEq(bool presente = true, bool sipagent=true) 
        {
            ProcessDataMock = new Mock<IProcessData>();
            ProcessPingMock = new Mock<IProcessPing>();
            ProcessSipMock = new Mock<IProcessSip>();
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
                    It.IsAny<bool>(), 
                    It.IsAny<Action<bool, IPStatus[]>>()))
                .Callback((string p1, bool p2, Action<bool, IPStatus[]> result) => result(Presente, new IPStatus[] { IPStatus.Success }));
        }
        void SetupProcessSip()
        {
            ProcessSipMock
                .Setup(proc => proc.Ping(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<Action<bool, string>>()))
                .Callback((string p1, string p2, int p3, bool p4, Action<bool, string> result) =>
                {
                    result(SipAgent, "OK");
                });
        }

        U5kManStdData mockedData => new U5kManStdData()
        {
            CFGEQS = new List<EquipoEurocae>()
            {
                new EquipoEurocae(){ Id="MK-EQ1", Ip1="127.0.0.1", sip_port=5060, sip_user="MK-RC01", Tipo=0},
                new EquipoEurocae(){ Id="MK-EQ1", Ip1="127.0.0.1", sip_port=5060, sip_user="MK-RC02", Tipo=1}
            }
        };
        readonly Mock<IProcessData> ProcessDataMock;
        readonly Mock<IProcessPing> ProcessPingMock;
        readonly Mock<IProcessSip> ProcessSipMock;
        readonly bool Presente;
        readonly bool SipAgent;
    }
}
