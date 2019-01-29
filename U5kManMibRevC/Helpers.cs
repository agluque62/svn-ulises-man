using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Management;

namespace U5kManMibRevC
{
    public class MibIITableHelpers
    {
        public class IpHlpApiHelper
        {
            public delegate int TableDataGetDel(IntPtr pTable, ref int dwsize, bool bOrder);

            // The insufficient buffer error.
            const int ERROR_INSUFFICIENT_BUFFER = 122;

            /** Datos Grupo IF */
            //public struct MIB_IFROW
            //{
            //    WCHAR wszName[MAX_INTERFACE_NAME_LEN];
            //    IF_INDEX dwIndex;
            //    IFTYPE dwType;
            //    DWORD dwMtu;
            //    DWORD dwSpeed;
            //    DWORD dwPhysAddrLen;
            //    UCHAR bPhysAddr[MAXLEN_PHYSADDR];
            //    DWORD dwAdminStatus;
            //    INTERNAL_IF_OPER_STATUS dwOperStatus;
            //    DWORD dwLastChange;
            //    DWORD dwInOctets;
            //    DWORD dwInUcastPkts;
            //    DWORD dwInNUcastPkts;
            //    DWORD dwInDiscards;
            //    DWORD dwInErrors;
            //    DWORD dwInUnknownProtos;
            //    DWORD dwOutOctets;
            //    DWORD dwOutUcastPkts;
            //    DWORD dwOutNUcastPkts;
            //    DWORD dwOutDiscards;
            //    DWORD dwOutErrors;
            //    DWORD dwOutQLen;
            //    DWORD dwDescrLen;
            //    UCHAR bDescr[MAXLEN_IFDESCR];
            //}

            /** Datos Grupo IP */
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_IPSTATS
            {
                public Int32 dwForwarding;
                public Int32 dwDefaultTTL;
                public Int32 dwInReceives;
                public Int32 dwInHdrErrors;
                public Int32 dwInAddrErrors;
                public Int32 dwForwDatagrams;
                public Int32 dwInUnknownProtos;
                public Int32 dwInDiscards;
                public Int32 dwInDelivers;
                public Int32 dwOutRequests;
                public Int32 dwRoutingDiscards;
                public Int32 dwOutDiscards;
                public Int32 dwOutNoRoutes;
                public Int32 dwReasmTimeout;
                public Int32 dwReasmReqds;
                public Int32 dwReasmOks;
                public Int32 dwReasmFails;
                public Int32 dwFragOks;
                public Int32 dwFragFails;
                public Int32 dwFragCreates;
                public Int32 dwNumIf;
                public Int32 dwNumAddr;
                public Int32 dwNumRoutes;
            };
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_IPADDRROW_W2K
            {
                public UInt32 dwAddr;
                public UInt32 dwIndex;
                public UInt32 dwMask;
                public UInt32 dwBCastAddr;
                public UInt32 dwReasmSize;
                public UInt16 unused1;
                public UInt16 unused2;
            };
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_IPFORWARDROW
            {
                public UInt32 dwForwardDest;
                public UInt32 dwForwardMask;
                public UInt32 dwForwardPolicy;
                public UInt32 dwForwardNextHop;
                public UInt32 dwForwardIfIndex;
                public UInt32 ForwardType;
                public UInt32 dwForwardProto;
                public UInt32 dwForwardAge;
                public UInt32 dwForwardNextHopAS;
                public UInt32 dwForwardMetric1;
                public UInt32 dwForwardMetric2;
                public UInt32 dwForwardMetric3;
                public UInt32 dwForwardMetric4;
                public UInt32 dwForwardMetric5;
            };
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_IPNETROW
            {
                [MarshalAs(UnmanagedType.U4)]
                public int dwIndex;
                [MarshalAs(UnmanagedType.U4)]
                public int dwPhysAddrLen;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac0;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac1;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac2;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac3;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac4;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac5;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac6;
                [MarshalAs(UnmanagedType.U1)]
                public byte mac7;
                [MarshalAs(UnmanagedType.U4)]
                public int dwAddr;
                [MarshalAs(UnmanagedType.U4)]
                public int dwType;
            }

