using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utilities
{
    public class DebuggingHelper
    {
        public static bool Simulating => File.Exists("./simulating");
        public class SimulatedGw
        {
            static JObject GwData(string gw, string cpu)
            {
                var filename = $"./logs/GW-{gw}.json";
                if (File.Exists(filename))
                {
                    var strdata = File.ReadAllText(filename);
                    var alldata = JObject.Parse(strdata);
                    var aname = alldata["gwA"]["name"].ToString();
                    var bname = alldata["gwB"]["name"].ToString();
                    if (cpu == aname)
                    {
                        return alldata["gwA"].ToObject<JObject>();
                    }
                    else if (cpu == bname)
                    {
                        return alldata["gwB"].ToObject<JObject>();
                    }
                }
                return null; 
            }
            public static bool Ping2Cpu(string gw, string cpu)
            {
                var data = GwData(gw, cpu);
                if (data != null)
                {
                    var std = data["IpConn"].ToObject<bool>();
                    return std;
                }
                return false;
            }

            public static bool SipPing2Cpu(string gw, string cpu)
            {
                var data = GwData(gw, cpu);
                if (data != null)
                {
                    var std = data["SipMod"].ToObject<bool>();
                    return std;
                }
                return false;
            }

            public static bool CfgPing2Cpu(string gw, string cpu)
            {
                var data = GwData(gw, cpu);
                if (data != null)
                {
                    var std = data["CfgMod"].ToObject<bool>();
                    return std;
                }
                return false;
            }

            public static bool SnmpPing2Cpu(string gw, string cpu, Action<int, int, int, bool, int, string> response)
            {
                var data = GwData(gw, cpu);
                if (data != null)
                {
                    var mod = data["SnmpMod"].ToObject<bool>();
                    if (mod)
                    {
                        var std = data["std"].ToObject<int>();
                        var lan1 = data["lan1"].ToObject<int>();
                        var lan2 = data["lan2"].ToObject<int>();
                        var mss = data["Seleccionada"].ToObject<bool>();
                        var fa = data["stdFA"].ToObject<int>();
                        var ntpdata = data["ntp"]["LastInfoFromClient"].ToObject<string>();
                        response(std, lan1, lan2, mss, fa, ntpdata);
                    }
                    return mod;
                }
                return false;
            }
            public static void SnmpSlotGet(string gw, string cpu, int slot, Action<int,int> response)
            {
                var data = GwData(gw, cpu);
                if (data != null)
                {
                    var slots = data["slots"].ToArray<JToken>();
                    if (slots != null && slots.Length > slot)
                    {
                        var jslot = slots[slot];
                        var tipo = jslot["std_cfg"].ToObject<int>();
                        var status = jslot["std_online"].ToObject<int>();
                        response(tipo, status);
                    }
                }
            }
            public static void SnmpRecursoGet(string gw, string cpu, int slot, int rec, Action<int, int> response)
            {
                var data = GwData(gw, cpu);
                if (data != null)
                {
                    var slots = data["slots"].ToArray<JToken>();
                    if (slots != null && slots.Length > slot)
                    {
                        var jslot = slots[slot];
                        var recs = jslot["recs"].ToArray<JToken>();
                        if (recs !=null && recs.Length > rec)
                        {
                            var jrec = recs[rec];
                            var tipo = jrec["tipo_itf"].ToObject<int>();
                            var status = jrec["std_online"].ToObject<int>();

                           response(tipo, status);
                        }
                    }
                }

            }
        }
        public class SimulatedTop
        {
            JObject Data { get; set; }
            public SimulatedTop(string name)
            {
                LoadData(name);
            }
            protected void LoadData(string name)
            {
                var filename = $"./logs/Top-{name}.json";
                if (File.Exists(filename))
                {
                    var strdata = File.ReadAllText(filename);
                    Data = JObject.Parse(strdata);
                }
                else
                {
                    Data = null;
                }
            }
            public void SnmpPing(Action<bool, dynamic> response)
            {
                if (Data == null)
                {
                    response(false, null);
                }
                else
                {
                    var data = new
                    {
                        stdpos = Data["stdpos"].ToObject<int>(),
                        panel = Data["panel"].ToObject<int>(),
                        jack_exe = Data["jack_exe"].ToObject<int>(),
                        jack_ayu = Data["jack_ayu"].ToObject<int>(),
                        alt_r = Data["alt_r"].ToObject<int>(),
                        alt_t = Data["alt_t"].ToObject<int>(),
                        alt_hf = Data["alt_hf"].ToObject<int>(),
                        rec_w = Data["rec_w"].ToObject<int>(),
                        lan1 = Data["lan1"].ToObject<int>(),
                        lan2 = Data["lan2"].ToObject<int>(),
                        ntp = Data["ntp"].ToObject<string>()
                    };
                    response(true, data);
                }
            }
        }
        public class ThrowErrors
        {
            public enum LaunchableErrors { WebListenerError }
            public static void Program(LaunchableErrors error)
            {
                lock (Locker)
                {
                    ScheduledErrors?.Enqueue(error);
                }
            }
            public static void GetError(Action<LaunchableErrors> take)
            {
                lock (Locker)
                {
                    if (ScheduledErrors.Count > 0)
                    {
                        var error = ScheduledErrors.Dequeue();
                        take(error);
                    }
                }
            }
            static Queue<LaunchableErrors> ScheduledErrors { get; set; } = new Queue<LaunchableErrors>();
            static object Locker { get; set; } = new object();
        }
    }
}
