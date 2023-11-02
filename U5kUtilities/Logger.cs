using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using NLog;

namespace Utilities
{
    public class Uv5kLog
    {
        static string msgTemplate = "{file}:{line} ({master}, {idw}): {msg}";
        protected static Logger GetLogger<T>() => LogManager.GetLogger(typeof(T).Name);
        protected static void Log<T>(object key, object subkey, LogLevel level, string msg, string caller, int line, string file)
        {
            lock (Locker)
            {
                if (LogManager.Configuration != null)
                {
                    var filename = Path.GetFileName(file);
                    //var from = $"[{filename}:{caller ?? ""}:{line}]";
                    //var from = $"[{filename}:{line}]";
                    var logger = Uv5kLog.GetLogger<T>();
                    //var eventInfo = new LogEventInfo(level, typeof(T).Name, msg);
                    //eventInfo.Properties["where"] = from;
                    //eventInfo.Properties["master"] = key;
                    //eventInfo.Properties["inci"] = subkey;
                    //logger.Log(eventInfo);
                    logger.Log(level, msgTemplate, filename, line, key, subkey, msg);
                }
            }
        }
        public static void Trace<T>(object key, object subkey, String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            Log<T>(key, subkey, LogLevel.Trace, msg, caller, lineNumber, file);
        }
        public static void Debug<T>(object key, object subkey, String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            Log<T>(key, subkey, LogLevel.Debug, msg, caller, lineNumber, file);
        }
        public static void Info<T>(object key, object subkey, String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            Log<T>(key, subkey, LogLevel.Info, msg, caller, lineNumber, file);
        }
        public static void Warn<T>(object key, object subkey, String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            Log<T>(key, subkey, LogLevel.Warn, msg, caller, lineNumber, file);
        }
        public static void Error<T>(object key, object subkey, String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            Log<T>(key, subkey, LogLevel.Error, msg, caller, lineNumber, file);
        }
        public static void Fatal<T>(object key, object subkey, String msg,
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            Log<T>(key, subkey, LogLevel.Fatal, msg, caller, lineNumber, file);
        }
        public static void Exception<T>(object key, object subkey, Exception x, string msg1 = "",
            [System.Runtime.CompilerServices.CallerMemberName] string caller = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerFilePath] string file = null)
        {
            while (x != null)
            {
                var msg = msg1 + " [EXCEPTION]: " + x.Message; // + (x.InnerException != null ? (" [INNER EXCEPTION]: " + x.InnerException.Message) : "");
                Log<T>(key, subkey, LogLevel.Error, msg, caller, lineNumber, file);
                Log<T>(key, subkey, LogLevel.Trace, $"[EXCEPTION TRACE] => {x}", caller, lineNumber, file);
                x = x.InnerException;
            }
            //Uv5kLog.GetLogger<T>().Trace($"[EXCEPTION TRACE] => {x}");
        }
        static object Locker { get; set; } = new object();
    }
}
