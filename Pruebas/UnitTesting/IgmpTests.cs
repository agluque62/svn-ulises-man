using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

using Utilities;

namespace UnitTesting
{
    [TestClass]
    public class IgmpTests
    {
        [TestMethod]
        public void RawSocketTestMethod1()
        {
            Debug.WriteLine($"{DateTime.Now}: Comienza la Prueba.");
            //var ip = IPAddress.Parse("10.34.52.84");
            //var ip = IPAddress.Parse("192.168.1.39");
            var ip = IPAddress.Parse("11.21.94.2");
            var s = new RawSocket(ip, ProtocolType.Igmp);
            s.RawSocketEvent += new EventHandler<RawSocketEventArgs>((o, ev) =>
            {
                if (ev.EventCode== RawSocketEventCodes.rseError)
                {
                    Debug.WriteLine($"{DateTime.Now}: RawSocketException => {ev.Excp.Message}");
                }
                else if (ev.EventCode== RawSocketEventCodes.rseData)
                {
                    IgmpData igmpPacket = new IgmpData(ev.Data);
                    Debug.WriteLine($"{DateTime.Now}: DataReceived from {ev.From} => {igmpPacket}");
                }
            });
            s.Start();
            Task.Delay(TimeSpan.FromMinutes(5)).Wait();
            s.Dispose();
            Debug.WriteLine($"{DateTime.Now}: Fin de la Prueba.");
        }
        [TestMethod]
        public void IgmpManagerTestMethod1()
        {
            Debug.WriteLine($"{DateTime.Now}: Comienza la Prueba.");
            var ip = IPAddress.Parse("11.21.94.2");
            using (var manager = new IgmpManager())
            using (var s = new RawSocket(ip, ProtocolType.Igmp))
            {
                manager.IgmpMasterPresence += new EventHandler<IgmpMasterPresenceArgs>((o, e) =>
                {
                    switch (e.Code)
                    {
                        case IgmpMasterPresenceEventCodes.impeError:
                            Debug.WriteLine($"{DateTime.Now}: IgmpMasterPresenceException => {e.Error.Message}");
                            break;
                        case IgmpMasterPresenceEventCodes.impeNotify:
                            Debug.WriteLine($"{DateTime.Now}: IGMP Querier on {e.Where} => {e.Active}. Queriers => {manager}");
                            break;
                    }
                });
                manager.Start(s, TimeSpan.FromSeconds(20));
                Task.Delay(TimeSpan.FromMinutes(5)).Wait();
            }
            Debug.WriteLine($"{DateTime.Now}: Fin de la Prueba.");
        }
        public static string ByteArrayToHexadecimalString(byte[] byteArray)
        {
            StringBuilder hexadecimalString = new StringBuilder(byteArray.Length * 2);
            foreach (byte b in byteArray)
            {
                hexadecimalString.AppendFormat("{0:x2}-", b);
            }
            return hexadecimalString.ToString();
        }
    }
}
