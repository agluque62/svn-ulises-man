using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization;

using NLog;

using U5kBaseDatos;
using U5kManServer;
using Utilities;

namespace NucleoGeneric
{
    /// <summary>
    /// Objecto inicial del arbol de objetos.
    /// </summary>
    public class BaseCode
    {

        /** 20160905. AGL. Localizacion del Servicio. */
        public String ServiceSite => System.Environment.MachineName;
        public BaseCode()
        {
        }

        #region Logs - Base

        protected static bool IsMaster => U5kManService._Master;
        protected static string KeyLog(eIncidencias code) => code != eIncidencias.IGNORE ? "[" + ((Int32)code).ToString() + "] " : "";
        protected static object [] NormalizeParams(string msg, object[] parametros)
        {
            var paramList = parametros?.ToList() ?? new List<object>();
            paramList.Insert(0, msg);
            var msgInci = String.Join(",", paramList.Select(e => e.ToString().Replace(",", ";")).ToList());
            return new object[] { msgInci };
        }
        protected static void RecordEventFromLog(DateTime when, eIncidencias code, eTiposInci tp, string idw, object[] parametros)
        {
            if (code == eIncidencias.IGNORE) return;
            HistThread.hproc?.AddInci(DateTime.Now, 0, code, (int)tp, idw, parametros);
        }
        #endregion

        #region Log - Public
        public void LogConsole<T>(LogLevel level, String message)
        {
            if (level == LogLevel.Fatal)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (level == LogLevel.Error)
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (level == LogLevel.Warn)
                Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" [" + DateTime.Now + "][" + level + "] [" + typeof(T).Name.ToUpper() + "] " + message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void RecordEvent<T>(DateTime when, eIncidencias inci, eTiposInci thw, string idhw, object[] parametros,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(when, inci, thw, idhw, parametros);
            var msgInci = StrRegHistorico(when, inci, thw, idhw, parametros);
            var trace = inci == eIncidencias.ITO_PTT ||
                Array.FindIndex(parametros, e => e.ToString().ToLower().Contains("ptt") || e.ToString().ToLower().Contains("sqh")) >= 0;
            if (trace)
                Uv5kLog.Trace<T>(IsMaster, KeyLog(inci), msgInci, caller, lineNumber, file);
            else
                Uv5kLog.Debug<T>(IsMaster, KeyLog(inci), msgInci, caller, lineNumber, file);
        }
        static public void LogTrace<T>(String message,
            eIncidencias code = eIncidencias.IGNORE,
            Object[] issueMessages = null,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, issueMessages));
            Uv5kLog.Trace<T>(IsMaster, idhw ?? Path.GetFileNameWithoutExtension(file), message, caller, lineNumber, file);
        }
        static public void LogDebug<T>(String message,
            eIncidencias code = eIncidencias.IGNORE,
            Object[] issueMessages = null,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, 
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, issueMessages));
            Uv5kLog.Debug<T>(IsMaster, idhw ?? Path.GetFileNameWithoutExtension(file), message, caller, lineNumber, file);
        }
        static public void LogInfo<T>(String message,
            eIncidencias code = eIncidencias.IGNORE,
            Object[] issueMessages = null,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, issueMessages));
            Uv5kLog.Info<T>(IsMaster, idhw ?? Path.GetFileNameWithoutExtension(file), message, caller, lineNumber, file);
        }
        static public void LogWarn<T>(String message,
            eIncidencias code = eIncidencias.IGNORE,
            Object[] issueMessages = null,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, issueMessages));
            Uv5kLog.Warn<T>(IsMaster, idhw ?? Path.GetFileNameWithoutExtension(file), message, caller, lineNumber, file);
        }
        static public void LogError<T>(String message,
            eIncidencias code = eIncidencias.IGNORE,
            Object[] issueMessages = null,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, issueMessages));
            Uv5kLog.Error<T>(IsMaster, idhw ?? Path.GetFileNameWithoutExtension(file), message, caller, lineNumber, file);
        }
        static public void LogFatal<T>(String message,
            eIncidencias code = eIncidencias.IGNORE,
            Object[] issueMessages = null,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0, 
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, issueMessages));
            Uv5kLog.Fatal<T>(IsMaster, idhw ?? Path.GetFileNameWithoutExtension(file), message, caller, lineNumber, file);
        }
        static public void LogException<T>(String message, Exception ex,
            bool severity = false, bool bRegistroHistorico = false,
            string idhw = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            var code = bRegistroHistorico ? eIncidencias.IGRL_U5KI_SERVICE_ERROR : eIncidencias.IGNORE;
            RecordEventFromLog(DateTime.Now, code, eTiposInci.TEH_SISTEMA, "MTTO", NormalizeParams(message, null));
            Uv5kLog.Exception<T>(IsMaster, idhw??Path.GetFileNameWithoutExtension(file), ex, message, caller, lineNumber, file);
        }
        #endregion

        /** */
        protected static Object[] Params(params Object[] objs)
        {
            return objs;
        }
        /** */
        protected static string StrParams(Object[] objs)
        {
            StringBuilder str = new StringBuilder("[");
            if (objs != null)
            {
                foreach (Object obj in objs)
                {
                    str.Append(obj.ToString() + ",");
                }
            }
            str.Append("]");
            return str.ToString();
        }

        protected static string StrRegHistorico(DateTime when, eIncidencias cd, eTiposInci tp, string who,
            object[] p) => $"Registro Historico => {when}, [{tp}, {cd} from {who}], data => {p.ToList().Select(e => e.ToString()).Aggregate("", (c, n) => c + ", " + n)}";

        /// <summary>
        /// 
        /// </summary>
        protected class ManagedSemaphore
        {
            public static void Init()
            {
                owners.Clear();
                sems.Clear();
            }

            public static string Create(string id)
            {
                //if (sems.ContainsKey(id))
                //    Throw("ManagedSemaphore Repetido: " + id);
                sems[id] = new System.Threading.Semaphore(1, 1);
                return id;
            }

            static public void WaitOne(string id)
            {
                if (!sems.ContainsKey(id))
                    Throw("ManagedSemaphore no creado: " + id);
                if (sems[id].WaitOne(60000) == false)
                    Throw(String.Format("Semaforo Pillado: {0}", id));

                owners[id] = System.Threading.Thread.CurrentThread.ManagedThreadId;
            }

            static public void Release(string id)
            {
                if (!sems.ContainsKey(id))
                    Throw("ManagedSemaphore no creado: " + id);

                if (sems[id].WaitOne(0) == true)
                    Throw("ManagedSemaphore. Release fuera de lugar: " + id);

                sems[id].Release();
                owners[id] = -1;
            }

            static protected void Throw(string msg)
            {
                throw new Exception(msg);
            }

            static protected Dictionary<string, int> owners = new Dictionary<string, int>();
            static protected Dictionary<string, System.Threading.Semaphore> sems = new Dictionary<string, System.Threading.Semaphore>();
        }
        /// <summary>
        /// 
        /// </summary>
        public class ImAliveTick
        {
            public ImAliveTick(Int16 Interval)
            {
                interval = TimeSpan.FromSeconds(Interval);
                next = DateTime.Now + interval;
            }
            public void Tick(String who, Action cb)
            {
                TimeSpan elapsed = next - DateTime.Now;
                if (elapsed <= TimeSpan.FromSeconds(0))
                {
                    if (cb != null) cb();
                    next = DateTime.Now + interval;
                }
            }
            public void Message(string msg)
            {
#if DEBUG
                LogWarn<NGThread>(msg);
#else
                LogDebug<NGThread>(msg);
#endif
            }
            TimeSpan interval;
            DateTime next;
        }
        public ImAliveTick IamAlive = new ImAliveTick(60);

