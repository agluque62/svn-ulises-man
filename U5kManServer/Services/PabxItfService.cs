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
using NucleoGeneric;
using System.Threading.Tasks;

namespace U5kManServer
{
    public enum ServiceStatus { Running, Stopped, Disabled }

    public class PabxItfService : NucleoGeneric.NGThread //BaseCode
    {
        IDataService dataService = null;
        IPingService pingService = null;
        IPbxWsService pbxWsService = null;
        public PabxItfService(IDataService dataService = null, IPingService pingService=null, IPbxWsService pbxWsService=null)
        {
            Int32.TryParse(Properties.u5kManServer.Default.PabxWsPort, out int port);
            this.dataService = dataService ?? new RunTimeData();
            this.pingService = pingService ?? new RuntimePingService();
            this.pbxWsService = pbxWsService ?? new RuntimePbxWsService(port, "sa", Properties.u5kManServer.Default.PabxSaPwd);
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
                    dataService.Data.STDG.stdPabx.Estado = std.NoInfo;

                    _Status = ServiceStatus.Running;
                    _WorkingThread.Start();

                    pbxWsService.Connect(dataService.PbxIp);
                    ConnectTheEvents();
                    pbxWsService.Open();
                    _pabxStatus = EPabxStatus.epsConectando;

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
            Action stopbase = () => { base.Stop(timeout); };
            List<Tuple<bool, Action>> ConditionalStopActions = new List<Tuple<bool, Action>>()
            {
                new Tuple<bool, Action>(_Status == ServiceStatus.Running, Dispose),
                new Tuple<bool, Action>(true, _WorkingThread.Stop),
                new Tuple<bool, Action>(true, stopbase),
            };

            LogInfo<PabxItfService>("Iniciando parada servicio.", default, default, Name);

            ConditionalStopActions.ForEach(action =>
            {
                try
                {
                    if (action.Item1)
                        action.Item2();
                    LogInfo<PabxItfService>($"{action.Item2}. Detenido.", default, default, Name);
                }
                catch (Exception x)
                {
                    LogException<PabxItfService>("Stoping Exception", x, default, default, Name);
                }
            });
            _Status = ServiceStatus.Stopped;
            LogInfo<PabxItfService>("Servicio Detenido.", default, default, Name);
        }
        public override void StopAsync(Action<Task> cb)
        {
            Stop(TimeSpan.FromSeconds(5));
        }

        #region Formatos de Tablas..
        class PabxParamInfo
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
        class PabxEvent
        {
            public string Jsonrpc { get; set; }
            public string Method { get; set; }
            public PabxParamInfo Parametros { get; set; }
        };
        #endregion

        #region Private Members
        enum EPabxStatus { epsDesconectado, epsConectando, epsConectado };
        private EPabxStatus _pabxStatus = EPabxStatus.epsDesconectado;
        private ServiceStatus _Status = ServiceStatus.Stopped;
        private EventQueue _WorkingThread = new EventQueue();
        private Timer _TimerPbax = null;
        private string InfoString { get => String.Format("HayPbx={0}, PbxUrl={1}, Estado={2}", dataService.IsTherePbx, pbxWsService.Url, _pabxStatus); }
        private bool IsOperative => dataService.IsMaster && dataService.IsTherePbx;
        #endregion

