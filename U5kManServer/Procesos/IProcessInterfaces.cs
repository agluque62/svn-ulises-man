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
        IList<Variable> GetData(object from, IList<Variable> variables);
    }
    public class RuntimeSnmpService : IProcessSnmp
    {
        public event EventHandler<TrapBus.TrapEventArgs> TrapReceived;

        public IList<Variable> GetData(object from, IList<Variable> variables)
        {
            if (from is stdPos)
            {
                var pos = from as stdPos;
                return new SnmpClient().Get(VersionCode.V2,
                    new IPEndPoint(IPAddress.Parse(pos.ip), pos.snmpport),
                    new OctetString("public"), variables,
                    pos.SnmpTimeout, pos.SnmpReintentos);
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
        object trapSubscrition = default;
    }

    public interface IProcessPing
    {
        void Ping(string host, bool presente, Action<bool, IPStatus[]> ResultDelivery);
    }
    public class RuntimePingService : IProcessPing
    {
        public void Ping(string host, bool presente, Action<bool, IPStatus[]> ResultDelivery) => U5kGenericos.Ping(host, presente, ResultDelivery);
    }

    public interface IProcessSip : IDisposable
    {
        void Ping(string user, string ip, int port, bool isRadio, Action<bool, string> ResultDelivery);
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
        public void Ping(string user, string ip, int port, bool isRadio, Action<bool, string> ResultDelivery)
        {
            var ua = new SipUA() { user = user, ip = ip, port = port, radio = isRadio };
            var res = sips.SipPing(ua);
            ResultDelivery(res, ua.last_response?.Result);
        }
        private SipSupervisor sips = null;
    }
}
