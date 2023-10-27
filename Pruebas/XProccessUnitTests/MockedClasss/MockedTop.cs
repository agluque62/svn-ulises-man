using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Moq;
using U5kManServer;
using U5kManServer.Procesos;

namespace XProccessUnitTests.MockedClass
{
    public class MockedTop
    {
        public IProcessData ProcessData => ProcessDataMock.Object;

        public MockedTop()
        {
            ProcessDataMock = new Mock<IProcessData>();
            SetupProcessData();
        }
        void SetupProcessData()
        {
            ProcessDataMock.Setup(library => library.Data)
                .Returns(mockedData);
            ProcessDataMock.Setup(x => x.IsMaster)
                .Returns(true);
        }
        U5kManStdData mockedData => new U5kManStdData()
        {
            CFGTOPS = new List<stdPos>()
            {
                new stdPos(){name = "MOCKEDTOP01", ip = "127.0.0.1", SectorOnPos = "**FS**", snmpport = 161, uris = new List<string>(){ "uri1" } }
            }
        };
        readonly Mock<IProcessData> ProcessDataMock;
    }
}
