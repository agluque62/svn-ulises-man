using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lextm.SharpSnmpLib.Messaging;
using Moq;
using U5kManServer;
using Utilities;

namespace XProccessUnitTests.MockedClasss
{
    public class MockedPbx
    {
        #region Publicos

        public IDataService DataService => DataServiceMock?.Object;
        public IPingService PingService => PingServiceMock?.Object;
        public ICommFtpService FtpService => FtpServiceMock?.Object;
        public IPbxWsService PbxWsService => PbxWsServiceMock?.Object;

        public bool InDb {  get; set; }
        public string Ip { get; set; }
        public bool Reachable {  get; set; }
        public bool Connected { get; set; }
        public bool WithVersion { get; set; }

        public void RaiseWsOpenEvent()
        {
            PbxWsServiceMock.Raise(
                x => x.WsOpen += null, new EventArgs());
        }
        public void RaiseWsClosedEvent()
        {
            PbxWsServiceMock.Raise(
                x => x.WsClosed += null, new EventArgs());
        }
        public void RaiseWsErrorEvent(Exception exception)
        {
            PbxWsServiceMock.Raise(
                x => x.WsError += null, new WsErrorEventArgs() { Exception = exception });
        }
        public void RaiseWsMessageEvent()
        {
            PbxWsServiceMock.Raise(
                x => x.WsMessage += null, new WsMessageEventArgs() { Message = "None"});
        }

        #endregion

        public MockedPbx() 
        {
            DataServiceMock = new Mock<IDataService>();
            PingServiceMock = new Mock<IPingService>();
            PbxWsServiceMock = new Mock<IPbxWsService>();
            FtpServiceMock = new Mock<ICommFtpService>();

            InDb = false;
            Ip = "127.0.0.1";
            Reachable = false;
            Connected = false;
            WithVersion = false;

            DataServiceSetup();
            PingServiceSetup();
            PbxWsServiceSetup();
            FtpServiceSetup();
        }

        #region Privados

        void DataServiceSetup()
        {
            DataServiceMock.Setup(method => method.Data)
                .Returns(new U5kManStdData()
                {
                });
            DataServiceMock.Setup(prop => prop.IsMaster)
                .Returns(true);
            DataServiceMock.Setup(prop => prop.IsTherePbx)
                .Returns(() => InDb);
            DataServiceMock.Setup(prop => prop.PbxIp) 
                .Returns(() => Ip);
        }
        void PingServiceSetup()
        {
            PingServiceMock
                .Setup(method => method.Ping(
                    It.IsAny<string>(),
                    It.IsAny<bool>()).Result)
                .Returns(() => Reachable); 
        }
        void PbxWsServiceSetup()
        {
            PbxWsServiceMock
                .Setup(
                    prop => prop.Url)
                .Returns(() => RuntimeUrl);
            PbxWsServiceMock
                .Setup(
                    method => method.Connect(It.IsAny<string>()))
                .Callback((string ip) =>
                {
                    RuntimeUrl = $"ws://{ip}:8080/pbx/ws?login_user=mock&login_password=none&user=*&registered=True&status=True&line=*";
                })
                .Returns<string>((ip) => Task.Run(() => { }));
            PbxWsServiceMock
                .Setup(
                    method => method.Open().Result)
                .Callback(() => { RaisePostOpenEvents(); })
                .Returns(() => Reachable); // Todo. Conditional and delayed event (close, open or error)
            PbxWsServiceMock
                .Setup(
                    method => method.Close())
                .Callback(() => { RaisePostCloseEvents(); }) // Todo. Conditional and delayed event (close, open or error)
                .Returns(() => Task.Run(() => { }));
        }
        void FtpServiceSetup()
        {
            FtpServiceMock
                .Setup(
                    method => method.Download(It.IsAny<string>(), It.IsAny<string>()).Result
                    )
                .Returns(() => new QueryServiceResult<string>() { Success = true, Result = "" });
            FtpServiceMock
                .Setup(
                    method => method.Download(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Result
                    )
                .Returns(() => new QueryServiceResult<Exception>() { Success = WithVersion, Result = new Exception("Mocked Ftp Exception") });
        }

        void RaisePostOpenEvents()
        {
            Task.Run(() =>
            {
                if (Connected)
                {
                    Task.Delay(200).Wait();
                    RaiseWsOpenEvent();
                }
                else
                {
                    RaiseWsClosedEvent();
                    Task.Delay(5000).Wait();
                    RaiseWsErrorEvent(new Exception("Mocked WS Exception"));
                }
            });
        }
        void RaisePostCloseEvents()
        {
            Task.Run(() =>
            {
                if (Connected)
                {
                    Task.Delay(200).Wait();
                    RaiseWsClosedEvent();
                }
                else
                {
                    Task.Delay(5000).Wait();
                    RaiseWsErrorEvent(new Exception("Mocked WS Exception"));
                }
            });
        }

        string RuntimeUrl { get; set; } = "none";
        readonly Mock<IDataService> DataServiceMock;
        readonly Mock<IPingService> PingServiceMock;
        readonly Mock<IPbxWsService> PbxWsServiceMock;
        readonly Mock<ICommFtpService> FtpServiceMock;

        #endregion
    }
}
