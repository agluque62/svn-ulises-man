#define _SUBSCRIBE_CFG_
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Timers;

using WebSocket4Net;
using Newtonsoft.Json;

using Utilities;
using System.Threading.Tasks;

namespace U5kManServer
{
    public enum ServiceStatus { Running, Stopped, Disabled }

    public class PabxItfService : NucleoGeneric.NGThread //BaseCode
    {
        IDataService dataService = null;
        IPingService pingService = null;
        IPbxWsService pbxWsService = null;
        ICommFtpService ftpService = null;
        public PabxItfService(IDataService dataService = null, IPingService pingService=null, IPbxWsService pbxWsService=null, ICommFtpService ftpService=null)
        {
            Int32.TryParse(Properties.u5kManServer.Default.PabxWsPort, out int port);
            this.dataService = dataService ?? new RunTimeData();
            this.pingService = pingService ?? new RuntimePingService();
            this.pbxWsService = pbxWsService ?? new RuntimePbxWsService(port, "sa", Properties.u5kManServer.Default.PabxSaPwd);
            this.ftpService = ftpService ?? new RuntimeCommFtpService(
                Properties.u5kManServer.Default.ProxyFtpUser, 
                Properties.u5kManServer.Default.ProxyFtpPwd,
                (int)TimeSpan.FromSeconds(Properties.u5kManServer.Default.ProxyFtpTimeout).TotalMilliseconds);
        }
        public new string Name => "PabxItfService";
        public ServiceStatus Status => _Status;
        public override bool Start()
        {
            try
            {
                LogInfo<PabxItfService>("Iniciando Servicio...", default, default, Name);

                GlobalServices.GetWriteAccess(() =>
                {
                    IsMaster = dataService.IsMaster;
                    dataService.Data.STDG.stdPabx.Estado = std.NoInfo;

                    _Status = ServiceStatus.Running;
                    _WorkingThread.Start();

                    IsTherePbx = dataService.IsTherePbx;
                    PbxIp = dataService.PbxIp;
                    WsStatus = EPabxStatus.epsDesconectado;

                    _TimerPbax = new Timer();
                    _TimerPbax.Interval = 5000;
                    _TimerPbax.AutoReset = false;
                    _TimerPbax.Elapsed += OnTimePabxElapsed;
                    _TimerPbax.Enabled = true;

                    LogInfo<PabxItfService>("Servicio Iniciado.", default, default, Name);
                    base.Start();   // salir = false; // Compatibilidad con IsRunning
                }, false);
                return true;
            }
            catch (Exception ex)
            {
                Stop(TimeSpan.FromSeconds(5));
                LogException<PabxItfService>("", ex, default, default, Name);
            }
            return false;
        }
        public override void Stop(TimeSpan timeout)
        {
            LogInfo<PabxItfService>("Iniciando parada servicio.", default, default, Name);
            try
            {
                Dispose();
                _WorkingThread?.Stop();
                base.Stop(timeout);
            }
            catch (Exception x)
            {
                LogException<PabxItfService>("Stoping Exception", x, default, default, Name);
            }
            _Status = ServiceStatus.Stopped;
            LogInfo<PabxItfService>("Servicio Detenido.", default, default, Name);
        }
        public override void StopAsync(Action<Task> cb)
        {
            Stop(TimeSpan.FromSeconds(5));
        }

        #region Formatos de Tablas..
        public class PabxParamInfo
        {
            // Evento Register
            public string Registered { get; set; }
            public string User { get; set; }
            // Evento Status,
            public long Time { get; set; }
            public string Other_number { get; set; }
            public string Status { get; set; }
            // public string user { get; set; }
        };
        public class PabxEvent
        {
            public string Jsonrpc { get; set; }
            public string Method { get; set; }
            public PabxParamInfo Parametros { get; set; }
            public override string ToString() => JsonConvert.SerializeObject(this);
        }        
        
        #endregion

