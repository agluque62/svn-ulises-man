﻿using System;
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
    }
}
