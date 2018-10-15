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

namespace U5kManServer
{
    public enum ServiceStatus { Running, Stopped, Disabled }
    
    /// <summary>
    /// 
    /// </summary>
    public class PabxItfService : NucleoGeneric.NGThread //BaseCode
    {
        public PabxItfService()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool IsOperative
        {
            get
            {
                return (U5kManService._Master == true && HayPbx);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public new string Name
        {
            get { return "PabxItfService"; }
        }
        /// <summary>
        /// 
        /// </summary>
        public ServiceStatus Status
        {
            get { return _Status; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override bool Start()
        {
            try
            {
                LogInfo<PabxItfService>(String.Format("{0}: Iniciando Servicio...", Name));

                PbxIp = U5kManService.PbxEndpoint == null ? "none" : U5kManService.PbxEndpoint.Address.ToString();
                HayPbx = U5kManService.PbxEndpoint != null;
                _Status = ServiceStatus.Running;
                _WorkingThread.Start();

                _TimerPbax = new Timer();
                _TimerPabxSimulada = new Timer(5000);
                
                // Inicializar WebSocket. 
                if (Properties.u5kManServer.Default.PabxSimulada)
                {
                    _TimerPabxSimulada.AutoReset = false;
                    _TimerPabxSimulada.Elapsed += OnTimePabxSimuladaElapsed;
                    _TimerPabxSimulada.Enabled = true;
                }
                else
                {
                    PabxUrl = "ws://" + PbxIp + ":" + Properties.u5kManServer.Default.PabxWsPort +
                        "/pbx/ws?login_user=sa&login_password=" + Properties.u5kManServer.Default.PabxSaPwd + "&user=*&registered=True&status=True&line=*";
                    _pabxws = new WebSocket(PabxUrl);
                    _pabxws.Opened += new EventHandler(websocket_Opened);
                    _pabxws.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(websocket_Error);
                    _pabxws.Closed += new EventHandler(websocket_Closed);
                    _pabxws.MessageReceived += new EventHandler<MessageReceivedEventArgs>(websocket_MessageReceived);
                    _pabxStatus = ePabxStatus.epsDesconectado;
                }

                _TimerPbax.Interval = 5000;
                _TimerPbax.AutoReset = false;
                _TimerPbax.Elapsed += OnTimePabxElapsed;

                Init();
                LogInfo<PabxItfService>(Name + ": Servicio Iniciado.");
                base.Start();   // salir = false; // Compatibilidad con IsRunning
                return true;
            }
            catch (Exception ex)
            {
                Stop(TimeSpan.FromSeconds(5));
                LogException<PabxItfService>("", ex);
            }
            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Stop(TimeSpan timeout)
        {
            Action stopbase = () => { base.Stop(TimeSpan.FromSeconds(5)); };

            List<Tuple<bool, Action>> ConditionalStopActions = new List<Tuple<bool, Action>>()
            {
                new Tuple<bool, Action>(_Status == ServiceStatus.Running, Dispose),
                new Tuple<bool, Action>(true, _WorkingThread.Stop),
                new Tuple<bool, Action>(true, stopbase),
            };
            
            LogInfo<PabxItfService>("Iniciando parada servicio.");

            ConditionalStopActions.ForEach(action =>
            {
                try
                {
                    if (action.Item1)
                        action.Item2();
                }
                catch (Exception x)
                {
                    LogException<PabxItfService>("Stoping Exception", x);
                }
            });
            _Status = ServiceStatus.Stopped;

            //try
            //{
            //    if (_Status == ServiceStatus.Running)
            //    {
            //        Dispose();
            //    }
            //    _WorkingThread.Stop();
            //    base.Stop();
            //}
            //catch (Exception x)
            //{
            //    LogException<PabxItfService>("", x);
            //}
            //finally 
            //{
            //    _Status = ServiceStatus.Stopped;
            //}
            LogInfo<PabxItfService>("Servicio Detenido.");
        }
        /// <summary>
        /// 
        /// </summary>
        public string PabxUrl
        {
            get;
            set;
        }


        #region Formatos de Tablas..

        /// <summary>
        /// 
        /// </summary>
        class pabxParamInfo
        {
            // Evento Register
            public string registered { get; set; }
            public string user { get; set; }
            // Evento Status,
            public long time { get; set; }
            public string other_number { get; set; }
            public string status { get; set; }
            // public string user { get; set; }
        };

        class pabxEvent
        {
            public string jsonrpc { get; set; }
            public string method { get; set; }
            public pabxParamInfo parametros { get; set; }
        };

        #endregion

        #region Private Members

        /// <summary>
        /// 
        /// </summary>
        enum ePabxStatus { epsDesconectado, epsConectando, epsConectado };
        /// <summary>
        /// 
        /// </summary>
        //private bool _Master = false;
        private ePabxStatus _pabxStatus = ePabxStatus.epsDesconectado;
        /// <summary>
        /// 
        /// </summary>
        private ServiceStatus _Status = ServiceStatus.Stopped;
        /// <summary>
        /// 
        /// </summary>
        private EventQueue _WorkingThread = new EventQueue();
        /// <summary>
        /// 
        /// </summary>
        private WebSocket _pabxws=null;
        /// <summary>
        /// 
        /// </summary>
        private Timer _TimerPbax = null;
        private Timer _TimerPabxSimulada = null;

        private bool HayPbx { get; set; }
        private string PbxIp { get; set; }

        #endregion

        #region Callbacks

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_Opened(object sender, EventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_opened");
            if (IsOperative)
            {
                //_WorkingThread.Enqueue("websocket_Opened", delegate()
                //{
                //    U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_opened enqueue");
                //    try
                //    {
                //        LogInfo<PabxItfService>("WebSocket Abierto en " + PabxUrl);
                //    }
                //    catch (Exception x)
                //    {
                //        LogException<PabxItfService>("", x);
                //    }
                //});
            }
            LogInfo<PabxItfService>("WebSocket Abierto en " + PabxUrl);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_error");
            if (IsOperative)
            {
                //_WorkingThread.Enqueue("websocket_Error", delegate()
                //{
                //    try
                //    {
                //        U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_error enqueue");
                //        LogError<PabxItfService>("WebSocketError en " + PabxUrl + ": " + e.Exception.Message);
                //    }
                //    catch (Exception x)
                //    {
                //        LogException<PabxItfService>("", x);
                //    }
                //});
            }
            LogError<PabxItfService>("WebSocketError en " + PabxUrl + ": " + e.Exception.Message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_Closed(object sender, EventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_closed");

            _WorkingThread.Enqueue("websocket_Closed", delegate()
            {
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_closed enqueue");
                if (U5kManService._std.wrAccAcquire())
                {
                    try
                    {
                        if (IsOperative)
                        {
                            _pabxStatus = ePabxStatus.epsDesconectado;

                            U5KStdGeneral stdg = U5kManService._std.STDG;

                            if (stdg.stdPabx.Estado != std.NoInfo)
                                RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_ERROR, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV",
                                    Params(idiomas.strings.PBX_Desconectada/*"PBX Desconectada"*/, "", "", ""));

                            stdg.stdPabx.Estado = std.NoInfo;
                            // stdg.stdPabx.name = idiomas.strings.PBX_Desconocida/*"Desconocido"*/;

                            /* Desregistrar. */
                            List<Uv5kManDestinosPabx.DestinoPabx> stdpbxs = U5kManService._std.STDPBXS;
                            stdpbxs.ForEach(d => d.Estado = std.NoInfo);

                            /** Actualizar Estados */
                            U5kManService._std.STDPBXS = stdpbxs;
                            U5kManService._std.STDG = stdg;
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<PabxItfService>("", x);
                    }
                    finally
                    {
                        U5kManService._std.wrAccRelease();
                    }
                }
            });
            LogInfo<PabxItfService>("WebSocket Cerrado en " + PabxUrl);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_msg");

            _WorkingThread.Enqueue("websocket_MessageReceived", delegate()
            {
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " websocket_msg enqueue");
                // Como este evento puede tocar la tabla de estado, adquiero el acceso.
                if (U5kManService._std.wrAccAcquire())
                {
                    try
                    {
                        if (IsOperative)
                        {
                            string msg = e.Message.Replace("params", "parametros");

                            if (msg.StartsWith("{"))
                            {
                                pabxEvent _evento = JsonConvert.DeserializeObject<pabxEvent>(msg);
                                ProcessEvent(_evento);
                            }
                            else if (msg.StartsWith("["))
                            {
                                pabxEvent[] _eventos = JsonConvert.DeserializeObject<pabxEvent[]>(msg);
                                foreach (pabxEvent _evento in _eventos)
                                {
                                    ProcessEvent(_evento);
                                }
                            }
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<PabxItfService>("", x);
                    }
                    finally
                    {
                        U5kManService._std.wrAccRelease();
                    }
                }
            });
            
            LogTrace<PabxItfService>(String.Format("WebSocket en {0}: Mensaje Recibido: {1}", PabxUrl, e.Message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimePabxElapsed(object sender, ElapsedEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " TimePbxElapsed");

            _WorkingThread.Enqueue("OnTimePabxElapsed", delegate()
            {
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " TimePbxElapsed enqueue");
                ConfigCultureSet();
                if (U5kManService._std.wrAccAcquire())
                {
                    try
                    {
                        if (IsOperative)
                        {
                            //U5kGenericos.SetCurrentCulture();
                            if (Properties.u5kManServer.Default.PabxSimulada == false)
                            {
                                switch (_pabxStatus)
                                {
                                    case ePabxStatus.epsDesconectado:
                                        if (Ping(PbxIp, _pabxStatus))
                                        {
                                            _pabxStatus = ePabxStatus.epsConectando;
                                            _pabxws.Open();
                                        }
                                        break;

                                    case ePabxStatus.epsConectando:
                                        break;

                                    case ePabxStatus.epsConectado:
                                        if (!Ping(PbxIp, _pabxStatus))
                                        {
                                            LogWarn<PabxItfService>("Fallo de Ping....Cierro WS");
                                            _pabxws.Close();
                                            _pabxStatus = ePabxStatus.epsDesconectado;

                                            U5KStdGeneral stdg = U5kManService._std.STDG;
                                            if (stdg.stdPabx.Estado != std.NoInfo)
                                                RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_ERROR, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV",
                                                    Params(idiomas.strings.PBX_Desconectada/*"PBX Desconectada"*/, "", "", ""));
                                            stdg.stdPabx.Estado = std.NoInfo;
                                            // stdg.stdPabx.name = idiomas.strings.PBX_Desconocida/*"Desconocido"*/;
                                            /* Desregistrar. */
                                            List<Uv5kManDestinosPabx.DestinoPabx> stdpbxs = U5kManService._std.STDPBXS;
                                            stdpbxs.ForEach(d => d.Estado = std.NoInfo);

                                            /** Actualizar Estados */
                                            U5kManService._std.STDPBXS = stdpbxs;
                                            U5kManService._std.STDG = stdg;
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception x)
                    {
                        LogException<PabxItfService>("", x);
                    }
                    finally
                    {
                        U5kManService._std.wrAccRelease();
                    }
                }
                _TimerPbax.Enabled = true;
            });

            IamAlive.Tick("PbxItfService-Timer", () =>
            {
                IamAlive.Message("PbxItfService-Timer. Is Alive.");
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        pabxParamInfo infoSimul = new pabxParamInfo()
        {
            user = Properties.u5kManServer.Default.PabxSimuladaRegisterSubcriber,
            registered = "false",
            status = "0"
        };
        pabxParamInfo infoSimulNoRegistrado = new pabxParamInfo()
        {
            user = Properties.u5kManServer.Default.PabxSimuladaUnregisterSubcriber,
            registered = "false",
            status = "0"
        };
        enum epabxSimulState { eStd1, eStd2, eStd3, eStd4 };
        epabxSimulState _simulState = epabxSimulState.eStd1;
        private void OnTimePabxSimuladaElapsed(object sender, ElapsedEventArgs e)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " TimePbxSim");
            if (IsOperative)
            {
                _WorkingThread.Enqueue("OnTimePabxSimuladaElapsed", delegate()
                {
                    U5kGenericos.TraceCurrentThread(this.GetType().Name + " TimePbxSim enqueue");
                    switch (_simulState)
                    {
                        case epabxSimulState.eStd1:
                            infoSimulNoRegistrado.registered = "true";
                            ProcessUserRegistered(infoSimulNoRegistrado);

                            infoSimul.registered = "true";
                            ProcessUserRegistered(infoSimul);
                            _simulState = epabxSimulState.eStd2;
                            break;

                        case epabxSimulState.eStd2:
                            infoSimul.status = "1";
                            ProcessUserStatus(infoSimul);
                            _simulState = epabxSimulState.eStd3;
                            break;
                        case epabxSimulState.eStd3:
                            infoSimul.status = "14";
                            ProcessUserStatus(infoSimul);
                            _simulState = epabxSimulState.eStd4;
                            break;
                        case epabxSimulState.eStd4:
                            infoSimul.status = "-1";
                            ProcessUserStatus(infoSimul);

                            infoSimulNoRegistrado.registered = "false";
                            ProcessUserRegistered(infoSimulNoRegistrado);

                            _simulState = epabxSimulState.eStd1;
                            break;
                    }

                    _TimerPabxSimulada.Enabled = true;
                });
            }
            else
            {
                _TimerPabxSimulada.Enabled = true;
            }
        }


        #endregion

        #region Private Functions.
        /// <summary>
        /// 
        /// </summary>
        private void Init()
        {
            LogInfo<PabxItfService>("INIT");

            if (Properties.u5kManServer.Default.PabxSimulada)
                _pabxStatus = ePabxStatus.epsConectado;
            else
            {
                _pabxStatus = ePabxStatus.epsConectando;
                _pabxws.Open();
            }

            _TimerPbax.Enabled = true;

        }
        /// <summary>
        /// 
        /// </summary>
        private void Dispose()
        {
            _TimerPbax.Enabled = false;

            if (Properties.u5kManServer.Default.PabxSimulada)
            {
                _TimerPabxSimulada.Enabled = false;
                _pabxStatus = ePabxStatus.epsDesconectado;
            }
            else
            {
                if (_pabxStatus == ePabxStatus.epsConectado)
                {
                    _pabxws.Close();
                    _pabxStatus = ePabxStatus.epsDesconectado;
                }
            }
            // AGL. Al ser llamada desde 'fuera', no utilizare el control de acceso para evitar Lazos no deseados...
            U5KStdGeneral stdg = U5kManService._std.STDG;
            stdg.stdPabx.Estado = std.NoInfo;
            // stdg.stdPabx.name = idiomas.strings.PBX_Desconocida/*"Desconocido"*/;
            U5kManService._std.STDG = stdg;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_event"></param>
        private void ProcessEvent(pabxEvent _event)
        {
            switch (_event.method)
            {
                case "notify_serverstatus":
                    ProcessServerStatus(_event.parametros);
                    break;
                case "notify_status":
                    ProcessUserStatus(_event.parametros);
                    break;
                case "notify_registered":
                    ProcessUserRegistered(_event.parametros);
                    break;
                default:
                    LogInfo<PabxItfService>("Evento No Procesado: " + _event.method);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void ProcessServerStatus(pabxParamInfo info)
        {
            switch (info.status)
            {
                case "active":
                   _pabxStatus = ePabxStatus.epsConectado;
                   U5KStdGeneral stdg = U5kManService._std.STDG;
                   if (stdg.stdPabx.Estado != std.Ok)
                   {
                       RecordEvent<PabxItfService>(DateTime.Now, U5kBaseDatos.eIncidencias.IGRL_U5KI_SERVICE_INFO, U5kBaseDatos.eTiposInci.TEH_SISTEMA, "SPV", 
                           Params(idiomas.strings.PBX_Conectada/*"PBX Conectada"*/, "", "", ""));
                   }
                   stdg.stdPabx.Estado = std.Ok;                   
                   stdg.stdPabx.name = String.Format("{0}:{1}", PbxIp, Properties.u5kManServer.Default.PabxWsPort);                   
                   U5kManService._std.STDG = stdg;
                   break;
                default:
                    _pabxws.Close();
                    _pabxStatus = ePabxStatus.epsDesconectado;
                    break;
            }
            LogInfo<PabxItfService>("Server Status=>" + info.status);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void ProcessUserRegistered(pabxParamInfo info)
        {
            bool registrado = info.registered == "true";
            // U5kManService._std.wrAccAcquire(); Viene de la rutina general del evento que ya ha tomado el Semaforo...
            List<Uv5kManDestinosPabx.DestinoPabx> pbxdes = U5kManService._std.STDPBXS;

            pbxdes.Where(d => d.Id == info.user).ToList().ForEach(d =>
            {
                std EstadoNotificado = registrado ? std.Ok : std.NoInfo;
                if (d.Estado != EstadoNotificado)
                {
                    d.Estado = EstadoNotificado;
                    // Generar Historico de Conexion / Desconexion...
                    RecordEvent<PabxItfService>(DateTime.Now, registrado ? U5kBaseDatos.eIncidencias.IPBX_SUBSC_ACTIVE : 
                        U5kBaseDatos.eIncidencias.IPBX_SUBSC_INACTIVE, U5kBaseDatos.eTiposInci.TEH_EXTERNO_TELEFONIA, info.user, Params());
                }
            });

            U5kManService._std.STDPBXS = pbxdes;
            LogDebug<PabxItfService>(String.Format("Procesado Registro Usuario {0}, {1}", Name, info.user, info.registered));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        private void ProcessUserStatus(pabxParamInfo info)
        {
            LogDebug<PabxItfService>(String.Format("Procesado Estado Usuario {1}, Estado: {2}", Name, info.user, info.status));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private bool Ping(string host, ePabxStatus current)
        {
            int maxReint = current == ePabxStatus.epsConectado ? 3 : 1;
            int reint = 0;
            PingReply reply;
            
            do
            {
                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128, 
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "Ulises V 5000i. PabxItfService..";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;  // ms
                reply = pingSender.Send(host, timeout, buffer, options);
                reint++;
                System.Threading.Thread.Sleep(10);
            } while (reply.Status != IPStatus.Success && reint < maxReint);

            return reply.Status == IPStatus.Success ? true : false;
        }

        /// <summary>
        /// 
        /// </summary>
        private void DesregistraTodos()
        {
            LogError<PabxItfService>("Invocando Rutina Obsoleta...");
        }
        
        #endregion

    }
}