        #region Private Members
        enum EPabxStatus { epsDesconectado, epsConectando, epsConectado, epsDesconectando };
        private EPabxStatus _pabxStatus = EPabxStatus.epsDesconectado;
        EPabxStatus WsStatus 
        { 
            get => _pabxStatus;
            set
            {
                LogTrace<PabxItfService>($"WsStatus => {value}", default, default, Name);
                _pabxStatus = value;
            }
        }
        private ServiceStatus _Status = ServiceStatus.Stopped;
        private EventQueue _WorkingThread = new EventQueue();
        private Timer _TimerPbax = null;
        private string InfoString  => String.Format("HayPbx={0}, PbxUrl={1}, Estado={2}", IsTherePbx, pbxWsService.Url, WsStatus);
        private bool IsOperative => IsMaster && IsTherePbx;
        private new bool IsMaster { get; set; }
        private bool IsTherePbx { get; set; }
        private string PbxIp { get; set; }
        #endregion

        #region Callbacks
        private void OnWebsocketOpen(object sender, EventArgs e)
        {
            LogInfo<PabxItfService>($"WebSocketOpen Event on status {WsStatus} url {pbxWsService.Url} ", default, default, Name);
            if (IsOperative)
            {
            }
        }
        private void OnWebsocketError(object sender, WsErrorEventArgs e)
        {
            LogWarn<PabxItfService>($"WebSocketError Event on status {WsStatus} url {pbxWsService.Url} Error {e.Exception.Message}", default, default, Name);
            if (IsOperative)
            {
            }
        }
        private void OnWebsocketClosed(object sender, EventArgs e)
        {
            LogInfo<PabxItfService>($"WebSocketClose Event on status {WsStatus} url {pbxWsService.Url} ", default, default, Name);
            _WorkingThread.Enqueue("websocket_Closed", delegate ()
            {
                GlobalServices.GetWriteAccess(() =>
                {
                    U5KStdGeneral stdg = dataService.Data.STDG;
                    try
                    {
                        if (IsOperative)
                        {
                            WsStatus = EPabxStatus.epsDesconectado;

                            if (stdg.stdPabx.Estado != std.NoInfo)
                                RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_ERROR, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV",
                                    Params(idiomas.strings.PBX_Desconectada/*"PBX Desconectada"*/, "", "", ""));

                            stdg.stdPabx.Estado = std.NoInfo;
                            // stdg.stdPabx.name = idiomas.strings.PBX_Desconocida/*"Desconocido"*/;

                            /* Desregistrar. */
                            List<Uv5kManDestinosPabx.DestinoPabx> stdpbxs = dataService.Data.STDPBXS;
                            stdpbxs.ForEach(d => d.Estado = std.NoInfo);
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<PabxItfService>("", x, default, default, Name);
                    }
                });
            });
        }
        private void OnWebsocketMessageReceived(object sender, WsMessageEventArgs @event)
        {
            LogTrace<PabxItfService>($"WebSocketMessageReceived Event on status {WsStatus}, msg {@event.Message}, ", default, default, Name);

            _WorkingThread.Enqueue("websocket_MessageReceived", delegate ()
            {
                // Como este evento puede tocar la tabla de estado, adquiero el acceso.
                GlobalServices.GetWriteAccess(() =>
                {
                    try
                    {
                        if (IsOperative)
                        {
                            string msg = @event.Message.Replace("params", "parametros");

                            if (msg.StartsWith("{"))
                            {
                                PabxEvent _evento = JsonConvert.DeserializeObject<PabxEvent>(msg);
                                ProcessEvent(dataService.Data, _evento);
                            }
                            else if (msg.StartsWith("["))
                            {
                                PabxEvent[] _eventos = JsonConvert.DeserializeObject<PabxEvent[]>(msg);
                                foreach (PabxEvent _evento in _eventos)
                                {
                                    ProcessEvent(dataService.Data, _evento);
                                }
                            }
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<PabxItfService>("", x, default, default, Name);
                    }
                });
            });
        }
        private void OnTimePabxElapsed(object sender, ElapsedEventArgs e)
        {
            _WorkingThread.Enqueue("OnTimePabxElapsed", delegate ()
            {
                ConfigCultureSet();
                try
                {
                    LogTrace<PabxItfService>($"OnTimePabxElapsed entry => {WsStatus}", default, default, Name);

                    var statusOnEntry = WsStatus;
                    // Supervisión de Master...
                    MasterSlaveManager();
                    if (IsMaster)
                    {
                        // Supervision de la configuracion de PBX
                        PbxConfigManager();
                        if (IsOperative)
                        {
                            switch (WsStatus)
                            {
                                case EPabxStatus.epsDesconectado:
                                    var resp = pingService.Ping(PbxIp, WsStatus == EPabxStatus.epsConectado).Result;
                                    if (resp)
                                    {
                                        WsStatus = SafeWsConnect();
                                    }
                                    break;

                                case EPabxStatus.epsConectando:
                                    /** 20181114. Han pasado 5 segundos sin respuesta. Fuerzo otro ping... */
                                    WsStatus = SafeWsDisconnect();
                                    break;

                                case EPabxStatus.epsConectado:
                                    resp = pingService.Ping(PbxIp, WsStatus == EPabxStatus.epsConectado).Result;
                                    if (resp == false)
                                    {
                                        LogWarn<PabxItfService>("Fallo de Ping....Cierro WS", default, default, Name);
                                        WsStatus = SafeWsDisconnect();

                                        GlobalServices.GetWriteAccess(() =>
                                        {
                                            if (dataService.Data.STDG.stdPabx.Estado != std.NoInfo)
                                                RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_ERROR, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV",
                                                    Params(idiomas.strings.PBX_Desconectada/*"PBX Desconectada"*/, "", "", ""));

                                            dataService.Data.STDG.stdPabx.Estado = std.NoInfo;

                                            /* Desregistrar. */
                                            dataService.Data.STDPBXS.ForEach(d => d.Estado = std.NoInfo);
                                        });
                                    }
                                    else
                                    {
                                    }
                                    break;

                                case EPabxStatus.epsDesconectando:
                                    if (statusOnEntry == EPabxStatus.epsDesconectando)
                                    {
                                        /** 20181114. Han pasado 5 segundos si el evento */
                                        WsStatus = EPabxStatus.epsDesconectado;
                                        LogWarn<PabxItfService>($"Timeot on disconnecting...", default, default, Name);
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                        GetProxyDataAndVersions(null);
                    }
                    else
                    {
                        WsStatus = EPabxStatus.epsDesconectado;
                    }

                    LogTrace<PabxItfService>($"OnTimePabxElapsed exit => {WsStatus}", default, default, Name);
                }
                catch (Exception x)
                {
                    LogException<PabxItfService>("", x, default, default, Name);
                }

                _TimerPbax.Enabled = true;
            });

            IamAlive.Tick("PbxItfService-Timer", () =>
            {
                IamAlive.Message(String.Format("PbxItfService-Timer ({0}). Is Alive.", InfoString));
            });
        }
        #endregion

        #region Private Functions.

        void ConnectTheEvents()
        {
            pbxWsService.WsOpen -= OnWebsocketOpen;
            pbxWsService.WsClosed -= OnWebsocketClosed;
            pbxWsService.WsError -= OnWebsocketError;
            pbxWsService.WsMessage -= OnWebsocketMessageReceived;

            pbxWsService.WsOpen += OnWebsocketOpen;
            pbxWsService.WsClosed += OnWebsocketClosed;
            pbxWsService.WsError += OnWebsocketError;
            pbxWsService.WsMessage += OnWebsocketMessageReceived;
        }
        void MasterSlaveManager()
        {
            if (IsMaster != dataService.IsMaster)
            {
                if (dataService.IsMaster)
                {
                    LogInfo<PabxItfService>($"Changed to Master mode", default, default, Name);
                }
                else
                {
                    WsStatus = SafeWsDisconnect();
                    LogInfo<PabxItfService>($"Changed to Slave mode", default, default, Name);
                }
                IsMaster = dataService.IsMaster;
            }
        }
        void PbxConfigManager()
        {
            if (dataService.IsTherePbx != IsTherePbx)
            {
                if (dataService.IsTherePbx)
                {
                    // Ha aparecido la PBX en CFG.
                    LogInfo<PabxItfService>("PBX on System.", default, default, Name);
                }
                else
                {
                    // Ha desaparecido la PBX en CFG
                    WsStatus = SafeWsDisconnect();
                    LogInfo<PabxItfService>("PBX out of System.", default, default, Name);
                }
                IsTherePbx = dataService.IsTherePbx;
            }
            if (dataService.PbxIp != PbxIp)
            {
                LogInfo<PabxItfService>($"PBX IP Change Detected {PbxIp} => {dataService.PbxIp}. Reconnecting...", default, default, Name);
                PbxIp = dataService.PbxIp;
                if (IsOperative)
                {
                    // Ha cambiado la IP y hay que reconectarse...
                    WsStatus = SafeWsDisconnect();
                }
            }
        }
        EPabxStatus SafeWsDisconnect()
        {
            GlobalServices.GetWriteAccess(() => dataService.Data.STDG.stdPabx.Estado = std.NoInfo);
            LogTrace<PabxItfService>("SafeWsDisconnect Entry");
            if (WsStatus != EPabxStatus.epsDesconectado)
            {
                try
                {
                    pbxWsService.Disconnect().Wait(); // El procedimiento general post eventos?
                }
                finally
                {
                }
                return EPabxStatus.epsDesconectando;
            }
            return EPabxStatus.epsDesconectado;
        }
        EPabxStatus SafeWsConnect()
        {
            LogTrace<PabxItfService>("SafeWsConnect Entry");
            ConnectTheEvents();
            return pbxWsService.Connect(PbxIp).Result ? EPabxStatus.epsConectando : EPabxStatus.epsDesconectado;
        }
        private new void Dispose()
        {
            _TimerPbax.Enabled = false;

            if (WsStatus == EPabxStatus.epsConectado)
            {
                WsStatus = SafeWsDisconnect();
            }
            // AGL. Al ser llamada desde 'fuera', no utilizare el control de acceso para evitar Lazos no deseados...
            GlobalServices.GetWriteAccess(() =>
            {
                dataService.Data.STDG.stdPabx.Estado = std.NoInfo;
            }, false);
        }
        private void ProcessEvent(U5kManStdData gdata, PabxEvent _event)
        {
            switch (_event.Method)
            {
                case "notify_serverstatus":
                    ProcessServerStatus(gdata, _event.Parametros);
                    break;
                case "notify_status":
                    ProcessUserStatus(_event.Parametros);
                    break;
                case "notify_registered":
                    ProcessUserRegistered(gdata, _event.Parametros);
                    break;
                default:
                    LogWarn<PabxItfService>("Evento No Procesado: " + _event.Method, default, default, Name);
                    break;
            }
        }
        private void ProcessServerStatus(U5kManStdData gdata, PabxParamInfo info)
        {
            switch (info.Status)
            {
                case "active":
                    WsStatus = EPabxStatus.epsConectado;
                    U5KStdGeneral stdg = gdata.STDG;
                    if (stdg.stdPabx.Estado != std.Ok)
                    {
                        RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_INFO, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV",
                            Params(idiomas.strings.PBX_Conectada/*"PBX Conectada"*/, "", "", ""));
                    }
                    stdg.stdPabx.Estado = std.Ok;
                    stdg.stdPabx.name = String.Format("{0}:{1}", PbxIp, Properties.u5kManServer.Default.PabxWsPort);
                    break;
                default:
                    WsStatus = SafeWsDisconnect();
                    break;
            }
            LogDebug<PabxItfService>("Server Status=>" + info.Status, default, default, Name);
        }
        private void ProcessUserRegistered(U5kManStdData gdata, PabxParamInfo info)
        {
            bool registrado = info.Registered == "true";
            List<Uv5kManDestinosPabx.DestinoPabx> pbxdes = gdata.STDPBXS;
            pbxdes.Where(d => d.Id == info.User).ToList().ForEach(d =>
            {
                std EstadoNotificado = registrado ? std.Ok : std.NoInfo;
                if (d.Estado != EstadoNotificado)
                {
                    d.Estado = EstadoNotificado;
                    // Generar Historico de Conexion / Desconexion...
                    RecordEvent<PabxItfService>(DateTime.Now, registrado ? U5kBaseDatos.eIncidencias.IPBX_SUBSC_ACTIVE :
                        U5kBaseDatos.eIncidencias.IPBX_SUBSC_INACTIVE, U5kBaseDatos.eTiposInci.TEH_EXTERNO_TELEFONIA, info.User, Params());
                }
            });
            LogTrace<PabxItfService>(String.Format("Procesado Registro Usuario {0}, {1}", Name, info.User, info.Registered), default, default, Name);
        }
        private void ProcessUserStatus(PabxParamInfo info)
        {
            LogTrace<PabxItfService>(String.Format("Procesado Estado Usuario {1}, Estado: {2}", Name, info.User, info.Status), default, default, Name);
        }
        private void ChangeConfigSpv(Action processChange)
        {
            if (IsTherePbx != dataService.IsTherePbx || PbxIp != dataService.PbxIp)
            {
                IsTherePbx = dataService.IsTherePbx;
                PbxIp = dataService.PbxIp;
                processChange();
            }
        }
        private void GetProxyDataAndVersions(Action notify)
        {
            var elapsed = DateTime.Now - LastTestProxyData;
            if (elapsed > TestProxyDataInterval)
            {
                LogTrace<PabxItfService>($"GetProxyDataAndVersions entry...", default, default, Name);
                var FileName = "SipProxyPBXVersions.json";
                var RemotePath = "/home/user";
                var ftpLocalServer = $"ftp://{Properties.u5kManServer.Default.ProxyLocalAdd}";

                var resping = pingService.Ping(Properties.u5kManServer.Default.ProxyLocalAdd, true).Result;
                if (resping)
                {
                    var resftp = ftpService.Download(ftpLocalServer, $"{RemotePath}/{FileName}", FileName).Result;
                    LogDebug<PabxItfService>($"Getting Local PBXVersion file on {ftpLocalServer} Result: {resftp.Success}, Error: {resftp.Result?.Message}", default, default, Name);
                }
                else
                {
                    LogTrace<PabxItfService>($"GetProxyDataAndVersions LocalHost {Properties.u5kManServer.Default.ProxyLocalAdd} No Presente.", default, default, Name);
                }
                if (IsOperative)
                {
                    var resp = pingService.Ping(PbxIp, true).Result;
                    if (resp)
                    {
                        var ftpActiveServer = $"ftp://{PbxIp}";
                        var resftp = ftpService.Download(ftpActiveServer, $"{RemotePath}/{FileName}").Result;
                        var error = resftp.Success ? "" : resftp.Result;
                        LogDebug<PabxItfService>($"Getting Active PBXVersion file on {ftpActiveServer} Result: {resftp.Success}, Error: {error}", default, default, Name);
                        if (resftp.Success)
                        {
                            var jdata = JsonHelper.SafeJObjectParse(resftp.Result);
                            NodeId = jdata != null ? jdata["local_node"]?.ToString() : "Error";
                            NodeStatus = jdata != null ? jdata["node_status"]?.ToString() : "Error";
                        }
                    }
                    else
                    {
                        LogTrace<PabxItfService>($"GetProxyDataAndVersions Configured PBX {PbxIp} No Presente.", default, default, Name);
                    }
                }

                LastTestProxyData = DateTime.Now;
                LogTrace<PabxItfService>($"GetProxyDataAndVersions exit...", default, default, Name);
            }
        }
        private TimeSpan TestProxyDataInterval = TimeSpan.FromSeconds(30);
        private DateTime LastTestProxyData = DateTime.MinValue;
        private string NodeId { get; set; }
        private string NodeStatus { get; set; }
        #endregion

    }

}
