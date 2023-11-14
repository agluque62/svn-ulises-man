using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Lextm.SharpSnmpLib;

using U5kManServer;
using System.Net;

namespace XProccessUnitTests.MockedClasss
{
    public class MockedGw
    {
        public IDataService DataService => DataServiceMock.Object;
        public IPingService PingService => PingServiceMock.Object;
        public ICommSipService SipService => SipServiceMock.Object;
        public ICommSnmpService SnmpService => SnmpServiceMock.Object;
        public ICommHttpService HttpService => HttpServiceMock.Object;
        public MockedGw(bool presente = true, bool sipagent = true, bool snmpagent = true, bool httpserver = true)
        {
            DataServiceSetup();
            PingServiceSetup(presente);
            SipServiceSetup(sipagent);
            SnmpServiceSetup(snmpagent);
            HttpServiceSetup(httpserver);
        }
        public void RaiseTrap()
        {
            SnmpServiceMock.Raise(x => x.TrapReceived += null,
                new TrapBus.TrapEventArgs()
                {
                    From = new IPEndPoint(IPAddress.Parse("11.12.91.101"), 161),
                    TrapOid = ".1.3.6.1.4.1.7916.8.3.2.1.5", // Historico
                    VarOid =  ".1.3.6.1.4.1.7916.8.3.2.1.7.0",
                    VarData = new OctetString("hola")
                });
        }

