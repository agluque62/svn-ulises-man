using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

using Moq;

using U5kManServer;


namespace XProccessUnitTests.MockedClasss
{
    public class MockedExtEq
    {
        public IDataService DataService => DataServiceMock.Object;
        public IPingService PingService => PingServiceMock.Object;
        public ICommSipService SipService => SipServiceMock.Object;

        public MockedExtEq(bool presente = true, bool sipagent=true) 
        {
            DataServiceMock = new Mock<IDataService>();
            PingServiceMock = new Mock<IPingService>();
            SipServiceMock = new Mock<ICommSipService>();
            Presente = presente;
            SipAgent = sipagent;

            DataServiceSetup();
            PingServiceSetup();
            SipServiceSetup();
        }
        void DataServiceSetup()
        {
            DataServiceMock.Setup(library => library.Data)
                .Returns(mockedData);
            DataServiceMock.Setup(x => x.IsMaster)
                .Returns(true);
        }
        void PingServiceSetup()
        {
            PingServiceMock
                .Setup(proc => proc.Ping(
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .Returns(Task.Run(() => Presente));
        }
        void SipServiceSetup()
        {
            SipServiceMock
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
        readonly Mock<IDataService> DataServiceMock;
        readonly Mock<IPingService> PingServiceMock;
        readonly Mock<ICommSipService> SipServiceMock;
        readonly bool Presente;
        readonly bool SipAgent;
    }
}
