using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;

using Utilities;

namespace U5kManServer.Services
{
    public class CentralServicesMonitor : IDisposable
    {
        private string _radioMNDataString="[]";
        private string _presenceDataString= "{}";
        private string _phoneDataString ="{}";
        private string _radioSessionsString = "[]";
        private string _hFRadioDataString ="{}";

        public class ServerDataAndState
        {
            public string ip { get; set; }
            public string url { get; set; }

            public string Machine { get; set; }
            public string ServerType { get; set; }
            public string GlobalMaster { get; set; }
            public string RadioService { get; set; }
            public string CfgService { get; set; }
            public string PhoneService { get; set; }
            public string TifxService { get; set; }
            public string PresenceService { get; set; }
            public string WebPort { get; set; }

            public DateTime TimeStamp { get; set; }
        };

        static TimeSpan smpTimeout = TimeSpan.FromSeconds(5);

        static TimeSpan mainTick = TimeSpan.FromSeconds(1);
        static TimeSpan notResponingTick = TimeSpan.FromSeconds(5);
        static TimeSpan notResponingTimeout = TimeSpan.FromSeconds(10);
        static TimeSpan globalStateTick = TimeSpan.FromSeconds(5);
        static TimeSpan globalStateAlarmTimeout = TimeSpan.FromSeconds(30);
        static TimeSpan operationalDataTick = TimeSpan.FromSeconds(10);

        #region Propiedades

        public static CentralServicesMonitor Monitor { get; set; }
        public string RadioSessionsString
        {
            get
            {
                if (GetDataAccess())
                {
                    var ret = _radioSessionsString;
                    ReleaseDataAccess();
                    return ret;
                }
                return "[]";
            }
            set => _radioSessionsString = value;
        }
        public string RadioMNDataString
        {
            get
            {
                if (GetDataAccess())
                {
                    var ret = _radioMNDataString;
                    ReleaseDataAccess();
                    return ret;
                }
                return "{}";
            }
            set => _radioMNDataString = value;
        }
        public string PresenceDataString
        {
            get
            {
                if (GetDataAccess())
                {
                    var ret = _presenceDataString;
                    ReleaseDataAccess();
                    return ret;
                }
                return "{}";
            }
            set => _presenceDataString = value;
        }
        public string PhoneDataString
        {
            get
            {
                if (GetDataAccess())
                {
                    var ret = _phoneDataString;
                    ReleaseDataAccess();
                    return ret;
                }
                return "{}";
            }
            set => _phoneDataString = value;
        }
        public string HFRadioDataString
        {
            get
            {
                if (GetDataAccess())
                {
                    var ret = _hFRadioDataString;
                    ReleaseDataAccess();
                    return ret;
                }
                return "{}";
            }
            set => _hFRadioDataString = value;
        }

        #endregion

        #region Constructores

        public CentralServicesMonitor(Func<bool> masterStateInfo,
            Action<bool, string, string, string> internalEvent,
            Action<String, Exception> notify,
            Action<int, String> trace = null, int Port = 8103)
        {
            SmpAccess = new Semaphore(1, 1);
            UdpServer = new UdpClient(Port);
            DataAndStates = new Dictionary<String, ServerDataAndState>();
            SpvTask = null;

            //
            MasterStateInfo = masterStateInfo;
            InternalEvent = internalEvent;
            Notify = notify;
            Trace = trace;

            Monitor = this;
        }

        public void Dispose()
        {
            if (SpvTask != null && GetDataAccess())
            {
                SpvTask = null;
                UdpServer.Close();
                ReleaseDataAccess();
            }
        }

        #endregion

        #region Metodos Publicos

        public void Start()
        {
            if (SpvTask == null)
            {
                UdpServer.BeginReceive(ReceiveCallback, null);
                SpvTask = Task.Factory.StartNew(SupervisionCallback);
            }
        }

        public void DataGetForSnmpAgent(Action<string, string, string, string> cb)
        {
            if (GetDataAccess())
            {
                var idRadio = RadioServer != null ? RadioServer.ip : "???";
                var idPhone = PhoneServer != null ? PhoneServer.ip : "???";
                var stdRadio = GlobalRadioServiceState.ToString();
                var stdPhone = GlobalPhoneServiceState.ToString();
                ReleaseDataAccess();

                cb(idRadio, stdRadio, idPhone, stdPhone);
            }
        }