#if _FILTER_V1_
        /// <summary>
        /// 
        /// </summary>
        protected class BaseStoreFilter
        {
            class StoreFilteData
            {
                public DateTime timestamp { get; set; }
                public Int32 repeats { get; set; }
            }
            public BaseStoreFilter()
            {
                // PttAndSqhFilter = U5kManService.cfgSettings/* U5kManServer.Properties.u5kManServer.Default*/.Historico_PttSqhOnBdt;
                LogRepeatControlTime = U5kManServer.Properties.u5kManServer.Default.LogRepeatFilterSecs;
                LogRepeatSupervisionTime = U5kManServer.Properties.u5kManServer.Default.LogRepeatSupervisionMin;
            }
            public bool ToStore(string keyString)
            {
                lock (locker)
                {
                    /** Control de repetición */
                    bool bStore = true;
                    DateTime now = DateTime.Now;
                    Int32 repeats = 1;
                    Int32 key = keyString.GetHashCode();

                    if (_control.ContainsKey(key))
                    {
                        TimeSpan elapsed = now - _control[key].timestamp;
                        bStore = elapsed > TimeSpan.FromSeconds(LogRepeatControlTime);
                        repeats = bStore ? 1 : _control[key].repeats + 1;
                    }
                    /** Aviso para evento que se está repitiendo */
                    if (repeats == 20)
                    {
                    }

                    _control[key] = new StoreFilteData() { timestamp = now, repeats = repeats };
                    /** Limpiar los eventos que no se repiten... */
                    CleanOld(now);
                    return bStore;
                }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="now"></param>
            void CleanOld(DateTime now)
            {
                TimeSpan elapsed = now - lastClean;
                if (elapsed > TimeSpan.FromMinutes(LogRepeatSupervisionTime))
                {
                    var keysForClean = (from item in _control
                                        where now - item.Value.timestamp > TimeSpan.FromMinutes(5)
                                        select item.Key).ToList();

                    keysForClean.ForEach((key) => _control.Remove(key));
                    lastClean = now;
                }
            }

            int LogRepeatControlTime { get; set; }
            int LogRepeatSupervisionTime { get; set; }
            Dictionary<Int32, StoreFilteData> _control = new Dictionary<Int32, StoreFilteData>();
            DateTime lastClean = DateTime.Now;
            object locker = new object();
        }
#endif

        public static void ConfigCultureSet()
        {
            U5kGenericos.CurrentCultureSet((idioma) =>
            {
                LogTrace<BaseCode>("ConfigCultureSet => " + idioma);
            });
        }

        #region SafeExecute
        protected static T SafeExecute<T>(string who, Func<T> action, T defaultValue = default)
        {
            try
            {
                LogDebug<BaseCode>($"Execute Function for {who}");
                return action();
            }
            catch (Exception x)
            {
                LogException<BaseCode>($"On SecureExecute {who} exception ", x);
                LogDebug<BaseCode>($"{x}");
                return defaultValue;
            }
        }
        protected static void SafeExecute(string who, Action action)
        {
            try
            {
                LogDebug<BaseCode>($"Execute Action for {who}");
                action();
            }
            catch (Exception x)
            {
                LogException<BaseCode>($"On SecureExecute {who} exception ", x);
                LogDebug<BaseCode>($"{x}");
            }
        }
        #endregion SafeExecute
    }
}
