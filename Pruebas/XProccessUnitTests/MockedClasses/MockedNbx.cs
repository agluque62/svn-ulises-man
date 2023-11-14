using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


using U5kManServer;

namespace XProccessUnitTests.MockedClasses
{
    public class MockedNbx : IDisposable
    {
        public IDataService DataService => dataServiceMock?.Object;
        public IRawUdpCommService UdpService => udpServiceMock?.Object;
        public ICommHttpService HtppService => httpServiceMock?.Object;
        public bool IsMaster { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public MockedNbx()
        {
            dataServiceMock = new Mock<IDataService>();
            udpServiceMock = new Mock<IRawUdpCommService>();
            httpServiceMock = new Mock<ICommHttpService>();
            automaton = Task.Run(() =>
            {
                while (cts.IsCancellationRequested == false)
                {
                    Task.Delay(2000).Wait();
                    if (IsActive)
                    {
                        var data = nbxEventdata;
                        RaiseNbxEvent(data);
                    }
                    Debug.WriteLine("Automaton Tick");
                }
                Debug.WriteLine("Automaton exit");
            });
            SetupDataService();
            SetupUdpServiceMock();
            SetupHttpServiceMock();
        }
        public void Dispose()
        {
            cts.Cancel();
            automaton.Wait();
        }

        void SetupDataService()
        {
            dataServiceMock.Setup(x => x.IsMaster)
                .Returns(() => IsMaster);
        }
        void SetupUdpServiceMock()
        {
        }
        void SetupHttpServiceMock()
        {
            httpServiceMock.Setup(proc => proc.Get(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns<string, TimeSpan, string>((url, timeout, def) => Task.Run(() => SimulatedHttpData(IsActive, url)));
        }
        void RaiseNbxEvent(string data)
        {
            Debug.WriteLine("Raising Nbx event");
            udpServiceMock.Raise(
                x => x.DataReceived += null, new RawUdpCommServiceArgs() { from = "127.0.0.1", data = Encoding.UTF8.GetBytes(data) });
        }
        QueryServiceResult<string> SimulatedHttpData(bool presente, string url)
        {
            string response =
                url.Contains("/rdsessions") ? rdSessionsData :
                url.Contains("/gestormn") ? rdMNData :
                url.Contains("/rdhf") ? rdHFData : 
                url.Contains("/rd11") ? rd11Data : 
                url.Contains("/tifxinfo") ? tifxData : phoneData;
            return new QueryServiceResult<string>(presente, response);
        }

        string MockedData(string strSection)
        {
            var strData = System.IO.File.ReadAllText("nbxdata.json");
            var parent = JObject.Parse(strData);
            var section = parent[strSection];
            return section.ToString();
        }
        readonly Mock<IDataService> dataServiceMock;
        readonly Mock<IRawUdpCommService> udpServiceMock;
        readonly Mock<ICommHttpService> httpServiceMock;
        CancellationTokenSource cts = new CancellationTokenSource();
        Task automaton = null;

        string nbxEventdata => MockedData("nbx");
        string rdSessionsData => MockedData("sesiones");
        string rdMNData => MockedData("mn");
        string rdHFData => MockedData("hf");
        string rd11Data => MockedData("unomasuno");
        string tifxData => MockedData("tifx");
        string phoneData = "{}";
    }
}
