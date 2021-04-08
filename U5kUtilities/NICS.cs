using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.NetworkInformation;
using System.Management;

namespace Utilities
{
    public class NICS
    {
        static public NetworkInterface[] Interfaces
        {
            get
            {
                return NetworkInterface.GetAllNetworkInterfaces();
            }
        }
        public static void ShowInterfaceSummary()
        {            
            foreach (NetworkInterface adapter in Interfaces)
            {
                Console.WriteLine("Name: {0}", adapter.Name);

                Console.WriteLine(adapter.Description);

                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));

                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);

                Console.WriteLine("  Operational status ...................... : {0}", adapter.OperationalStatus);

                string versions = "";
                // Create a display string for the supported IP versions.
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    versions = "IPv4";
                }
                if (adapter.Supports(NetworkInterfaceComponent.IPv6))
                {
                    if (versions.Length > 0)
                    {
                        versions += " ";
                    }
                    versions += "IPv6";
                }
                Console.WriteLine("  IP version .............................. : {0}", versions);

                Console.WriteLine("  IPV4 Address..............................");

                foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        Console.WriteLine("      {0}", ip.Address.ToString());
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        static public bool GetEthInterface(string ipin, ref string name, ref bool up)
        {
            foreach (NetworkInterface adapter in Interfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (ipin == ip.Address.ToString())
                            {
                                name = adapter.Name;
                                up = adapter.OperationalStatus == OperationalStatus.Up;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static IEnumerable<ManagementObject> GetAllAdapters()
        {
            var tempList = new List<ManagementObject>();

            ManagementObjectCollection moc =
                new ManagementClass("Win32_NetworkAdapterConfiguration").GetInstances();

            foreach (ManagementObject obj in moc)
            {
                tempList.Add(obj);
            }

            return tempList;
        }

        public static IEnumerable<IPAddress> GetAllIps()
        {
            var address = new List<IPAddress>();
            var adapters = GetAllAdapters();
            
            foreach (ManagementObject obj in adapters)
            {
                var add = (string[])obj["IPAddress"];
                if (add != null)
                {
                    foreach(string ips in add)
                    {
                        if (IPHelper.IsIpv4(ips) == true)
                        {
                            address.Add(IPHelper.SafeParse(ips));
                        }
                    }
                }
            }

            return address;
        }
    }
}
