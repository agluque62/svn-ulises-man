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
    public enum RawSocketEventCodes { rseError, rseData }
    public class RawSocketEventArgs : EventArgs
    {
        public RawSocketEventCodes EventCode { get; set; }
        public IPAddress From { get; set; }
        public byte[] Data { get; set; }
        public Exception Excp { get; set; }
    }
    public interface IRawSocket : IDisposable
    {
        event EventHandler<RawSocketEventArgs> RawSocketEvent;
        void Start();
    }
    public class RawSocket : IRawSocket
    {
        public event EventHandler<RawSocketEventArgs> RawSocketEvent;
        public RawSocket(IPAddress ipToBind, ProtocolType protocolType)
        {
            Ip2Bind = ipToBind;
            Protocol = protocolType;
        }
        public void Start()
        {
            IgmpListener = Task.Run(() =>
            {
                PrepareSocket(Ip2Bind, Protocol, (socket) =>
                {
                    while (endEvent.WaitOne(TimeSpan.FromMilliseconds(100)) == false)
                    {
                        Byte[] ReceiveBuffer = new Byte[256];
                        EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                        try
                        {
                            var nBytes = socket.ReceiveFrom(ReceiveBuffer, ref remoteEndpoint);
                            if (nBytes > 0)
                            {
                                /** Enviar el paquete */
                                var data = new RawSocketEventArgs() { EventCode = RawSocketEventCodes.rseData, From = (remoteEndpoint as IPEndPoint)?.Address };
                                data.Data = new byte[nBytes];
                                Array.Copy(ReceiveBuffer, data.Data, nBytes);
                                RawSocketEvent?.Invoke(this, data);
                            }
                        }
                        catch (SocketException x)
                        {
                            if (x.SocketErrorCode != SocketError.TimedOut)
                            {
                                RawSocketEvent?.Invoke(this, new RawSocketEventArgs() { EventCode = RawSocketEventCodes.rseError, Excp = x });
                            }
                        }
                        catch (Exception x)
                        {

                            RawSocketEvent?.Invoke(this, new RawSocketEventArgs() { EventCode = RawSocketEventCodes.rseError, Excp = x });
                        }
                    }
                    socket?.Close();
                    socket?.Dispose();
                });
            });

        }
        public void Dispose()
        {
            endEvent?.Set();
            IgmpListener?.Wait(TimeSpan.FromSeconds(2));
        }
        void PrepareSocket(IPAddress ipToBind, ProtocolType protocolType, Action<Socket> execute)
        {
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, protocolType)
                {
                    ReceiveTimeout = 1000
                };
                socket?.Bind(new IPEndPoint(/*IPAddress.Any*/ipToBind, 0));
                socket?.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });
                execute(socket);
            }
            catch(Exception x)
            {
                RawSocketEvent?.Invoke(this, new RawSocketEventArgs() { EventCode = RawSocketEventCodes.rseError, Excp = x });
            }
        }
        Task IgmpListener { get; set; } = null;
        ManualResetEvent endEvent { get; set; } = new ManualResetEvent(false);
        IPAddress Ip2Bind { get; set; } = null;
        ProtocolType Protocol { get; set; } = ProtocolType.Igmp;
    }
    public class IgmpData
    {
        public enum IgmpPacketTypes { IgmpMembershipQuery = 0x11, IgmpMembershipReport = 0x12, IgmpMembershipReportV2 = 0x16, IgmpLeaveGroup = 0x17, IgmpUnknow=0x00 }
        static public int IgmpHeaderLength = 8;
        // IGMP queries and responses are send to the all systems address
        static public IPAddress AllSystemsAddress = IPAddress.Parse("224.0.0.1");

        public class Ipv4Header
        {
            static public int Ipv4HeaderLength = 20;
            public byte IpVersion { get; set; } = 4;    // actually only 4 bits
            public byte IpLength { get; set; } = (byte)Ipv4HeaderLength;    // actually only 4 bits
            public byte IpTypeOfService { get; set; } = 0;
            public ushort IpTotalLength { get; set; } = 0;
            public ushort IpId { get; set; } = 0;
            public ushort IpOffset { get; set; } = 0;
            public byte IpTtl { get; set; } = 1;
            public byte IpProtocol { get; set; } = 0;
            public ushort IpChecksum { get; set; } = 0;
            public IPAddress IpSourceAddress { get; set; } = IPAddress.Any;
            public IPAddress IpDestinationAddress { get; set; } = IPAddress.Any;

            public Ipv4Header(Byte [] ipv4Packet)
            {
                if (ipv4Packet.Length >= Ipv4HeaderLength)
                {
                    // Decode the data in the array back into the class properties
                    IpVersion = (byte)((ipv4Packet[0] >> 4) & 0xF);
                    IpLength = (byte)((ipv4Packet[0] & 0xF) * 4);
                    IpTypeOfService = ipv4Packet[1];
                    IpTotalLength = BitConverter.ToUInt16(ipv4Packet, 2);
                    IpId = BitConverter.ToUInt16(ipv4Packet, 4);
                    IpOffset = BitConverter.ToUInt16(ipv4Packet, 6);
                    IpTtl = ipv4Packet[8];
                    IpProtocol = ipv4Packet[9];
                    IpChecksum = BitConverter.ToUInt16(ipv4Packet, 10);

                    IpSourceAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 12));
                    IpDestinationAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 16));
                }
            }
        }
        public Ipv4Header IpData { get; set; }

        public IgmpPacketTypes IgmpVersionType { get; set; } = IgmpPacketTypes.IgmpMembershipQuery;
        public byte IgmpMaxResponseTime { get; set; } = 0;
        public ushort IgmpChecksum { get; set; } = 0;
        public IPAddress IgmpGroupAddress { get; set; } = IPAddress.Any;
        public IgmpData(byte [] packet)
        {
            int MinLen = Ipv4Header.Ipv4HeaderLength + IgmpHeaderLength;

            if (packet.Length >= MinLen)
            {
                IpData = new Ipv4Header(packet);
                int offset = IpData.IpLength;

                IgmpVersionType = ToIgmpPacketTypes(packet[offset]);
                IgmpMaxResponseTime = packet[offset+1];
                IgmpChecksum = BitConverter.ToUInt16(packet, offset+2);
            }
        }
        public override string ToString()
        {
            return $"IGMP [Type: {IgmpVersionType}, MaxResponseTime: {IgmpMaxResponseTime}, Checksum: {IgmpChecksum}]";
        }

        IgmpPacketTypes ToIgmpPacketTypes(byte code)
        {
            if (code == (byte)IgmpPacketTypes.IgmpMembershipQuery) return IgmpPacketTypes.IgmpMembershipQuery;
            if (code == (byte)IgmpPacketTypes.IgmpMembershipReport) return IgmpPacketTypes.IgmpMembershipReport;
            if (code == (byte)IgmpPacketTypes.IgmpLeaveGroup) return IgmpPacketTypes.IgmpLeaveGroup;
            if (code == (byte)IgmpPacketTypes.IgmpMembershipReportV2) return IgmpPacketTypes.IgmpMembershipReportV2;
            return IgmpPacketTypes.IgmpUnknow;
        }
    }
    public enum IgmpMasterPresenceEventCodes { impeError, impeNotify }
    public class IgmpMasterPresenceArgs : EventArgs
    {
        public IgmpMasterPresenceEventCodes Code { get; set; }
        public string Where { get; set; }
        public bool Active { get; set; }
        public Exception Error { get; set; }
    }
    public interface IIgmpManager : IDisposable 
    {
        event EventHandler<IgmpMasterPresenceArgs> IgmpMasterPresence;
        void Start(IRawSocket rs, TimeSpan timeout);
        List<string> Queriers { get; }
    }
    public class IgmpManager : IIgmpManager
    {
        public event EventHandler<IgmpMasterPresenceArgs> IgmpMasterPresence;
        public void Start(IRawSocket rawSocket2Igmp, TimeSpan timeout)
        {
            if (rawSocket2Igmp != null)
            {
                RawSocket = rawSocket2Igmp;
                InactiveTimeout = timeout;
                RawSocket.RawSocketEvent += new EventHandler<RawSocketEventArgs>((o, ev) =>
                {
                    if (ev.EventCode == RawSocketEventCodes.rseError)
                    {
                        IgmpMasterPresence?.Invoke(this, new IgmpMasterPresenceArgs() { Code = IgmpMasterPresenceEventCodes.impeError, Error = ev.Excp });
                    }
                    else if (ev.EventCode == RawSocketEventCodes.rseData)
                    {
                        IgmpData igmpPacket = new IgmpData(ev.Data);
                        if (igmpPacket.IgmpVersionType == IgmpData.IgmpPacketTypes.IgmpMembershipQuery)
                        {
                            bool isNew = false;
                            lock (Locker)
                            {
                                isNew = IgmpMasterActives.Keys.Contains(ev.From) == false;
                                IgmpMasterActives[ev.From] = DateTime.Now;
                            }
                            if (isNew)
                            {
                                IgmpMasterPresence?.Invoke(this, new IgmpMasterPresenceArgs() { Code = IgmpMasterPresenceEventCodes.impeNotify, Where = ev.From.ToString(), Active = true });
                            }
                        }
                    }
                });
                RawSocket?.Start();
                Supervisor = Task.Run(() =>
                {
                    while (SupervisorEnd.WaitOne(TimeSpan.FromSeconds(1)) == false)
                    {
                        var now = DateTime.Now;
                        IEnumerable<IPAddress> keyForDelete = null;
                        lock (Locker)
                        {
                            // Se obtienen los elementos a borrar para poder generar los eventos.
                            keyForDelete = IgmpMasterActives.Where(e => now - e.Value > InactiveTimeout).Select(e => e.Key);
                            if (keyForDelete.Count() > 0)
                            {
                                // Se actualiza el diccionario.
                                IgmpMasterActives = IgmpMasterActives.Where(e => now - e.Value <= InactiveTimeout).ToDictionary(e => e.Key, e => e.Value);
                            }
                        }
                        // Se general los eventos.
                        if (keyForDelete != null)
                        {
                            foreach (var key in keyForDelete)
                            {
                                IgmpMasterPresence?.Invoke(this, new IgmpMasterPresenceArgs() { Code = IgmpMasterPresenceEventCodes.impeNotify, Where = key.ToString(), Active = false });
                            }
                        }
                    }
                });
            }
            else
            {
                IgmpMasterPresence?.Invoke(this, new IgmpMasterPresenceArgs() { Code = IgmpMasterPresenceEventCodes.impeError, Error = new Exception("No se puede pasar el SOCKET con valor NULL") });
            }
        }
        public void Dispose()
        {
            RawSocket?.Dispose();
            SupervisorEnd?.Set();
            Supervisor?.Wait(TimeSpan.FromSeconds(2));
            RawSocket = null;
        }
        public List<string> Queriers
        {
            get
            {
                List<string> queriers = null;
                lock (Locker)
                {
                    queriers = IgmpMasterActives.Select(m => m.Key.ToString()).ToList();
                }
                return queriers;
            }
        }
        public override string ToString()
        {
            return string.Join("##", Queriers.ToArray());
        }
        private Dictionary<IPAddress, DateTime> IgmpMasterActives { get; set; } = new Dictionary<IPAddress, DateTime>();
        IRawSocket RawSocket { get; set; } = null;
        Task Supervisor { get; set; } = null;
        ManualResetEvent SupervisorEnd { get; set; } = new ManualResetEvent(false);
        Object Locker { get; set; } = new object();
        TimeSpan InactiveTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
