using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Xml;
using System.Xml.Serialization;
using System.Management;

namespace Utilities
{
    public class NICEventMonitor : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public enum LanStatus { Unknown=0, Up=1, Down=2 }
        /// <summary>
        /// 
        /// </summary>
        public enum TeamType { Marvell, Intel, Unknown }
        /// <summary>
        /// 
        /// </summary>
        public event Action<int> StatusChanged;
        public event Action<string> MessageError;
#if OLD
        /// <summary>
        /// 
        /// </summary>
        public LanStatus Lan1Status { get; set; }
        public LanStatus Lan2Status { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Lan1Device { get; set; }
        public string Lan2Device { get; set; }
#else
        public class NICItem : IEquatable<NICItem>
        {
            public int Index { get; set; }
            public string DeviceId { get; set; }
            public LanStatus Status { get; set; }
            public bool Equals(NICItem other)
            {
                return other.DeviceId == DeviceId;
            }
        }
        public List<NICItem> NICList = new List<NICItem>();
        /// <summary>
        /// 
        /// </summary>
        public class NicEventMonitorConfig
        {
            public string TeamingType { get; set; }
            public string WindowsLog { get; set; }
            public string EventSource { get; set; }
            public int UpEventId { get; set; }
            public int DownEventId { get; set; }

            public NicEventMonitorConfig() 
            {
                TeamingType = "Intel";
                WindowsLog = "System";
                EventSource = "iANSMiniport";
                UpEventId = 15;
                DownEventId = 11;
            }
            public NicEventMonitorConfig(string xml_string) : this()
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(NicEventMonitorConfig));
                using (TextReader reader = new StringReader(xml_string))
                {
                    try
                    {
                        object obj = deserializer.Deserialize(reader);
                        TeamingType = ((NicEventMonitorConfig)obj).TeamingType;
                        WindowsLog = ((NicEventMonitorConfig)obj).WindowsLog;
                        EventSource = ((NicEventMonitorConfig)obj).EventSource;
                        UpEventId = ((NicEventMonitorConfig)obj).UpEventId;
                        DownEventId = ((NicEventMonitorConfig)obj).DownEventId;
                    }
                    catch (Exception x)
                    {
                        throw new Exception(String.Format("NicEventMonitorConfig Exception {0}", x.Message));
                    }
                }
            }
            public string Serialize()
            {
                XmlSerializer serializer = new XmlSerializer(typeof(NicEventMonitorConfig));
                using (TextWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, this);
                    return writer.ToString();
                } 
            }
        }