        public void DataGetForWebServer(Action<object> cb)
        {
            if (GetDataAccess())
            {
                var ret = new
                {
                    radio = new
                    {
                        std = GlobalRadioServiceState.ToString(),    
                        mas = RadioServer == null ? "" : RadioServer.ip,
                        rdsl = DataAndStates.Where(e => e.Value.ServerType == "Radio").Select(e => e)
                    },
                    phone = new
                    {
                        std = GlobalPhoneServiceState.ToString(),
                        mas = PhoneServer == null ? "" : PhoneServer.ip,
                        phsl = DataAndStates.Where(e => e.Value.ServerType == "Phone").Select(e => e)
                    }
                };

                ReleaseDataAccess();

                cb(ret);
            }
        }

        #endregion

        #region Callbacks
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ar"></param>
        void ReceiveCallback(IAsyncResult ar)
        {
            Task.Factory.StartNew(() =>
            {
                if (UdpServer != null && UdpServer.Client != null)
                {
                    try
                    {
                        IPEndPoint remote = null;
                        byte[] data = UdpServer.EndReceive(ar, ref remote);
                        var str = System.Text.Encoding.Default.GetString(data);
                        var not = JsonConvert.DeserializeObject<ServerDataAndState>(str);

                        not.ip = remote.Address.ToString();
                        not.url = "http://" + not.ip + ":" + not.WebPort + "/";

                        var key = string.Format("{0}#{1}", not.ip, not.ServerType);

                        if (MasterStateInfo() && GetDataAccess())
                        {
                            try
                            {
                                if (DataAndStates.Keys.Contains(key) == false)
                                {
                                    /** Evento de Activacion de server */
                                    RaiseInternalEvent(false, not.ServerType, not.Machine, "Activado");
                                }
                                /** Actualizo la tabla */
                                DataAndStates[key] = not;
                            }
                            catch (Exception x)
                            {
                                RaiseMessage(x.Message, x);
                            }
                            ReleaseDataAccess();
                        }

                        TraceMsg(3, String.Format("Frame Received from {0} en {1}", not.ServerType, not.ip));
                    }
                    catch (Exception x)
                    {
                        RaiseMessage(x.Message, x);
                    }
                    finally
                    {
                        if (UdpServer != null && UdpServer.Client != null)
                            UdpServer.BeginReceive(ReceiveCallback, null);
                        else
                        {
                            RaiseMessage("UdpServer No valido...");
                        }
                    }
                }
                else
                {
                    RaiseMessage("UdpServer No valido...");
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        void SupervisionCallback()
        {
            DateTime lastOperationalRadioData = DateTime.MinValue;
            DateTime lastOperationalPhoneData = DateTime.MinValue;
            DateTime lastGlobalStateSupervision = DateTime.MinValue;
            DateTime lastNotRespondingSupervision = DateTime.MinValue;

            /** */
            //NucleoGeneric.BaseCode.ConfigCultureSet();

            do
            {
                if (MasterStateInfo() && GetDataAccess())
                {
                    try
                    {
                        lastNotRespondingSupervision = NotRespondingSupervision(lastNotRespondingSupervision);

                        lastGlobalStateSupervision = GlobalStateSupervision(lastGlobalStateSupervision);

                        lastOperationalRadioData = OperationalRadioDataGet(lastOperationalRadioData);

                        lastOperationalPhoneData = OperationalPhoneDataGet(lastOperationalPhoneData);
                    }
                    catch (Exception x)
                    {
                        RaiseMessage(x.Message, x);
                    }

                    ReleaseDataAccess();
                }
                Task.Delay(mainTick).Wait();

            } while (SpvTask != null);
        }

        #endregion

        #region Metodos Privados
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastSup"></param>
        /// <returns></returns>
        DateTime NotRespondingSupervision(DateTime lastSup)
        {
            TimeSpan ElapsedTime = DateTime.Now - lastSup;
            if (ElapsedTime > notResponingTick)
            {
                var quiets = DataAndStates.Where(e => DateTime.Now - e.Value.TimeStamp > notResponingTimeout).
                    Select(e => e.Key).ToList();
                quiets.ForEach(k =>
                {
                    /** Evento de Desactivacion de server */
                    var not = DataAndStates[k];
                    RaiseInternalEvent(false, not.ServerType, not.Machine, "Desactivado");

                    DataAndStates.Remove(k);
                });

                lastSup = DateTime.Now;
            }
            return lastSup;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastSup"></param>
        /// <returns></returns>
        DateTime GlobalStateSupervision(DateTime lastSup)
        {
            TimeSpan ElapsedTime = DateTime.Now - lastSup;
            if (ElapsedTime > globalStateTick) 
            {
                /** Supervisa el Radio Server */
                RadioServerState.Supervises(RadioServer, () =>
                {
                    RaiseInternalEvent(true, "RadioServer", "", "No Server Found");
                });

                /** Supervisa el servidor de telefonia (presencia) */
                PhoneServerState.Supervises(PhoneServer, () =>
                {
                    RaiseInternalEvent(true, "PhoneServer", "", "No Server Found");
                });

                lastSup = DateTime.Now;
            }
            return lastSup;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastGet"></param>
        /// <returns></returns>
        Task GettingDataOfRadio = null;
        DateTime OperationalRadioDataGet(DateTime lastGet)
        {
            if (GettingDataOfRadio == null)
            {
                TimeSpan ElapsedTime = DateTime.Now - lastGet;
                if (ElapsedTime > operationalDataTick)
                {
                    GettingDataOfRadio = Task.Factory.StartNew(() =>
                    {
                        var currentMasters = DataAndStates.Where(e => e.Value.RadioService == "Master");
                        if (currentMasters != null && currentMasters.Count() > 0)
                        {
                            ServerDataAndState master = currentMasters.First().Value;

                            TraceMsg(2, "Getting Data Of Radio...");

                            string LocalRadioSessionsString = HttpHelper.Get(master.ip, master.WebPort, "/rdsessions", "[]");
                            string LocalRadioMNDataString = HttpHelper.Get(master.ip, master.WebPort, "/gestormn", "[]");
                            string LocalHFRadioDataString = HttpHelper.Get(master.ip, master.WebPort, "/rdhf", "{}");

                            if (GetDataAccess())
                            {
                                RadioSessionsString = LocalRadioSessionsString;
                                RadioMNDataString = LocalRadioMNDataString;
                                HFRadioDataString = LocalHFRadioDataString;

                                TraceMsg(2, "Data Of Radio READY...");
                                ReleaseDataAccess();
                            }
                        }
                        GettingDataOfRadio = null;
                    });
                    lastGet = DateTime.Now;
                }
            }
            return lastGet;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lastGet"></param>
        /// <returns></returns>
        Task GettingDataOfTelephony = null;
        DateTime OperationalPhoneDataGet(DateTime lastGet)
        {
            if (GettingDataOfTelephony == null)
            {
                TimeSpan ElapsedTime = DateTime.Now - lastGet;
                if (ElapsedTime > operationalDataTick)
                {
                    GettingDataOfTelephony = Task.Factory.StartNew(() =>
                    {
                        var currentMasters = DataAndStates.Where(e => e.Value.PresenceService == "Master");
                        if (currentMasters != null && currentMasters.Count() > 0)
                        {
                            ServerDataAndState master = currentMasters.First().Value;

                            TraceMsg(2, "Getting Data Of Telephony...");

                            string LocalPresenceDataString = HttpHelper.Get(master.ip, master.WebPort, "/tifxinfo");
                            string LocalPhoneDataString = HttpHelper.Get(master.ip, master.WebPort, "/phone");

                            if (GetDataAccess())
                            {
                                PresenceDataString = LocalPresenceDataString;
                                PhoneDataString = LocalPhoneDataString;

                                TraceMsg(2, "Data Of Telephony READY...");
                                ReleaseDataAccess();
                            }
                        }
                        GettingDataOfTelephony = null;
                    });

                    lastGet = DateTime.Now;
                }
            }
            return lastGet;
        }
        /// <summary>
        /// 
        /// </summary>
        protected ServerDataAndState RadioServer
        {
            get
            {
                /** Busco el servidor radio */
                var currentMasters = DataAndStates.Where(e => e.Value.RadioService == "Master");
                return (currentMasters != null && currentMasters.Count() > 0) ? currentMasters.First().Value : null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected ServerDataAndState PhoneServer
        {
            get
            {
                /** Busco el servidor radio */
                var currentMasters = DataAndStates.Where(e => e.Value.PresenceService == "Master");
                return (currentMasters != null && currentMasters.Count() > 0) ? currentMasters.First().Value : null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private ServiceStates GlobalRadioServiceState
        {
            get
            {
                int total = DataAndStates.Where(e => e.Value.ServerType == "Radio").Count();
                int master = DataAndStates.Where(e => e.Value.ServerType == "Radio" && e.Value.RadioService=="Master").Count();
                return master == 1 && total > 1 ? ServiceStates.Ok :
                    master == 1 && total == 1 ? ServiceStates.Warning : ServiceStates.Alarm;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private ServiceStates GlobalPhoneServiceState
        {
            get
            {
                int total = DataAndStates.Where(e => e.Value.ServerType == "Phone").Count();
                int master = DataAndStates.Where(e => e.Value.ServerType == "Phone" && e.Value.PresenceService == "Master").Count();
                return master == 1 && total > 1 ? ServiceStates.Ok :
                    master == 1 && total == 1 ? ServiceStates.Warning : ServiceStates.Alarm;
            }
        }

        string data_access_owner = "";
        protected bool GetDataAccess([CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            if (SmpAccess.WaitOne(smpTimeout))
            {
                data_access_owner = String.Format("[{0},{1}]", caller, lineNumber);
                return true;
            }
            RaiseMessage(String.Format("GetDataAccess from [{0},{1}] => Semaphore Timeout ({2}) ...", caller, lineNumber, data_access_owner));
            return false;
        }

        protected void ReleaseDataAccess()
        {
            data_access_owner = "";
            SmpAccess.Release();
        }

        protected void RaiseMessage(string msg, Exception x = null, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            string exString = String.Format("RaiseErrorMessage from [{0},{1}] => {2}", caller, lineNumber, msg);
            Task.Factory.StartNew(() =>
            {
                Notify(exString, x);
            });
        }

        protected void TraceMsg(int level, string msg)
        {
            Trace?.Invoke(level, msg);
        }

        protected void RaiseInternalEvent(bool alarma=false, string str1="", string str2="", string str3="")
        {
            Task.Factory.StartNew(() =>
            {
                InternalEvent?.Invoke(alarma, str1, str2, str3);
            });
        }

        #endregion Metodos Privados.

        #region Atributos privados

        private UdpClient UdpServer { get; set; }
        Task SpvTask { get; set; }
        Dictionary<String, ServerDataAndState> DataAndStates { get; set; }
        Semaphore SmpAccess { get; set; }

        enum GlobalStates { Inicio, Ok, Fallo }
        enum ServiceStates { Ok = 0, Warning = 1, Alarm = 2 }
        class GlobalStateItem
        {
            public GlobalStates State { get; set; }
            public DateTime DateOfChange { get; set; }
            public void Supervises(ServerDataAndState ItemServer, Action cb)
            {
                switch (State)
                {
                    case GlobalStates.Inicio:
                        if (ItemServer == null)
                        {
                            if (DateTime.Now - DateOfChange > globalStateAlarmTimeout)
                            {
                                // Alarma No tenemos servidor de Radio                           
                                State = GlobalStates.Fallo;
                                DateOfChange = DateTime.Now;
                                cb();
                            }
                        }
                        else
                        {
                            State = GlobalStates.Fallo;
                            DateOfChange = DateTime.Now;
                        }
                        break;
                    case GlobalStates.Ok:
                        if (ItemServer == null)
                        {
                            State = GlobalStates.Fallo;
                            DateOfChange = DateTime.Now;
                            cb();
                        }
                        break;

                    case GlobalStates.Fallo:
                        if (ItemServer != null)
                        {
                            State = GlobalStates.Ok;
                            DateOfChange = DateTime.Now;
                        }
                        break;
                }

            }
        };
        GlobalStateItem RadioServerState = new GlobalStateItem() { State = GlobalStates.Inicio, DateOfChange = DateTime.Now };
        GlobalStateItem PhoneServerState = new GlobalStateItem() { State = GlobalStates.Inicio, DateOfChange = DateTime.Now };

        readonly Action<String, Exception> Notify;
        readonly Action<bool, string, string, string> InternalEvent;
        readonly Func<bool> MasterStateInfo;
        readonly Action<int, string> Trace;

        #endregion Atributos Privados
    }
}
