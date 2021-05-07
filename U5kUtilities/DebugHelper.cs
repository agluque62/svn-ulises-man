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

            public static bool SnmpPing2Cpu(string gw, string cpu, Action<int, int, int, bool, int> response)
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
                        response(std, lan1, lan2, mss, fa);
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
    }
}
