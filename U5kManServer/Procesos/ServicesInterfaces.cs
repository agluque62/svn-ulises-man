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
    public class QueryServiceResult<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public QueryServiceResult(bool success, T result)
        {
            Success = success;
            Result = result;
        }
    }
    public interface IDataService
    {
        U5kManStdData Data { get; }
        bool IsMaster { get; }
        std StatusChangeManage(std antiguo, std nuevo, int scv, eIncidencias inci, eTiposInci thw, string idhw, params object[] parametros);
    }
    public class RunTimeData : IDataService
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

    public interface ICommSnmpService : IDisposable
    {
        event EventHandler<TrapBus.TrapEventArgs> TrapReceived;
        Task<QueryServiceResult<IList<Variable>>> GetData(object from, IList<Variable> variables);
        int ToInt(ISnmpData data);
        string ToStr(ISnmpData data);
    }
    public class RuntimeSnmpService : ICommSnmpService
    {
        internal class Data4Client
        {
            public string Ip { get; }
            public int Port { get; }
            public int Timeout { get; }
            public int Retries { get; }
            public string Id { get; }
            public Data4Client(object from)
            {
                if (from is stdPos)
                {
                    var pos = from as stdPos;
                    Id = pos.name;
                    Ip = pos.ip;
                    Port = pos.snmpport;
                    Timeout = pos.SnmpTimeout;
                    Retries = pos.SnmpReintentos;
                }
                else if (from is stdPhGw)
                {
                    var cgw = from as stdPhGw;
                    Id = cgw.name;
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
        public Task<QueryServiceResult<IList<Variable>>> GetData(object from, IList<Variable> variables)
        {
            var data4Poll = new Data4Client(from);
            if (data4Poll.Ip != null)
            {
                return Task.Run(() =>
                {
                    try
                    {
                        var snmpRes = new SnmpClient().Get(
                            VersionCode.V2,
                            new IPEndPoint(IPAddress.Parse(data4Poll.Ip), data4Poll.Port),
                            new OctetString("public"), variables,
                            data4Poll.Timeout, data4Poll.Retries);
                        return new QueryServiceResult<IList<Variable>>(true, snmpRes);
                    }
                    catch (Exception x)
                    {
                        BaseCode.LogException<RuntimeSnmpService>("SnmpException", x, default, default, data4Poll.Id);
                        return new QueryServiceResult<IList<Variable>>(false, null);
                    }
                });
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

    public interface IPingService
    {
        Task<bool> Ping(string host, bool presente);
    }
    public class RuntimePingService : IPingService
    {
        public Task<bool> Ping(string host, bool presente) => Task.Run(() => U5kGenericos.Ping(host, presente));
    }

    public interface ICommSipService : IDisposable
    {
        Task<QueryServiceResult<string>> Ping(string user, string ip, int port, bool isRadio);
    }
    public class RuntimeSipService : ICommSipService
    {
        public RuntimeSipService() 
        {
            var local_ua = new SipUA() { user = "MTTO", ip = Properties.u5kManServer.Default.MiDireccionIP, port = 7060 };
            sips = new SipSupervisor(local_ua, Properties.u5kManServer.Default.SipOptionsTimeout);
            sips.NotifyException += (ua, x) =>
            {
                BaseCode.LogException<RuntimeSipService>("SipSupervisor", x, default, default, ua.uri);
            };
        }
        public void Dispose()
        {
            sips.Dispose();
        }
        public Task<QueryServiceResult<string>> Ping(string user, string ip, int port, bool isRadio)
        {
            return Task.Run(() =>
            {
                try
                {
                    var ua = new SipUA() { user = user, ip = ip, port = port, radio = isRadio };
                    var res = sips.SipPing(ua);
                    return new QueryServiceResult<string>(res, ua.last_response?.Result);
                }
                catch (Exception x)
                {
                    BaseCode.LogException<RuntimeSipService>("", x, default, default, user);
                    return new QueryServiceResult<string>(false, x.Message);
                }
            });
        }
        private SipSupervisor sips = null;
    }

    public interface IProcessHttp : IDisposable
    {
        Task<QueryServiceResult<string>> Get(string url, TimeSpan timeout);
    }
    public class RuntimeHttpService : IProcessHttp
    {
        public void Dispose()
        {
        }
        public Task<QueryServiceResult<string>> Get(string url, TimeSpan timeout)
        {
            return Task.Run( async () =>
            {
                try
                {
                    var res = await HttpHelper.GetAsync(url, timeout);
                    return new QueryServiceResult<string>(true, res);
                }
                catch (Exception x)
                {
                    BaseCode.LogException<RuntimeSnmpService>("Http exception", x, default, default, url);
                    return new QueryServiceResult<string>(false, x.Message);
                }
            });
        }
    }
}