        #region Callbacks
        private void OnWebsocketOpen(object sender, EventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_opened");
            if (IsOperative)
            {
            }
            LogTrace<PabxItfService>("WebSocket Abierto en " + pbxWsService.Url, default, default, Name);
        }
        private void OnWebsocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_error");
            if (IsOperative)
            {
            }
            LogWarn<PabxItfService>("WebSocketError en " + pbxWsService.Url + ": " + e.Exception.Message, default, default, Name);
        }
        private void OnWebsocketClosed(object sender, EventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_closed");

            _WorkingThread.Enqueue("websocket_Closed", delegate ()
            {
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_closed enqueue");

                GlobalServices.GetWriteAccess(() =>
                {
                    U5KStdGeneral stdg = dataService.Data.STDG;
                    try
                    {
                        if (IsOperative)
                        {
                            _pabxStatus = EPabxStatus.epsDesconectado;

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
            LogInfo<PabxItfService>("WebSocket Cerrado en " + pbxWsService.Url, default, default, Name);
        }
        private void OnWebsocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_msg");

            _WorkingThread.Enqueue("websocket_MessageReceived", delegate ()
            {
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_msg enqueue");
                // Como este evento puede tocar la tabla de estado, adquiero el acceso.
                GlobalServices.GetWriteAccess(() =>
                {
                    try
                    {
                        if (IsOperative)
                        {
                            string msg = e.Message.Replace("params", "parametros");

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

            LogTrace<PabxItfService>(String.Format("WebSocket en {0}: Mensaje Recibido: {1}", pbxWsService.Url, e.Message), default, default, Name);
        }
        private void OnTimePabxElapsed(object sender, ElapsedEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " TimePbxElapsed");

            _WorkingThread.Enqueue("OnTimePabxElapsed", delegate ()
            {
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " TimePbxElapsed enqueue");
                ConfigCultureSet();
                try
                {
                    if (IsOperative)
                    {
                        if (Properties.u5kManServer.Default.PabxSimulada == false)
                        {
                            /** 20181114. Supervisa los cambios de configuracion */
                            ChangeConfigSpv(() =>
                            {
                                GlobalServices.GetWriteAccess(() =>
                                {
                                    dataService.Data.STDG.stdPabx.Estado = std.NoInfo;
                                });

                                if (_pabxStatus != EPabxStatus.epsDesconectado)
                                {
                                    try
                                    {
                                       pbxWsService.Close();
                                    }
                                    finally
                                    {
                                    }
                                }

                                pbxWsService.Connect(dataService.PbxIp);
                                ConnectTheEvents();
                                _pabxStatus = EPabxStatus.epsDesconectado;
                                LogInfo<PabxItfService>("Servicio Reinicializado por cambio de configuracion.", default, default, Name);
                            });

                            switch (_pabxStatus)
                            {
                                case EPabxStatus.epsDesconectado:
                                    var resp = pingService.Ping(dataService.PbxIp, _pabxStatus == EPabxStatus.epsConectado).Result;
                                    if (resp)
                                    {
                                        pbxWsService.Open();
                                        _pabxStatus = EPabxStatus.epsConectando;
                                    }
                                    break;

                                case EPabxStatus.epsConectando:
                                    /** 20181114. Han pasado 5 segundos sin respuesta. Fuerzo otro ping....*/
                                    _pabxStatus = EPabxStatus.epsDesconectado;
                                    pbxWsService.Close();
                                    break;

                                case EPabxStatus.epsConectado:
                                    resp = pingService.Ping(dataService.PbxIp, _pabxStatus == EPabxStatus.epsConectado).Result;
                                    if (resp)
                                    {
                                        LogWarn<PabxItfService>("Fallo de Ping....Cierro WS", default, default, Name);
                                        _pabxStatus = EPabxStatus.epsDesconectado;
                                        pbxWsService.Close();

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
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    GetProxyDataAndVersions(null);
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
            pbxWsService.WsOpen += OnWebsocketOpen;
            pbxWsService.WsClosed += OnWebsocketClosed;
            pbxWsService.WsError += OnWebsocketError;
            pbxWsService.WsMessage += OnWebsocketMessageReceived;
        }
        private new void Dispose()
        {
            _TimerPbax.Enabled = false;

            if (_pabxStatus == EPabxStatus.epsConectado)
            {
                pbxWsService.Close();
                _pabxStatus = EPabxStatus.epsDesconectado;
            }
            // AGL. Al ser llamada desde 'fuera', no utilizare el control de acceso para evitar Lazos no deseados...
            GlobalServices.GetWriteAccess((data) =>
            {
                data.STDG.stdPabx.Estado = std.NoInfo;
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
                    _pabxStatus = EPabxStatus.epsConectado;
                    U5KStdGeneral stdg = gdata.STDG;
                    if (stdg.stdPabx.Estado != std.Ok)
                    {
                        RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_INFO, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV",
                            Params(idiomas.strings.PBX_Conectada/*"PBX Conectada"*/, "", "", ""));
                    }
                    stdg.stdPabx.Estado = std.Ok;
                    stdg.stdPabx.name = String.Format("{0}:{1}", dataService.PbxIp, Properties.u5kManServer.Default.PabxWsPort);
                    break;
                default:
                    pbxWsService.Close();
                    _pabxStatus = EPabxStatus.epsDesconectado;
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
            string actualPbxIp = U5kManService.PbxEndpoint == null ? "none" : U5kManService.PbxEndpoint.Address.ToString();
            bool actualHayPbx = U5kManService.PbxEndpoint != null;

            if (actualHayPbx != dataService.IsTherePbx || actualPbxIp != dataService.PbxIp)
            {
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
                var ftpUser = Properties.u5kManServer.Default.ProxyFtpUser;
                var ftpPassword = Properties.u5kManServer.Default.ProxyFtpPwd;
                var ftpTimeout = (int)TimeSpan.FromSeconds(Properties.u5kManServer.Default.ProxyFtpTimeout).TotalMilliseconds;
                var ftpLocalServer = $"ftp://{Properties.u5kManServer.Default.ProxyLocalAdd}";
                // Leer el Fichero Local para las versiones.
                U5kGenericos.Ping(Properties.u5kManServer.Default.ProxyLocalAdd, true, (presente, status) =>
                {
                    if (presente)
                    {
                        using (var ftp = new FtpClient(ftpLocalServer, ftpUser, ftpPassword, ftpTimeout))
                        {
                            ftp.Download($"{RemotePath}/{FileName}", FileName, (res, ex) =>
                            {
                                LogDebug<PabxItfService>($"Getting Local PBXVersion file on {ftpLocalServer} Result: {res}, Error: {ex?.Message}", default, default, Name);
                               //if (!res && File.Exists(FileName)) File.Delete(FileName);
                           });
                        }
                    }
                    else
                    {
                        LogTrace<PabxItfService>($"GetProxyDataAndVersions {Properties.u5kManServer.Default.ProxyLocalAdd} No Presente.", default, default, Name);
                    }
                });
                if (IsOperative)
                {
                    var resp = pingService.Ping(dataService.PbxIp, true).Result;
                    if (resp)
                    {
                        var ftpActiveServer = $"ftp://{dataService.PbxIp}";
                        using (var ftp = new FtpClient(ftpActiveServer, ftpUser, ftpPassword, ftpTimeout))
                        {
                            ftp.Download($"{RemotePath}/{FileName}", (res, data, ex) =>
                            {
                                LogDebug<PabxItfService>($"Getting Active PBXVersion file on {ftpActiveServer} Result: {res}, Error: {ex?.Message}", default, default, Name);
                                if (res)
                                {
                                    var jdata = JsonHelper.SafeJObjectParse(data);
                                    NodeId = jdata != null ? jdata["local_node"]?.ToString() : "Error";
                                    NodeStatus = jdata != null ? jdata["node_status"]?.ToString() : "Error";
                                }
                            });
                        }
                    }
                    else
                    {
                        LogTrace<PabxItfService>($"GetProxyDataAndVersions {dataService.PbxIp} No Presente.", default, default, Name);
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