            /** Datos Grupo TCP */
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_TCPSTATS
            {
                public Int32 dwRtoAlgorithm;
                public Int32 dwRtoMin;
                public Int32 dwRtoMax;
                public Int32 dwMaxConn;
                public Int32 dwActiveOpens;
                public Int32 dwPassiveOpens;
                public Int32 dwAttemptFails;
                public Int32 dwEstabResets;
                public Int32 dwCurrEstab;
                public Int32 dwInSegs;
                public Int32 dwOutSegs;
                public Int32 dwRetransSegs;
                public Int32 dwInErrs;
                public Int32 dwOutRsts;

                public Int32 dwNumConns;
            };
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_TCPROW
            {
                public Int32 dwState;
                public UInt32 dwLocalAddr;
                public Int32 dwLocalPort;
                public UInt32 dwRemoteAddr;
                public Int32 dwRemotePort;
            };

            /** Datos Grupo UDP */
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_UDPSTATS
            {
                public Int32 dwInDatagrams;
                public Int32 dwNoPorts;
                public Int32 dwInErrors;
                public Int32 dwOutDatagrams;
                public Int32 dwNumAddrs;
            };
            [StructLayout(LayoutKind.Sequential)]
            public struct MIB_UDPROW
            {
                public Int32 dwLocalAddr;
                public Int32 dwLocalPort;
            }

            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetIpStatistics(ref MIB_IPSTATS pStats);
            [DllImport("IpHlpApi.dll")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static extern int GetIpAddrTable(
                IntPtr pIpNetTable,
                [MarshalAs(UnmanagedType.U4)] ref int pdwSize,
                bool bOrder);
            [DllImport("IpHlpApi.dll")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static extern int GetIpNetTable(
                IntPtr pIpNetTable,
                [MarshalAs(UnmanagedType.U4)] ref int pdwSize,
                bool bOrder);
            [DllImport("IpHlpApi.dll")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static extern int GetIpForwardTable(
                IntPtr pIpNetTable,
                [MarshalAs(UnmanagedType.U4)] ref int pdwSize,
                bool bOrder);

            [DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int FreeMibTable(IntPtr plpNetTable);

            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetTcpStatistics(ref MIB_TCPSTATS pStats);
            [DllImport("IpHlpApi.dll")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static extern int GetTcpTable(
                IntPtr pTcpTable,
                [MarshalAs(UnmanagedType.U4)] ref int pdwSize,
                bool bOrder);

            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetUdpStatistics(ref MIB_UDPSTATS pStats);
            [DllImport("IpHlpApi.dll")]
            [return: MarshalAs(UnmanagedType.U4)]
            public static extern int GetUdpTable(
                IntPtr pTcpTable,
                [MarshalAs(UnmanagedType.U4)] ref int pdwSize,
                bool bOrder);

            /// <summary>
            /// 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="TableDataGet"></param>
            /// <returns></returns>
            public static List<T> Table<T>(TableDataGetDel TableDataGet) 
            {
                int bytesNeeded = 0;
                // The result from the API call.
                int result = TableDataGet(IntPtr.Zero, ref bytesNeeded, false);
                // Call the function, expecting an insufficient buffer.
                if (result != IpHlpApiHelper.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception(result);
                }
                // Allocate the memory, do it in a try/finally block, to ensure
                // that it is released.
                IntPtr buffer = IntPtr.Zero;
                // Try/finally.
                try
                {
                    // Allocate the memory.
                    buffer = Marshal.AllocCoTaskMem(bytesNeeded);
                    // Make the call again. If it did not succeed, then
                    // raise an error.
                    result = TableDataGet(buffer, ref bytesNeeded, false);
                    // If the result is not 0 (no error), then throw an exception.
                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }
                    // Now we have the buffer, we have to marshal it. We can read
                    // the first 4 bytes to get the length of the buffer.
                    int entries = Marshal.ReadInt32(buffer);

                    // Increment the memory pointer by the size of the int.
                    IntPtr currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(int)));

                    // Allocate an array of entries.
                    T[] table = new T[entries];

                    // Cycle through the entries.
                    for (int index = 0; index < entries; index++)
                    {
                        // Call PtrToStructure, getting the structure information.
                        table[index] = (T)Marshal.PtrToStructure(
                            new IntPtr(currentBuffer.ToInt64() + (index *
                           Marshal.SizeOf(typeof(T)))), typeof(T));
                    }
                    return table.OfType<T>().ToList();
                }
                finally
                {
                    // Release the memory.
                    IpHlpApiHelper.FreeMibTable(buffer);
                }
            }
        }

        //public class IpAddrTable
        //{
        //    public class IpAddTableEntry
        //    {
        //        public string IpAdEntAddr { get; set; }
        //        public int IpAdEntIfIndex { get; set; }
        //        public string IpAdEntNetMask { get; set; }
        //        public int IpAdEntBcastAddr { get; set; }
        //        public int IpAdEntReasmMaxSize { get; set; }
        //    }
        //    static public List<IpAddTableEntry> Data
        //    {
        //        get
        //        {
        //            var lst = new List<IpAddTableEntry>();
        //            int index = 1;
        //            var adapters = NetworkInterface.GetAllNetworkInterfaces();
        //            foreach (NetworkInterface adapter in adapters)
        //            {
        //                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
        //                {
        //                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
        //                    {
        //                        lst.Add(new IpAddTableEntry
        //                        {
        //                            IpAdEntAddr = unicastIPAddressInformation.Address.ToString(),
        //                            IpAdEntIfIndex = index,
        //                            IpAdEntNetMask = unicastIPAddressInformation.IPv4Mask.ToString(),
        //                            IpAdEntBcastAddr = 1,
        //                            IpAdEntReasmMaxSize = 65535
        //                        });
        //                    }
        //                }
        //                index++;
        //            }
        //            return lst;

        //        }
        //    }
        //}
        //public class IpRouteTable
        //{
        //    public class IpRouteTableEntry
        //    {
        //        public string IpRouteDest { get; set; }
        //        public Int32 IpRouteIfIndex { get; set; }
        //        public Int32 IpRouteMetric1 { get; set; }
        //        public Int32 IpRouteMetric2 { get; set; }
        //        public Int32 IpRouteMetric3 { get; set; }
        //        public Int32 IpRouteMetric4 { get; set; }
        //        public Int32 IpRouteMetric5 { get; set; }
        //        public string IpRouteNextHop { get; set; }
        //        public Int32 IpRouteType { get; set; }
        //        public Int32 IpRouteProto { get; set; }
        //        public Int32 IpRouteAge { get; set; }
        //        public string IpRouteMask { get; set; }
        //        public string IpRouteInfo { get; set; }
        //    };

        //    static public List<IpRouteTableEntry> Data
        //    {
        //        get
        //        {
        //            var lst = new List<IpRouteTableEntry>();
        //            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2",        
        //                "SELECT * FROM Win32_IP4RouteTable");
        //            var routeTable = searcher.Get().Cast<ManagementObject>().ToList();
        //            routeTable.ForEach(e =>
        //            {
        //                try
        //                {
        //                    var item = new IpRouteTableEntry()
        //                    {
        //                        IpRouteDest = e["Destination"].ToString(),
        //                        IpRouteIfIndex = Convert.ToInt32(e["InterfaceIndex"]),
        //                        IpRouteMetric1 = Convert.ToInt32(e["Metric1"]),
        //                        IpRouteMetric2 = Convert.ToInt32(e["Metric2"]),
        //                        IpRouteMetric3 = Convert.ToInt32(e["Metric3"]),
        //                        IpRouteMetric4 = Convert.ToInt32(e["Metric4"]),
        //                        IpRouteMetric5 = Convert.ToInt32(e["Metric5"]),
        //                        IpRouteNextHop = e["NextHop"].ToString(),
        //                        IpRouteType = Convert.ToInt32(e["Type"]),
        //                        IpRouteProto = Convert.ToInt32(e["Protocol"]),
        //                        IpRouteAge = Convert.ToInt32(e["Age"]),
        //                        IpRouteMask = e["Mask"].ToString(),
        //                        IpRouteInfo = e["Information"].ToString(),
        //                    };

        //                    lst.Add(item);
        //                }
        //                catch (Exception )
        //                {
        //                }
        //            });

        //            return lst;
        //        }
        //    }
        //}
        //public class IpNetTable
        //{
        //    // The max number of physical addresses.
        //    const int MAXLEN_PHYSADDR = 8;
        //    // Define the MIB_IPNETROW structure.
        //    [StructLayout(LayoutKind.Sequential)]
        //    struct MIB_IPNETROW
        //    {
        //        [MarshalAs(UnmanagedType.U4)]
        //        public int dwIndex;
        //        [MarshalAs(UnmanagedType.U4)]
        //        public int dwPhysAddrLen;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac0;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac1;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac2;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac3;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac4;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac5;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac6;
        //        [MarshalAs(UnmanagedType.U1)]
        //        public byte mac7;
        //        [MarshalAs(UnmanagedType.U4)]
        //        public int dwAddr;
        //        [MarshalAs(UnmanagedType.U4)]
        //        public int dwType;
        //    }

        //    // Clase de datos convertidos.
        //    public class IpNetToMediaEntry
        //    {
        //        public Int32 IpNetToMediaIfIndex { get; set; }
        //        public string IpNetToMediaPhysAddress { get; set; }
        //        public string IpNetToMediaNetAddress { get; set; }
        //        public Int32 IpNetToMediaType { get; set; }
        //    }

        //    static public List<IpNetToMediaEntry> Data
        //    {
        //        get
        //        {
        //            var data = new List<IpNetToMediaEntry>();
        //            int bytesNeeded = 0;
        //            // The result from the API call.
        //            int result = IpHlpApiHelper.GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);
        //            // Call the function, expecting an insufficient buffer.
        //            if (result != IpHlpApiHelper.ERROR_INSUFFICIENT_BUFFER)
        //            {
        //                // Throw an exception.
        //                throw new Win32Exception(result);
        //            }

        //            // Allocate the memory, do it in a try/finally block, to ensure
        //            // that it is released.
        //            IntPtr buffer = IntPtr.Zero;
        //            // Try/finally.
        //            try
        //            {
        //                // Allocate the memory.
        //                buffer = Marshal.AllocCoTaskMem(bytesNeeded);
        //                // Make the call again. If it did not succeed, then
        //                // raise an error.
        //                result = IpHlpApiHelper.GetIpNetTable(buffer, ref bytesNeeded, false);
        //                // If the result is not 0 (no error), then throw an exception.
        //                if (result != 0)
        //                {
        //                    // Throw an exception.
        //                    throw new Win32Exception(result);
        //                }

        //                // Now we have the buffer, we have to marshal it. We can read
        //                // the first 4 bytes to get the length of the buffer.
        //                int entries = Marshal.ReadInt32(buffer);

        //                // Increment the memory pointer by the size of the int.
        //                IntPtr currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(typeof(int)));

        //                // Allocate an array of entries.
        //                MIB_IPNETROW[] table = new MIB_IPNETROW[entries];

        //                // Cycle through the entries.
        //                for (int index = 0; index < entries; index++)
        //                {
        //                    // Call PtrToStructure, getting the structure information.
        //                    table[index] = (MIB_IPNETROW)Marshal.PtrToStructure(new
        //                       IntPtr(currentBuffer.ToInt64() + (index *
        //                       Marshal.SizeOf(typeof(MIB_IPNETROW)))), typeof(MIB_IPNETROW));
        //                }

        //                for (int index = 0; index < entries; index++)
        //                {
        //                    MIB_IPNETROW row = table[index];

        //                    var item = new IpNetToMediaEntry()
        //                    {
        //                        IpNetToMediaIfIndex = row.dwIndex,
        //                        IpNetToMediaPhysAddress = row.mac0.ToString("X2") + '-' +
        //                                                  row.mac1.ToString("X2") + '-' +
        //                                                  row.mac2.ToString("X2") + '-' +
        //                                                  row.mac3.ToString("X2") + '-' +
        //                                                  row.mac4.ToString("X2") + '-' +
        //                                                  row.mac5.ToString("X2") + '-' ,
        //                        IpNetToMediaNetAddress = new IPAddress(BitConverter.GetBytes(row.dwAddr)).ToString(),
        //                        IpNetToMediaType = row.dwType
        //                    };
        //                    //IPAddress ip = new IPAddress(BitConverter.GetBytes(row.dwAddr));
        //                    //Console.Write("IP:" + ip.ToString() + "\t\tMAC:");

        //                    //Console.Write(row.mac0.ToString("X2") + '-');
        //                    //Console.Write(row.mac1.ToString("X2") + '-');
        //                    //Console.Write(row.mac2.ToString("X2") + '-');
        //                    //Console.Write(row.mac3.ToString("X2") + '-');
        //                    //Console.Write(row.mac4.ToString("X2") + '-');
        //                    //Console.WriteLine(row.mac5.ToString("X2"));
        //                    data.Add(item);
        //                }
        //            }
        //            finally
        //            {
        //                // Release the memory.
        //                IpHlpApiHelper.FreeMibTable(buffer);
        //            }

        //            return data;
        //        }
        //    }
        //}

        public class IfData
        {
        }

        public class IpData
        {
            static public IpHlpApiHelper.MIB_IPSTATS Statistics
            {
                get
                {
                    var stats = new IpHlpApiHelper.MIB_IPSTATS();
                    IpHlpApiHelper.GetIpStatistics(ref stats);
                    return stats;
                }
            }
            static public List<IpHlpApiHelper.MIB_IPADDRROW_W2K> AddrTable
            {
                get
                {
                    return IpHlpApiHelper.Table<IpHlpApiHelper.MIB_IPADDRROW_W2K>(IpHlpApiHelper.GetIpAddrTable);
                }
            }
            static public List<IpHlpApiHelper.MIB_IPFORWARDROW> RouteTable
            {
                get
                {
                    return IpHlpApiHelper.Table<IpHlpApiHelper.MIB_IPFORWARDROW>(IpHlpApiHelper.GetIpForwardTable);
                }
            }
            static public List<IpHlpApiHelper.MIB_IPNETROW> NetTable
            {
                get
                {
                    return IpHlpApiHelper.Table<IpHlpApiHelper.MIB_IPNETROW>(IpHlpApiHelper.GetIpNetTable);
                }
            }
        }

        public class TcpData
        {
            static public IpHlpApiHelper.MIB_TCPSTATS Statistics
            {
                get
                {
                    var stats = new IpHlpApiHelper.MIB_TCPSTATS();
                    IpHlpApiHelper.GetTcpStatistics(ref stats);
                    return stats;
                }
            }
            static public List<IpHlpApiHelper.MIB_TCPROW> TableData
            {
                get
                {
                    var data = IpHlpApiHelper.Table<IpHlpApiHelper.MIB_TCPROW>(IpHlpApiHelper.GetTcpTable);
                    return data;
                }
            }
        }
        public class UdpData
        {
            static public IpHlpApiHelper.MIB_UDPSTATS Statistics
            {
                get
                {
                    var stats = new IpHlpApiHelper.MIB_UDPSTATS();
                    IpHlpApiHelper.GetUdpStatistics(ref stats);
                    return stats;
                }
            }
            static public List<IpHlpApiHelper.MIB_UDPROW> TableData
            {
                get
                {
                    var data = IpHlpApiHelper.Table<IpHlpApiHelper.MIB_UDPROW>(IpHlpApiHelper.GetUdpTable);
                    return data;
                }
            }
        }
    }
}
