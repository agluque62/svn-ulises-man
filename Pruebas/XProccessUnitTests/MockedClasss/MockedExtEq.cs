using System;
using System.Collections.Generic;
using System.Linq;
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

        public MockedExtEq() 
        {
            ProcessDataMock = new Mock<IProcessData>();
            ProcessPingMock = new Mock<IProcessPing>();
            ProcessSipMock = new Mock<IProcessSip>();
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

        }
        void SetupProcessSip()
        {

        }

        U5kManStdData mockedData => new U5kManStdData()
        {
        };
        readonly Mock<IProcessData> ProcessDataMock;
        readonly Mock<IProcessPing> ProcessPingMock;
        readonly Mock<IProcessSip> ProcessSipMock;
    }
}
