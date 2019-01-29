using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Management;
using System.Dynamic;

using U5kManMibRevC;

namespace UnitTesting
{
    [TestClass]
    public class NetworkInformationTests
    {
        [TestMethod]
        public void GettingTcpInfo()
        {
            var tcpStats = MibIITableHelpers.TcpData.Statistics;
            var tcpTable = MibIITableHelpers.TcpData.TableData;
        }

        [TestMethod]
        public void GettingUdpInfo()
        {
            var udpStats = MibIITableHelpers.UdpData.Statistics;
            var udpTable = MibIITableHelpers.UdpData.TableData;
        }

        [TestMethod]
        public void GettingIpInfo()
        {
            var ipStats = MibIITableHelpers.IpData.Statistics;
            var ipAddTb = MibIITableHelpers.IpData.AddrTable;
            var ipRouTb = MibIITableHelpers.IpData.RouteTable;
            var ipNetTb = MibIITableHelpers.IpData.NetTable;
        }

        [TestMethod]
        public void InitialitingMib()
        {
            Ed137RevCMib _mib = new Ed137RevCMib(
                new Func<object>(() => { return SnmpStatatics; }),
                new AgentDataGet(
                    (toma) =>
                    {
                        toma(ServiceData);
                    }));
        }       

        object SnmpStatatics
        {
            get
            {
                return new 
                {
                    SnmpInPkts = 0,
                    SnmpOutPkts = 0,
                    SnmpInBadVersions = 0,
                    SnmpInBadCommunityNames = 0,
                    SnmpInBadCommunityUses = 0,
                    SnmpInASNParseErrs = 0,
                    SnmpInTooBigs = 0,
                    SnmpInNoSuchNames = 0,
                    SnmpInBadValues = 0,
                    SnmpInReadOnlys = 0,
                    SnmpInGenErrs = 0,
                    SnmpInTotalReqVars = 0,
                    SnmpInTotalSetVars = 0,
                    SnmpInGetRequest = 0,
                    SnmpInGetNexts = 0,
                    SnmpInSetRequest = 0,
                    SnmpInGetResponses = 0,
                    SnmpInTraps = 0,
                    SnmpOutTooBigs = 0,
                    SnmpOutNoSuchNames = 0,
                    SnmpOutBadValues = 0,
                    SnmpOutGenErrs = 0,
                    SnmpOutGetRequests = 0,
                    SnmpOutGetNexts = 0,
                    SnmpOutSetRequests = 0,
                    SnmpOutGetResponses = 0,
                    SnmpOutTraps = 0,
                    SnmpEnableAuthenTraps = 2
                };
            }
        }

        object ServiceData
        {
            get
            {
                return new
                {
                    STDTOPS = new List<int>() { 0, 1, 2, 3, 4 }
                };
            }
        }

    }

}