#endif


        /// <summary>
        /// 
        /// </summary>
        /// <param name="lan1dev"></param>
        /// <param name="lan2dev"></param>
        public NICEventMonitor(string nicservice, bool fileInsteadLog, string logName = "System", string lan1dev = "Unkown", string lan2dev = "Unkown") 
        {
            try
            {
                NicService = nicservice;
                FileInsteadLog = fileInsteadLog;
                LogName = logName;
#if OLD
                Lan1Status = LanStatus.Unknown;
                Lan2Status = LanStatus.Unknown;
                Lan1Device = lan1dev;
                Lan2Device = lan2dev;
#endif
                UpEventId = 123;
                DownEventId = 83;
                LogEntriesGet();
                SearchForPhysicalDevices();
                InitialStatusGet();
            }
            catch (Exception x)
            {
                throw new Exception(String.Format("NICEventMonitor-1 Exception {0}", x.Message));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="teaming"></param>
        public NICEventMonitor(string teaming, string logger, string service, int evDown, int evUp)
        {
            try
            {
                Team = teaming == "Marvell" ? TeamType.Marvell :
                    teaming == "Intel" ? TeamType.Intel : TeamType.Unknown;

                NicService = service;
                UpEventId = evUp;
                DownEventId = evDown;

                LogName = logger;
                FileInsteadLog = false;

                if (Team != TeamType.Unknown)
                {
#if DEBUG1
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
#endif
                    LogEntriesGet();
#if DEBUG1
                    LogManager.GetCurrentClassLogger().Fatal(String.Format("LogEntries. {0} ms, {1} ticks. {2} s.",
                        sw.ElapsedMilliseconds, sw.ElapsedTicks, (float)((float)sw.ElapsedTicks / (float)Stopwatch.Frequency)));
#endif
                    SearchForPhysicalDevices();
#if DEBUG1
                    LogManager.GetCurrentClassLogger().Fatal(String.Format("SearchForPhysicalDevices. {0} ms, {1} ticks. {2} s.",
                        sw.ElapsedMilliseconds, sw.ElapsedTicks, (float)((float)sw.ElapsedTicks / (float)Stopwatch.Frequency)));
#endif
                    InitialStatusGet();
#if DEBUG1
                    LogManager.GetCurrentClassLogger().Fatal(String.Format("InitialStatusGet. {0} ms, {1} ticks. {2} s.",
                        sw.ElapsedMilliseconds, sw.ElapsedTicks, (float)((float)sw.ElapsedTicks / (float)Stopwatch.Frequency)));
#endif

                    //watcher = new EventLogWatcher(new EventLogQuery(LogName, FileInsteadLog ? PathType.FilePath : PathType.LogName));
                    //watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
                }

#if _OLD_
                if (teaming == "Marvell")
                {
                    Team = TeamType.Marvell;
                    LogName = "System";

                    FileInsteadLog = false;

                    NicService = "yukonw7";             // TODO. En config...
                    UpEventId = 123;
                    DownEventId = 83;

                    LogEntriesGet();
                    SearchForPhysicalDevices();
                }
                else if (teaming == "Intel")
                {
                    Team = TeamType.Intel;
                    LogName = "System";

                    FileInsteadLog = false;

                    NicService = "e1rexpress";
                    UpEventId = 32;
                    DownEventId = 27;

                    LogEntriesGet();
                    SearchForPhysicalDevices();
                }
                else
                {
                    Team = TeamType.Unknown;
                    NicService = "UNKNOWN";
                    FileInsteadLog = false;
                    LogName = "System";

                    UpEventId = 123;
                    DownEventId = 83;
                    return;
                }
                watcher = new EventLogWatcher(new EventLogQuery(LogName, FileInsteadLog ? PathType.FilePath : PathType.LogName));
                watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
#endif
            }
            catch (Exception x)
            {
                throw new Exception(String.Format("NICEventMonitor-2 Exception {0}", x.Message));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        public NICEventMonitor(NicEventMonitorConfig cfg) :
            this(cfg.TeamingType, cfg.WindowsLog, cfg.EventSource, cfg.DownEventId, cfg.UpEventId)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            try
            {
                if ( watcher==null )
                {
                    watcher = new EventLogWatcher(new EventLogQuery(LogName, FileInsteadLog ? PathType.FilePath : PathType.LogName));
                    watcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
                    watcher.Enabled = true;
                }
            }
            catch (Exception x)
            {
                RaiseMessageError(x.Message);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (watcher != null && watcher.Enabled == true)
                {
                    watcher.EventRecordWritten -= new EventHandler<EventRecordWrittenEventArgs>(watcher_EventRecordWritten);
                    watcher = null;
                    watcher.Enabled = false;
#if OLD
                    Lan1Status = Lan1Status == LanStatus.Unknown ? LanStatus.Unknown : LanStatus.Down;
                    Lan2Status = Lan2Status == LanStatus.Unknown ? LanStatus.Unknown : LanStatus.Down;
#endif
                }
                NICList.Clear();
            }
            catch (Exception x)
            {
                RaiseMessageError(x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lan">0: LAN1, 1: LAN2</param>
        /// <param name="status"></param>
        public void EventSimulate(int lan, bool status) 
        {
            try
            {
#if OLD
                using (EventLog eventLog = new EventLog(LogName))
                {
                    eventLog.Source = "EventSimulate";

                    eventLog.WriteEntry(String.Format("Log message {0} example", lan == 0 ? Lan1Device : Lan2Device),
                        EventLogEntryType.Information,
                        status == false ? DownEventId : UpEventId, 1);

                    eventLog.Close();
                }
#else
                if (lan < NICList.Count)
                {
                    using (EventLog eventLog = new EventLog(LogName))
                    {
                        eventLog.Source = "EventSimulate";
                        eventLog.WriteEntry(String.Format("Log message {0} example", NICList[lan].DeviceId),
                            EventLogEntryType.Information,
                            status == false ? DownEventId : UpEventId, 1);

                        eventLog.Close();
                    }
                }
#endif
            }
            catch (Exception x)
            {
                RaiseMessageError(x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void watcher_EventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord.Id == UpEventId)
            {
#if OLD
                if (e.EventRecord.ToXml().Contains(Lan1Device) == true)
                {
                    Lan1Status = LanStatus.Up;
                    RaiseStatusChanged(0);
                }
                else if (e.EventRecord.ToXml().Contains(Lan2Device) == true)
                {
                    Lan2Status = LanStatus.Up;
                    RaiseStatusChanged(1);
                }
                else
                {
                    RaiseMessageError(e.EventRecord.ToXml());
                }
#elif OLD_1
                for (int lan = 0; lan<NICList.Count; lan++)
                {
                    if (e.EventRecord.ToXml().Contains(NICList[lan].DeviceId) == true)
                    {
                        NICList[lan].Status = LanStatus.Up;
                        RaiseStatusChanged(lan);
                        break;
                    }
                }
#else
                string idLan = e.EventRecord.Properties[0].Value.ToString();
                NICItem lan = NICList.Where(nic => nic.DeviceId == idLan).FirstOrDefault();
                lan.Status = LanStatus.Up;
                RaiseStatusChanged(lan.Index);

#endif
                RaiseMessageError(String.Format("UpEventRecord: {0}", e.EventRecord.ToXml()));
            }
            else if (e.EventRecord.Id == DownEventId)
            {
#if OLD
                if (e.EventRecord.ToXml().Contains(Lan1Device) == true)
                {
                    Lan1Status = LanStatus.Down;
                    RaiseStatusChanged(0);
                }
                else if (e.EventRecord.ToXml().Contains(Lan2Device) == true)
                {
                    Lan2Status = LanStatus.Down;
                    RaiseStatusChanged(0);
                }
                else
                {
                    RaiseMessageError(e.EventRecord.ToXml());
                }
#elif OLD_1
                for (int lan = 0; lan < NICList.Count; lan++)
                {
                    if (e.EventRecord.ToXml().Contains(NICList[lan].DeviceId) == true)
                    {
                        NICList[lan].Status = LanStatus.Down;
                        RaiseStatusChanged(lan);
                        break;
                    }
                }
#else
                string idLan = e.EventRecord.Properties[0].Value.ToString();
                NICItem lan = NICList.Where(nic => nic.DeviceId == idLan).FirstOrDefault();
                lan.Status = LanStatus.Down;
                RaiseStatusChanged(lan.Index);
#endif
                RaiseMessageError(String.Format("DownEventRecord: {0}", e.EventRecord.ToXml()));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void ScanForDevices()
        {
            try
            {
                ManagementObjectSearcher mos = null;
                mos = new ManagementObjectSearcher(@"SELECT * FROM   Win32_NetworkAdapter WHERE  Manufacturer != 'Microsoft'");
                IList<ManagementObject> managementObjectList = mos.Get()
                                                                  .Cast<ManagementObject>()
                                                                  .OrderBy(p => Convert.ToUInt32(p.Properties["Index"].Value))
                                                                  .ToList();
                foreach (ManagementObject mo in managementObjectList)
                {
                    Console.Clear();
                    foreach (PropertyData pd in mo.Properties)
                    {
                        Console.WriteLine(pd.Name + ": " + (pd.Value ?? "N/A"));
                    }
                    Console.ReadKey(true);
                }
            }
            catch (Exception x)
            {
                RaiseMessageError(x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Info()
        {
            return String.Format("LogName: {0}, NicService: {1}, UpEventId: {2}, DownEventId: {3}", LogName, NicService, UpEventId, DownEventId);
        }

        /// <summary>
        /// 
        /// </summary>
        protected void SearchForPhysicalDevices()
        {
            try
            {
#if OLD_1
                ManagementObjectSearcher mos = null;
                mos = new ManagementObjectSearcher(@"SELECT * FROM   Win32_NetworkAdapter WHERE  Manufacturer != 'Microsoft'");
                IList<ManagementObject> managementObjectList = mos.Get()
                                                                  .Cast<ManagementObject>()
                                                                  .Where(p => (p.Properties["ServiceName"].Value != null && p.Properties["ServiceName"].Value.ToString().ToLower() == NicService.ToLower()))
                                                                  .OrderBy(p => Convert.ToUInt32(p.Properties["Index"].Value))
                                                                  .ToList();
                
                LogManager.GetCurrentClassLogger().Info("SearchForPhysicalDevices {0}", managementObjectList.Count);

                int nLan = 0;
                foreach (ManagementObject mo in managementObjectList)
                {
                    string LanDevice = mo.Properties["MACAddress"].Value.ToString().Replace(":","");
#if OLD
                    Lan1Device = nLan == 0 ? LanDevice : Lan1Device;
                    Lan1Status = nLan == 0 ? LanStatus.Down : Lan1Status;

                    Lan2Device = nLan == 1 ? LanDevice : Lan2Device;
                    Lan2Status = nLan == 1 ? LanStatus.Down : Lan2Status;
                    LogManager.GetCurrentClassLogger().Info("SearchForPhysicalDevices. Device {0}: {1}", nLan, LanDevice);
#else
                    NICList.Add(new NICItem() { DeviceId = LanDevice, Status = LanStatus.Down });
#endif
                    nLan++;
                }
#else
                int Index = 0;
                NICList.Clear();
                foreach (EventRecord evento in _LogEntries)
                {
                    if (evento.Properties.Count > 0)
                    {
                        string idDevice = Team == TeamType.Marvell ? evento.Properties[0].Value.ToString() :
                        Team == TeamType.Intel ? evento.Properties[1].Value.ToString() : "";
#if DEBUG
                        if (NICList.Contains(new NICItem() { DeviceId = idDevice }) == false)
#else
                        if (NICList.Count < 2 && NICList.Contains(new NICItem() { DeviceId = idDevice }) == false)
#endif
                        {
                            NICList.Add(new NICItem() { DeviceId = idDevice, Status = LanStatus.Down, Index = Index++ });
                        }
                    }
                }
                NICList = NICList.OrderBy(lan => lan.DeviceId).ToList();
#endif
            }
            catch (Exception x)
            {
                RaiseMessageError(x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        protected string DeviceGetFromXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement.SelectSingleNode(".EventData");
            return node != null ? node.InnerText : "Not Found";
        }

        /// <summary>
        /// 
        /// </summary>
        protected void InitialStatusGet()
        {
            try
            {
                /** Leer la lista de eventos */
#if OLD_1
                EventLogQuery logquery = new EventLogQuery(LogName, FileInsteadLog ? PathType.FilePath : PathType.LogName);

                List<EventRecord> Entries = new List<EventRecord>();
                EventLogReader elr = new EventLogReader(logquery);
                EventRecord entry;
                while ((entry = elr.ReadEvent()) != null)
                {
                    if (entry.Id == DownEventId || entry.Id == UpEventId)
                        Entries.Add(entry);
                }
#endif
#if OLD
                /** Separarlos */
                List<EventRecord> last_lan1_ev_down = Entries.Where(e => DownEventId == (e.Id) && e.ToXml().Contains(Lan1Device)).OrderByDescending(e => e.TimeCreated).ToList();
                List<EventRecord> last_lan2_ev_down = Entries.Where(e => DownEventId == (e.Id) && e.ToXml().Contains(Lan2Device)).OrderByDescending(e => e.TimeCreated).ToList();
                List<EventRecord> last_lan1_ev_up = Entries.Where(e => UpEventId == (e.Id) && e.ToXml().Contains(Lan1Device)).OrderByDescending(e => e.TimeCreated).ToList();
                List<EventRecord> last_lan2_ev_up = Entries.Where(e => UpEventId == (e.Id) && e.ToXml().Contains(Lan2Device)).OrderByDescending(e => e.TimeCreated).ToList();

                long last_lan1_down = last_lan1_ev_down.Count == 0 ? DateTime.MinValue.Ticks : last_lan1_ev_down[0].TimeCreated.Value.Ticks;
                long last_lan2_down = last_lan2_ev_down.Count == 0 ? DateTime.MinValue.Ticks : last_lan2_ev_down[0].TimeCreated.Value.Ticks;
                long last_lan1_up = last_lan1_ev_up.Count == 0 ? DateTime.MinValue.Ticks : last_lan1_ev_up[0].TimeCreated.Value.Ticks;
                long last_lan2_up = last_lan2_ev_up.Count == 0 ? DateTime.MinValue.Ticks : last_lan2_ev_up[0].TimeCreated.Value.Ticks;

                Lan1Status = last_lan1_up > last_lan1_down ? LanStatus.Up : LanStatus.Down;
                Lan2Status = last_lan2_up > last_lan2_down ? LanStatus.Up : LanStatus.Down; 
#elif OLD_1
                for (int lan = 0; lan < NICList.Count; lan++)
                {
                    string LanDevice = NICList[lan].DeviceId;
                    List<EventRecord> last_lan_ev_down = Entries.Where(e => DownEventId == (e.Id) && e.ToXml().Contains(LanDevice)).OrderByDescending(e => e.TimeCreated).ToList();
                    List<EventRecord> last_lan_ev_up = Entries.Where(e => UpEventId == (e.Id) && e.ToXml().Contains(LanDevice)).OrderByDescending(e => e.TimeCreated).ToList();

                    long last_lan_down = last_lan_ev_down.Count == 0 ? DateTime.MinValue.Ticks : last_lan_ev_down[0].TimeCreated.Value.Ticks;
                    long last_lan_up = last_lan_ev_up.Count == 0 ? DateTime.MinValue.Ticks : last_lan_ev_up[0].TimeCreated.Value.Ticks;

                    NICList[lan].Status = last_lan_up > last_lan_down ? LanStatus.Up : LanStatus.Down;
                }
#else
                for (int lan = 0; lan < NICList.Count; lan++)
                {
                    string LanDevice = NICList[lan].DeviceId;
                    List<EventRecord> last_lan_ev_down = _LogEntries.Where(e => DownEventId == (e.Id) && e.ToXml().Contains(LanDevice)).OrderByDescending(e => e.TimeCreated).ToList();
                    List<EventRecord> last_lan_ev_up = _LogEntries.Where(e => UpEventId == (e.Id) && e.ToXml().Contains(LanDevice)).OrderByDescending(e => e.TimeCreated).ToList();

                    long last_lan_down = last_lan_ev_down.Count == 0 ? DateTime.MinValue.Ticks : last_lan_ev_down[0].TimeCreated.Value.Ticks;
                    long last_lan_up = last_lan_ev_up.Count == 0 ? DateTime.MinValue.Ticks : last_lan_ev_up[0].TimeCreated.Value.Ticks;

                    NICList[lan].Status = last_lan_up > last_lan_down ? LanStatus.Up : LanStatus.Down;
                }
#endif
            }
            catch (Exception x)
            {
                RaiseMessageError(x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected void LogEntriesGet()
        {
            string evtQuery = String.Format("*[System[(Provider/@Name=\"{0}\") and ((EventID={1}) or (EventID={2}) )]]", NicService, DownEventId, UpEventId);
#if DEBUG
            //string base_path = System.AppDomain.CurrentDomain.BaseDirectory;
            //string strQuery = Team == TeamType.Intel ? "d:\\Datos\\Empresa\\_SharedPrj\\UlisesV5000i-trunk-Unificada\\ulises-man\\seguimientos\\servidores.evtx" :
            //    "d:\\Datos\\Empresa\\_SharedPrj\\UlisesV5000i-MN\\Incidencias\\20161004. Misma MAC en ambos NIC Marvell\\eventos-basico.evtx";
            string strQuery = Team == TeamType.Intel ? "..\\..\\..\\DebugData\\servidores.evtx" :
                "..\\..\\..\\DebugData\\eventos-basico.evtx";
            EventLogQuery logquery = new EventLogQuery(strQuery, PathType.FilePath, evtQuery);
#else
            EventLogQuery logquery = new EventLogQuery(LogName, FileInsteadLog ? PathType.FilePath : PathType.LogName, evtQuery);
#endif

            EventLogReader elr = new EventLogReader(logquery);
            EventRecord entry;

            _LogEntries.Clear();
            while ((entry = elr.ReadEvent()) != null)
            {
                //if (entry.ProviderName==NicService && ( entry.Id == DownEventId || entry.Id == UpEventId))
                    _LogEntries.Add(entry);
            }
            _LogEntries = _LogEntries.OrderByDescending(e => e.TimeCreated).ToList();  
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mensaje"></param>
        private void RaiseMessageError(string mensaje)
        {
            if (MessageError != null) MessageError(mensaje);            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lan"></param>
        private void RaiseStatusChanged(int lan)
        {
            if (StatusChanged != null) StatusChanged(lan);
        }
        /// <summary>
        /// 
        /// </summary>
        private EventLogWatcher watcher = null;
        private bool FileInsteadLog { get; set; }
        private string LogName { get; set; }
        private string NicService { get; set; }
        private int UpEventId { get; set; }
        private int DownEventId { get; set; }
        private List<EventRecord> _LogEntries = new List<EventRecord>();
        private TeamType Team { get; set; }
    }
}
