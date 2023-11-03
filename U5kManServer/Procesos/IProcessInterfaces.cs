using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

using Lextm.SharpSnmpLib;
using U5kBaseDatos;
using NucleoGeneric;
using Utilities;
using NAudio.Gui;

namespace U5kManServer.Procesos
{
    public interface IProcessData
    {
        U5kManStdData Data { get; }
        bool IsMaster { get; }
        std StatusChangeManage(std antiguo, std nuevo, int scv, eIncidencias inci, eTiposInci thw, string idhw, params object[] parametros);
    }
    public class RunTimeData : IProcessData
    {
        public U5kManStdData Data => U5kManService.GlobalData;

        public bool IsMaster => U5kManService._Master;

        public std StatusChangeManage(std antiguo, std nuevo, int scv, eIncidencias inci, eTiposInci thw, string idhw, params object[] parametros)
        {
            if (antiguo != nuevo)
            {
                if (HistThread.hproc != null)
                {
                    BaseCode.RecordEvent<MainThread>(DateTime.Now, inci, thw, idhw, parametros);
                }
            }
            return nuevo;
        }
    }

    public interface IProcessSnmp : IDisposable
    {
        event EventHandler<TrapBus.TrapEventArgs> TrapReceived;
        Task<IList<Variable>> GetData(object from, IList<Variable> variables);
        int ToInt(ISnmpData data);
        string ToStr(ISnmpData data);
    }
    public class RuntimeSnmpService : IProcessSnmp
    {
        internal class Data4Client
        {
            public string Ip { get; }
            public int Port { get; }
            public int Timeout { get; }
            public int Retries { get; }
            public Data4Client(object from)
            {
                if (from is stdPos)
                {
                    var pos = from as stdPos;
                    Ip = pos.ip;
                    Port = pos.snmpport;
                    Timeout = pos.SnmpTimeout;
                    Retries = pos.SnmpReintentos;
                }
                else if (from is stdPhGw)
                {
                    var cgw = from as stdPhGw;
                    Ip = cgw.ip;
                    Port = cgw.snmpport;
                    Timeout = cgw.SnmpTimeout;
                    Retries += cgw.SnmpReintentos;
                }
                else
                {
                    Ip = null;
                }
            }
        }
        public event EventHandler<TrapBus.TrapEventArgs> TrapReceived;
        public Task< IList<Variable>> GetData(object from, IList<Variable> variables)
        {
            var data4Poll = new Data4Client(from);
            if (data4Poll.Ip != null)
            {
                return Task.Run(() => 
                    new SnmpClient()
                    .Get(VersionCode.V2,
                        new IPEndPoint(IPAddress.Parse(data4Poll.Ip), data4Poll.Port),
                        new OctetString("public"), variables,
                        data4Poll.Timeout, data4Poll.Retries));
            }
            throw new NotImplementedException();
        }

        public RuntimeSnmpService() 
        {
            trapSubscrition = TrapBus.Subscribe((trap) => SafeRaiseEvent(trap));
        }
        void SafeRaiseEvent(TrapBus.TrapEventArgs e)
        {
            if (TrapReceived != null)
                TrapReceived(this, e);
        }
        public void Dispose()
        {
            TrapBus.Unsubscribe(trapSubscrition);
            TrapReceived = null;
        }

        public int ToInt(ISnmpData data) => data is Integer32 ? (data as Integer32).ToInt32() : -1;
        public string ToStr(ISnmpData data) => data.ToString();

        object trapSubscrition = default;
    }

    public interface IProcessPing
    {
        Task<bool> Ping(string host, bool presente);
    }
    public class RuntimePingService : IProcessPing
    {
        public Task<bool> Ping(string host, bool presente) => Task.Run(() => U5kGenericos.Ping(host, presente));
    }

    public interface IProcessSip : IDisposable
    {
        Task<Tuple<bool,string>> Ping(string user, string ip, int port, bool isRadio);
    }
    public class RuntimeSipService : IProcessSip
    {
        public RuntimeSipService() 
        {
            var local_ua = new SipUA() { user = "MTTO", ip = Properties.u5kManServer.Default.MiDireccionIP, port = 7060 };
            sips = new SipSupervisor(local_ua, Properties.u5kManServer.Default.SipOptionsTimeout);
            sips.NotifyException += (ua, x) =>
            {
                BaseCode.LogException<ExtEquSpv>("SipSupervisor" + ua.uri, x);
            };
        }
        public void Dispose()
        {
            sips.Dispose();
        }
        public Task<Tuple<bool, string>> Ping(string user, string ip, int port, bool isRadio)
        {
            return Task.Run(() =>
            {
                var ua = new SipUA() { user = user, ip = ip, port = port, radio = isRadio };
                var res = sips.SipPing(ua);
                return new Tuple<bool,string>(res, ua.last_response?.Result);
            });
        }
        private SipSupervisor sips = null;
    }

    public interface IProcessHttp : IDisposable
    {

    }
    public class RuntimeHttpService : IProcessHttp
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
