using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities
{
    public class RawData : EventArgs
    {
    }
    public interface IRawSocket : IDisposable
    {
        //event EventHandler<RawData> Data;
    }

    public class IgmpSocket : IRawSocket
    {
        public IgmpSocket()
        {
            IgmpListener = Task.Run(() =>
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp)
                {
                    ReceiveTimeout = 1000
                };
                socket?.Bind(new IPEndPoint(IPAddress.Any, 0));
                //socket?.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });
                SetSocketOption(socket);
                while (endEvent.WaitOne(TimeSpan.FromMilliseconds(100)) == false)
                {
                    Byte[] ReceiveBuffer = new Byte[256];
                    EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    try
                    {
                        var nBytes = socket.ReceiveFrom(ReceiveBuffer, ref remoteEndpoint);
                        if (nBytes > 0)
                        {
                            /** todo. enviar el paquete */
                        }
                    }
                    catch (Exception x)
                    {
                    }
                    socket?.Close();
                    socket?.Dispose();
                }
            });
        }
        public void Dispose()
        {
            endEvent?.Set();
            IgmpListener?.Wait(TimeSpan.FromSeconds(2));
        }
        private bool SetSocketOption(Socket socket)//Set raw socket
        {
            bool ret_value = true;
            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
                byte[] IN = new byte[4] { 1, 0, 0, 0 };
                byte[] OUT = new byte[4];
                //Low-level operation mode, accept all data packets, this step is the key, you must set the socket to raw and IP Level to be available SIO_RCVALL
                int ret_code = socket.IOControl(IOControlCode.ReceiveAll, IN, OUT);
                ret_code = OUT[0] + OUT[1] + OUT[2] + OUT[3];//Combine 4 8-bit bytes into a 32-bit integer
                if (ret_code != 0) ret_value = false;
            }
            catch (SocketException x)
            {
                ret_value = false;
            }
            return ret_value;
        }
        Task IgmpListener { get; set; } = null;
        ManualResetEvent endEvent { get; set; } = new ManualResetEvent(false);
    }


    public class IgmpMasterPresenceArgs : EventArgs
    {
        public string Where { get; set; }
        public bool Active { get; set; }
    }
    public class IgmpManager : IDisposable
    {
        public event EventHandler<IgmpMasterPresenceArgs> IgmpMasterPresence;
        public void Create(IRawSocket rawSocket2Igmp)
        {
            this.RawSocket = rawSocket2Igmp;
        }
        public void Dispose()
        {
            RawSocket?.Dispose();
        }

        class IgmpMaster
        {
            public IPAddress Id { get; set; }
            public DateTime LastPresence { get; set; }
        }
        private List<IgmpMaster> IgmpMasterActives { get; set; } = new List<IgmpMaster>();
        IRawSocket RawSocket { get; set; } = null;
    }
}
