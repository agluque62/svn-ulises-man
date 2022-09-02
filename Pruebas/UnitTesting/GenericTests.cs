using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

using System.Threading;
using System.Threading.Tasks;

using Utilities;
using U5kBaseDatos;
using U5kManServer;
using U5kManServer.WebAppServer;

namespace UnitTesting
{
    [TestClass]
    public class GenericTests
    {
        [TestMethod]
        public void HttpClientTests()
        {
            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Test START");

            HttpHelper.GetSync("http://192.168.1.121/pepe", TimeSpan.FromSeconds(5), (succes, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: GetSync. Res {succes}, data: {data}");
            });

            HttpHelper.GetSync("http://192.168.0.212:1234/pepe", TimeSpan.FromSeconds(5), (succes, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: GetSync. Res {succes}, data: {data}");
            });

            HttpHelper.GetSync("http://192.168.0.212/pepe", TimeSpan.FromSeconds(5), (succes, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: GetSync. Res {succes}, data: {data}");
            });

            HttpHelper.GetSync("http://192.168.0.50:8080/test", TimeSpan.FromSeconds(5), (succes, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: GetSync. Res {succes}, data: {data}");
            });

            HttpHelper.GetSync("http://192.168.0.223:8080/test", TimeSpan.FromSeconds(5), (succes, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: GetSync. Res {succes}, data: {data}");
            });

            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Test END");
        }

