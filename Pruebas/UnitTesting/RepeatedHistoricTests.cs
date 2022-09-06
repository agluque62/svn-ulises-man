using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

using U5kManServer;
using U5kBaseDatos;

namespace UnitTesting
{
    [TestClass]
    public class RepeatedHistoricTests
    {
        [TestMethod]
        public void RepeatedHistoricFilterTests()
        {
            var filter = new HistThread.StoreFilterControl();
            var inci1 = new U5kIncidencia()
            {
                id = 3052,
                idhw = "OnTest1",
                scv = 0,
                sistema = "OnTest",
                tipo = 0,
                desc = "Incidencia para test. Se esta repitiendo continuamente...",
                reconocida = DateTime.MinValue,
                fecha = DateTime.MinValue
            };
            var inci2 = new U5kIncidencia()
            {
                id = 3052,
                idhw = "OnTest2",
                scv = 0,
                sistema = "OnTest",
                tipo = 0,
                desc = "Incidencia para test. Se esta repitiendo continuamente...",
                reconocida = DateTime.MinValue,
                fecha = DateTime.MinValue
            };
            var protectionTime = 60;
            DateTime startTest = DateTime.Now;
            Debug.WriteLine($"{DateTime.Now} => Start Test.");
            do
            {
                inci1.fecha = DateTime.Now;
                if (filter.ToStore(inci1, protectionTime) == true)
                {
                    Debug.WriteLine($"{DateTime.Now} => Incidencia 1 Almacenada.");
                }
                Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                if (filter.ToStore(inci2, protectionTime) == true)
                {
                    Debug.WriteLine($"{DateTime.Now} => Incidencia 2 Almacenada.");
                }
                Task.Delay(TimeSpan.FromMilliseconds(1000)).Wait();
            } while ((DateTime.Now - startTest) < TimeSpan.FromMinutes(10));
            Debug.WriteLine($"{DateTime.Now} => End Test.");
        }
        [TestMethod]
        public void TracePlay()
        {
            var filter = new HistThread.StoreFilterControl();
            Debug.WriteLine($"{DateTime.Now} => Start Test.");
            ReadFile("dbg_stx03.txt", (items) =>
            {
                items.ForEach((item) =>
                {
                    Task.Delay(item.Delay).Wait();
                    if (filter.ToStore(item.Incidencia, item.ProtectionTime) == true)
                    {
                        Debug.WriteLine($"{DateTime.Now} => Incidencia {item} Almacenada.");
                    }
                });

            });
            Debug.WriteLine($"{DateTime.Now} => End Test.");
        }

        class TraceItem
        {
            public int Id { get; set; }
            public int Tipo { get; set; }
            public string Idhw { get; set; }
            public string Desc { get; set; }
            public TimeSpan Delay { get; set; }
            public int ProtectionTime { get; set; }

            public TraceItem(TimeSpan wait, int ptime, string item)
            {
                var items = item.Split('|').ToList();
                items.ForEach((i) =>
                {
                    var parts = i.Split('=');
                    if (parts.Count() == 2)
                    {
                        if (parts[0] == "id") Id = int.Parse(parts[1]);
                        else if (parts[0] == "tipo") Tipo = int.Parse(parts[1]);
                        else if (parts[0] == "idhw") Idhw = parts[1];
                        else if (parts[0] == "desc") Desc = parts[1];
                    }
                    Delay = wait;
                    ProtectionTime = ptime;
                });
            }

            public U5kIncidencia Incidencia => new U5kIncidencia() { fecha = DateTime.MinValue, id = Id, idhw = Idhw, tipo = Tipo, desc = Desc, reconocida = DateTime.MinValue, scv = 0, sistema = "OnTestSistema" };
            public override string ToString() => $"[{Id},{Idhw},{Desc}]";
        }
        void ReadFile(string filename, Action<List<TraceItem>> report)
        {
            var incis = new List<TraceItem>();
            var Last = DateTime.MinValue;

            if (File.Exists(filename))
            {
                var lines = File.ReadLines(filename).ToList();
                lines.ForEach((l) =>
                {
                    var il = l.Split(new string[] { "##" }, StringSplitOptions.None);
                    if (il.Count() == 4)
                    {
                        var when = DateTime.Parse(il[0]);
                        var ii = il[2].Split(new string[] { "=>" }, StringSplitOptions.None);
                        if (ii.Count() == 4)
                        {
                            var delay = Last == DateTime.MinValue ? TimeSpan.Zero : when - Last;

                            var ti = new TraceItem(delay, GetTrep(ii[2]), ii[1]);
                            incis.Add(ti);
                        }
                        Last = when;
                    }
                });
                var timeouts = incis.Where(i => i.Delay > TimeSpan.FromSeconds(i.ProtectionTime)).Count();
                report(incis);
            }
        }
        int GetTrep(string strtrep)
        {
            // 'Trep = (0)'
            var p = new Regex("(Trep = \\()([0-9]+)(\\))");
            var m = p.Match(strtrep);
            if (m.Success && m.Groups.Count == 4)
            {
                return int.Parse(m.Groups[2].Value);
            }
            return 0;
        }
    }
}
