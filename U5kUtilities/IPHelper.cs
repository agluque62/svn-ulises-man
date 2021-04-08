using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Utilities
{
    public class IPHelper
    {
        public static IPAddress  SafeParse(string ip)
        {
            try
            {
                return IPAddress.Parse(ip);
            }
            catch(Exception)
            {
            }
            return null;
        }
        public static IPEndPoint EndPointSafeParse(string text)
        {
            try
            {
                Uri uri;
                if (Uri.TryCreate(text, UriKind.Absolute, out uri))
                    return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
                if (Uri.TryCreate(String.Concat("tcp://", text), UriKind.Absolute, out uri))
                    return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);
                if (Uri.TryCreate(String.Concat("tcp://", String.Concat("[", text, "]")), UriKind.Absolute, out uri))
                    return new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port < 0 ? 0 : uri.Port);

                throw new FormatException("Failed to parse text to IPEndPoint");
            }
            catch (Exception )
            {
            }
            return null;
        }
        public static bool IsIpv4(string ip)
        {
            const string pattern = "^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            Regex check = new Regex(pattern);
            return check.IsMatch(ip);
        }
        public static bool IsLocalIpV4Address(string host)
        {
            try
            {
                if (!IsIpv4(host)) return false;
                // get host IP addresses
                IPAddress[] hostIPs = Dns.GetHostAddresses(host);
                // get local IP addresses
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                // test if any host IP equals to any local IP or to localhost
                foreach (IPAddress hostIP in hostIPs)
                {
                    // is localhost
                    if (IPAddress.IsLoopback(hostIP)) return true;
                    // is local address
                    foreach (IPAddress localIP in localIPs)
                    {
                        if (hostIP.Equals(localIP)) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool IsUdpPortInUse(int myport)
        {
            bool alreadyinuse = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().Any(p => p.Port == myport);
            return alreadyinuse;
        }
    }
}