        [TestMethod]
        public void HttpPostTests()
        {
            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Test START");

            HttpHelper.PostSync(HttpHelper.URL("10.12.60.130","1023","/rd11"), new { id = "test " }, TimeSpan.FromSeconds(5), (success, data) =>
            {            
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: PostSync. Res {success}, data: {data}");
            });

            HttpHelper.PostSync(HttpHelper.URL("10.12.60.130", "1023", "/rdhf"), new { id = "test " }, TimeSpan.FromSeconds(5), (success, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: PostSync. Res {success}, data: {data}");
            });

            HttpHelper.PostSync(HttpHelper.URL("10.12.60.130", "1023", "/rdhfhf"), new { id = "test " }, TimeSpan.FromSeconds(5), (success, data) =>
            {
                Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: PostSync. Res {success}, data: {data}");
            });

            Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: Test END");
        }
        [TestMethod]
        public void TestExceptionFlow()
        {
            try
            {
                var tmp = new TestingClass(() =>
                {
                    InnerFunction(() =>
                    {
                        Debug.WriteLine("Throwing primary exception...");
                        throw new Exception("Primary Exception.");
                    });
                });
            }
            catch
            {
                StackTrace stack = new StackTrace(true);
                Debug.WriteLine("Exception Catched: " + stack.ToString());
            }
            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
        }
        [TestMethod]
        public void LocatingClusterConfigTest()
        {
            var Is64 = Environment.Is64BitOperatingSystem;
            var ProgramFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var CompanyFolder = "DF Nucleo";
            var ProductFolder = "UlisesV5000Cluster";
            var ConfigFile = "ClusterSrv.exe.Config";
            var pathToConfig = $"{ProgramFolder}\\{CompanyFolder}\\{ProductFolder}\\{ConfigFile}";
            if (File.Exists(pathToConfig))
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap() { ExeConfigFilename = pathToConfig };
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
                ConfigurationSection configSection = config.GetSection("userSettings/ClusterSrv.Properties.Settings");
                string result = configSection.SectionInformation.GetRawXml();
                Console.WriteLine(result);

            }
            else
            {

            }
        }
        [TestMethod]
        public void ControlledDelayTest()
        {
            ManualResetEvent control = new ManualResetEvent(false);

            Action<ManualResetEvent, string> Delay = (ctrl, msg) =>
            {
                Task.Run(() =>
                {
                    if (ctrl.WaitOne(500))
                    {
                        Debug.WriteLine($"OK => {msg}");
                    }
                    else
                    {
                        Debug.WriteLine($"ERROR => {msg}");
                    }
                });
            };

            Delay(control, "Mensaje 1");
            Delay(control, "Mensaje 2");
            Task.Delay(300).Wait();
            Debug.WriteLine($"Fin Bloque 1");
            control.Set();

            Task.Delay(100).Wait();
            control.Reset();
            
            Delay(control, "Mensaje 3");
            Delay(control, "Mensaje 4");
            Task.Delay(700).Wait();
            Debug.WriteLine($"Fin Bloque 2");
            control.Set();

            Task.Delay(200).Wait();
        }
        [TestMethod]
        public void TestDateToGo()
        {
            var alt1 = new TimeSpan(11, 59, 0);
            var alt2 = new TimeSpan(23, 59, 0);
            var dref = DateTime.Now - TimeSpan.FromHours(1);

            var ttg1 = alt1 - dref.TimeOfDay;
            var ttg2 = alt2 - dref.TimeOfDay;

            var dalt = dref + (ttg1 > TimeSpan.FromSeconds(0) ? ttg1 : ttg2);

        }
        void InnerFunction(Action execute)
        {
            try
            {
                execute();
            }
            catch (Exception x)
            {
                Debug.WriteLine("Primary Exception cached: ", x.Message);
                throw x;
            }
            finally
            {
                Debug.WriteLine("Cierre de Bucle de Gestion de Excepcion.");
            }
        }
        class TestingClass
        {
            public TestingClass(Action action)
            {
                //Task.Run(() =>
                //{
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    action();
                //});
            }
        }
        [TestMethod]
        public void TestDbTableSistema()
        {
            using (var db = new U5kBdtService(Thread.CurrentThread.CurrentUICulture, eBdt.bdtMySql, "127.0.0.1", "root", "cd40"))
            {
                db.GetSystemParams("departamento", (group, port) =>
                {
                    Debug.WriteLine($"Grupo: {group}, Puerto: {port}");
                });
            }
        }
        [TestMethod]
        public void TestRoundTest()
        {
            var d1 = DateRoundUp(DateTime.Parse("2011-08-11 16:59") + TimeSpan.FromMinutes(1), TimeSpan.FromHours(12)) - TimeSpan.FromMinutes(1);
            Debug.WriteLine(d1.ToString());
            var d2 = DateRoundUp(DateTime.Parse("2011-08-11 9:59") + TimeSpan.FromMinutes(1), TimeSpan.FromHours(12)) - TimeSpan.FromMinutes(1);
            Debug.WriteLine(d2.ToString());
            var d3 = DateRoundUp(DateTime.Parse("2011-08-11 23:59:01") + TimeSpan.FromMinutes(1), TimeSpan.FromHours(12)) - TimeSpan.FromMinutes(1);
            Debug.WriteLine(d3.ToString());

            var d4 = DateTime.Now + TimeSpan.FromMinutes(1);
            Debug.WriteLine(d4.RoundUp(TimeSpan.FromHours(12)) - TimeSpan.FromMinutes(1));
        }
        DateTime DateRoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }
        [TestMethod]
        public void TestMainbergData1()
        {
            string line = "*DF1501  172.24.90.12     6 u  234  256  377    0.977  -82.219  26.097";
            char Class = line.ElementAt(0);
            var linedata = GeneralHelper.NormalizeWhiteSpace( line.Substring(1)).Split(' ');
            var info = new NtpMeinbergClientInfo.NtpServerInfo(line);
        }
        [TestMethod]
        public void TestMeinbergData2()
        {
            List<string> OkResp = new List<string>()
            {
                "     remote           refid      st t when poll reach   delay   offset  jitter",
                "==============================================================================",
                "*sarah.vandalswe 213.136.0.252    2 u   18   64  377   31.888  -45.950  13.130",
                "+ntp9.kashra-ser 90.187.148.77    2 u   53   64  377   36.299  -30.225   8.936",
                " 82.223.128.121  .STEP.          16 u   60   64    0    0.000    0.000   0.000",
                "+ntp4.kashra-ser 90.187.148.77    2 u    3   64  377   33.912  -30.896   8.626",
                "+sys.gegeweb.eu  145.238.203.14   2 u   25   64  377   29.584  -36.319  11.234",
                "xNucleoDF-VM01   LOCAL(0)         6 u   11   16  377    0.290   78.940   1.489"
            };
            List<string> EmptyResp = new List<string>() { "" };
            List<string> NoServersResp = new List<string>() { "No association ID's returned" };

            var info4Ok = new NtpMeinbergClientInfo(OkResp);
            var info4Empty = new NtpMeinbergClientInfo(EmptyResp);
            var info4NoServers = new NtpMeinbergClientInfo(NoServersResp);
            var infor4Live = new NtpMeinbergClientInfo(null, (sender, ev)=>
            {
            });

            var len = OkResp.ElementAt(0).Length;
            var GwResp = string.Join("", OkResp);
            var info4Gw = new NtpMeinbergClientInfo(GwResp, len);

        }
        [TestMethod]
        public void TestMeinbergData3()
        {
            string data4Gw = "{\"lines\":[\"     remote           refid      st t when poll reach   delay   offset  jitter==============================================================================x192.168.0.228   LOCAL(0)        13 u    2   16  377    0.472  +103.47  18.630x192.168.0.212   LOCAL(0)         6 u    7   16  377    0.481  -26.491   9.921\"]}";

            var pdata = (U5kManWebAppData.JDeserialize<stdGw.RemoteNtpClientStatus>(data4Gw)).lines;
            List<string> Value = NormalizeNtpStatusList(pdata);

        }
        [TestMethod]
        public void TestMeinbergData4()
        {
            var ntpInfo = new NtpInfoClass();
            var loop = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            loop.ToList().ForEach(l =>
            {
                Debug.WriteLine($"Checking {l}");
                //ntpInfo.Actualize("Testing", (connected, ip) =>
                //{
                //    Debug.WriteLine($"ChangeDetected {connected}, {ip}");
                //}, 0);
                Task.Delay(TimeSpan.FromSeconds(10)).Wait();
            });
        }
        protected List<string> NormalizeNtpStatusList(List<string> input)
        {
            List<string> output = new List<string>();
            int lenline = 78;

            if (input.Count == 1 && input[0].Length > lenline)
            {
                output = Enumerable.Range(0, input[0].Length / lenline).Select(i => input[0].Substring(i * lenline, lenline)).ToList();
            }
            else
                output = input;

            return output;
        }
    }
}