        void DataServiceSetup()
        {
            DataServiceMock = new Mock<IDataService>();
            DataServiceMock.Setup(library => library.Data)
                .Returns(mockedData);
            DataServiceMock.Setup(x => x.IsMaster)
                .Returns(true);
        }
        void PingServiceSetup(bool active)
        {
            PingServiceMock = new Mock<IPingService>();
            PingServiceMock
                .Setup(proc => proc.Ping(
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .Returns(Task.Run(() => active));
        }
        void SipServiceSetup(bool active)
        {
            SipServiceMock = new Mock<ICommSipService>();
            SipServiceMock
                .Setup(proc => proc.Ping(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>()))
                .Returns(Task.Run(() => new QueryServiceResult<string>(active, "200")));
        }
        void SnmpServiceSetup(bool active)
        {
            SnmpServiceMock = new Mock<ICommSnmpService>();
            SnmpServiceMock.Setup(x => x.GetData(It.IsAny<object>(), It.IsAny<IList<Variable>>()))
                .Returns<object, IList<Variable>>((from, input) => Task.Run(() => SimulatedSnmpData(active, input)));
        }
        void HttpServiceSetup(bool active)
        {
            HttpServiceMock = new Mock<ICommHttpService>();
            HttpServiceMock.Setup(proc => proc.Get(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns<string, TimeSpan>((url, timeout) => Task.Run(() => SimulatedHttpData(active, url)));
        }

        U5kManStdData mockedData => new U5kManStdData()
        {
            CFGGWS = new List<stdGw>()
            {
                new stdGw(null)
                {
                    name = "mockedCGW",
                    ip = "11.12.91.10",
                     gwA = new stdPhGw()
                     {
                         name = "mockedCGW_A",
                         ip = "11.12.91.101",
                         ParentName = "mockedCGW",
                         snmpport=161,
                         slots = new stdSlot[]
                         {
                             new stdSlot()
                             {
                                 std_cfg = std.Ok,
                                 rec = new stdRec[]
                                 {
                                    new stdRec() {name="R00", bdt_name="R00", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R01", bdt_name="R01", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R02", bdt_name="R02", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R03", bdt_name="R03", tipo=0, tipo_itf=0 },
                                 }
                             },
                             new stdSlot()
                             {
                                 std_cfg = std.Ok,
                                 rec = new stdRec[]
                                 {
                                    new stdRec() {name="R10", bdt_name="R10", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R11", bdt_name="R11", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R12", bdt_name="R12", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R13", bdt_name="R13", tipo=0, tipo_itf=0 },
                                 }
                             },
                             new stdSlot()
                             {
                                 std_cfg = std.Ok,
                                 rec = new stdRec[]
                                 {
                                    new stdRec() {name="R20", bdt_name="R20", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R21", bdt_name="R21", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R22", bdt_name="R22", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R23", bdt_name="R23", tipo=0, tipo_itf=0 },
                                 }
                             },
                             new stdSlot()
                             {
                                 std_cfg = std.Ok,
                                 rec = new stdRec[]
                                 {
                                    new stdRec() {name="R30", bdt_name="R30", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R31", bdt_name="R31", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R32", bdt_name="R32", tipo=0, tipo_itf=0 },
                                    new stdRec() {name="R33", bdt_name="R33", tipo=0, tipo_itf=0 },
                                 }
                             },
                         }
                     }
                }
            }
        };
        QueryServiceResult<IList<Variable>> SimulatedSnmpData(bool present, IList<Variable> input)
        {
            return new QueryServiceResult<IList<Variable>>(
                    present,
                    (IList<Variable>)SnmpData.Where(d => input.Where(i => i.Id == d.Id).Count() > 0).ToList()
                );
        }
        QueryServiceResult<string> SimulatedHttpData(bool presente, string url)
        {
            string response =
                url.Contains("/test") ? "...Handler por Defecto..." :
                url.Contains("/mant/lver") ? "{}" :
                url.Contains("/ntpstatus") ? "{ lines:[] }" : "???";
            return new QueryServiceResult<string>(presente, response);
        }

        Mock<IDataService> DataServiceMock;
        Mock<IPingService> PingServiceMock;
        Mock<ICommSipService> SipServiceMock;
        Mock<ICommSnmpService> SnmpServiceMock;
        Mock<ICommHttpService> HttpServiceMock;
        List<Variable> SnmpData = new List<Variable>()
        {
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.2.0")),    // 0 => Estado Hw.
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.6.0")),    // 1 => Estado LAN1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.7.0")),    // 2 => Estado LAN2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.8.0")),    // 3 => Estado P/R,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.4.0")),    // 4 => Estado FA,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.1.0")),    // 5 => Identificador. Habilita el envio de TRAPS
                                                                                    // 6 => Slot 0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.1")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.1")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.1")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.1")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.1")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.1")),   // Canal-3
                                                                                       // 12 => Slot 1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.2")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.2")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.2")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.2")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.2")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.2")),   // Canal-3
                                                                                       // 18 => Slot 2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.3")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.3")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.3")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.3")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.3")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.3")),   // Canal-3
                                                                                       // 24 => Slot 3
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.4")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.4")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.4")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.4")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.4")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.4")),   // Canal-3
                                                                                       // 30 => Recurso 0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.1")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.1")),  // Status Interfaz.
                                                                                       // 32 => Recurso 1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.2")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.2")),  // Status Interfaz.
                                                                                       // 34 => Recurso 2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.3")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.3")),  // Status Interfaz.
                                                                                       // 36 => Recurso 3
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.4")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.4")),  // Status Interfaz.
                                                                                       // 38 => Recurso 4
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.5")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.5")),  // Status Interfaz.
                                                                                       // 40 => Recurso 5
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.6")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.6")),  // Status Interfaz.
                                                                                       // 42 => Recurso 6
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.7")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.7")),  // Status Interfaz.
                                                                                       // 44 => Recurso 7
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.8")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.8")),  // Status Interfaz.
                                                                                       // 46 => Recurso 8
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.9")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.9")),  // Status Interfaz.
                                                                                       // 48 => Recurso 9
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.10")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.10")),  // Status Interfaz.
                                                                                        // 50 => Recurso 10
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.11")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.11")),  // Status Interfaz.
                                                                                        // 52 => Recurso 11
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.12")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.12")),  // Status Interfaz.
                                                                                        // 54 => Recurso 12
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.13")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.13")),  // Status Interfaz.
                                                                                        // 56 => Recurso 13
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.14")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.14")),  // Status Interfaz.
                                                                                        // 58 => Recurso 14
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.15")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.15")),  // Status Interfaz.
                                                                                        // 60 => Recurso 15
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.16")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.16")),  // Status Interfaz.
        };
    }
}
