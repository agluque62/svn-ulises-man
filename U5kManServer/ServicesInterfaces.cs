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
using WebSocket4Net;

using U5kBaseDatos;
using NucleoGeneric;
using Utilities;
using NAudio.Gui;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace U5kManServer
{
    public class QueryServiceResult<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public QueryServiceResult(bool success = default, T result = default)
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
        bool IsTherePbx { get; }
        string PbxIp { get; }
    }
    public class RunTimeData : IDataService
    {
        public U5kManStdData Data => U5kManService.GlobalData;
        public bool IsMaster => U5kManService._Master;
        public bool IsTherePbx => U5kManService.PbxEndpoint != null;
        public string PbxIp => U5kManService.PbxEndpoint == null ? "none" : U5kManService.PbxEndpoint.Address.ToString();

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

    public interface ICommHttpService : IDisposable
    {
        Task<QueryServiceResult<string>> Get(string url, TimeSpan timeout);
    }
    public class RuntimeHttpService : ICommHttpService
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

    public class WsErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }
    public class WsMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
    public interface IPbxWsService : IDisposable
    {
        event EventHandler<EventArgs> WsOpen;
        event EventHandler<EventArgs> WsClosed;
        event EventHandler<WsErrorEventArgs> WsError;
        event EventHandler<WsMessageEventArgs> WsMessage;
        Task<bool> Connect(string ip);
        Task<bool> Disconnect();
        string Url { get; set; }
    }
    public class RuntimePbxWsService : IPbxWsService
    {
        public event EventHandler<EventArgs> WsOpen = null;
        public event EventHandler<EventArgs> WsClosed = null;
        public event EventHandler<WsErrorEventArgs> WsError;
        public event EventHandler<WsMessageEventArgs> WsMessage;

        public string Url { get; set; } = null;
        public RuntimePbxWsService(int port, string  username, string password)
        {
            this.port = port;
            this.username = username;
            this.password = password;
        }
        public void Dispose()
        {
            ws.Opened += null;
            ws.Closed += null;
            ws.Error += null;
            ws.MessageReceived += null;
            ws?.Dispose();
            ws = null;
        }
        public Task<bool> Connect(string ip)
        {
            return Task.Run(() =>
            {
                if (!isCreated)
                {
                    try
                    {
                        Url = $"ws://{ip}:{port}/pbx/ws?login_user={username}&login_password={password}&user=*&registered=True&status=True&line=*";
                        ws = new WebSocket(Url);
                        ws.Opened += (from, data) => WsOpen?.Invoke(from, data);
                        ws.Closed += (from, data) => WsClosed?.Invoke(from, data);
                        ws.Error += (from, data) => WsError?.Invoke(from, new WsErrorEventArgs() { Exception = data.Exception });
                        ws.MessageReceived += (from, data) => WsMessage(from, new WsMessageEventArgs() { Message = data.Message });
                        ws.Open();
                        isCreated = true;
                        return true;
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                }
                return false;
            });
        }
        public Task<bool> Disconnect()
        {
            return Task.Run(() =>
            {
                if (isCreated)
                {
                    try
                    {
                        ws.Close();
                        Task.Delay(100).Wait(); // Para que entre el evento close.
                        Dispose();
                        isCreated = false;
                        return true;
                    }
                    catch (Exception)
                    {

                        throw;
                    }

                }
                return false;
            });
        }

        //Task<bool> Open()
        //{
        //    return Task.Run(() =>
        //    {
        //        if (ws?.State == WebSocketState.Closed || ws.State == WebSocketState.None)
        //        {
        //            ws?.Open();
        //            return true;
        //        }
        //        return false;
        //    });
        //}
        //Task Close()
        //{
        //    return Task.Run(() => ws?.Close());
        //}
        //Task Connect1(string ip)
        //{
        //    return Task.Run(() =>
        //    {
        //        Dispose();
        //        Setup(ip);
        //    });
        //}
        //void Setup(string ip)
        //{
        //    Url = $"ws://{ip}:{port}/pbx/ws?login_user={username}&login_password={password}&user=*&registered=True&status=True&line=*";
        //    ws = new WebSocket(Url);
        //    ws.Opened += (from, data) => WsOpen?.Invoke(from, data);
        //    ws.Closed += (from, data) => WsClosed?.Invoke(from, data);
        //    ws.Error += (from, data) => WsError?.Invoke(from, new WsErrorEventArgs() { Exception = data.Exception });
        //    ws.MessageReceived += (from, data) => WsMessage(from, new WsMessageEventArgs() { Message = data.Message });
        //}


        WebSocket ws = null;
        int port = 0;
        string username = null;
        string password = null;
        bool isCreated = false;
    }

    public interface ICommFtpService : IDisposable
    {
        Task<QueryServiceResult<Exception>> Download(string host, string remotePath, string localPath);
        Task<QueryServiceResult<string>> Download(string host, string remotePath);
    }
    public class RuntimeCommFtpService : ICommFtpService
    {
        public void Dispose()
        {
        }
        public Task<QueryServiceResult<Exception>> Download(string host, string remotePath, string localPath)
        {
            return Task.Run(() =>
            {
                QueryServiceResult<Exception> result = new QueryServiceResult<Exception>();
                using (var ftp = new FtpClient(host, user, password, timeout))
                {
                    ftp.Download(remotePath, localPath, (res, ex) =>
                    {
                        result.Success = res;
                        result.Result = ex;
                    });
                }
                return result;
            });
        }

        public Task<QueryServiceResult<string>> Download(string host, string remotePath)
        {
            return Task.Run(() =>
            {
                QueryServiceResult<string> result = new QueryServiceResult<string>();
                using (var ftp = new FtpClient(host, user, password, timeout))
                {
                    ftp.Download(remotePath, (res, data, ex) =>
                    {
                        result.Success = res;
                        result.Result = res ? data : ex.Message;
                    });
                }
                return result;
            });
        }

        public RuntimeCommFtpService(string user, string password, int timeout)
        {
            this.user = user;
            this.password = password;
            this.timeout = timeout;
        }
        string user = null;
        string password = null;
        int timeout = 0;
    }
}
