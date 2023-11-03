using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Utilities;
using NucleoGeneric;
using U5kManServer.Procesos;
using NAudio.SoundFont;

namespace U5kManServer
{
    /// <summary>
    /// 20200122: Se separa la Supervision del Equipo de la del recurso, para evitar hacer PINES repetidos en equipos con varios recursos.
    /// </summary>
    public class ExtEquSpv : NGThread/*, IDisposable*/
    {
        IProcessData pData;
        IProcessPing pPing;
        IProcessSip pSip;

        /// <summary>
        /// 
        /// </summary>
        public ExtEquSpv(IProcessData data=null, IProcessPing ping = null, IProcessSip sip = null)
        {
            Name = "ExtEquResSpv";
            
            pData = data ?? new RunTimeData();
            pPing = ping ?? new RuntimePingService();
            pSip = sip ?? new RuntimeSipService();

            /** 20180709. Peticion #3632 */
            AllowedSipResponses = new List<string>();
            if (Properties.u5kManServer.Default.AllowedResponsesToSipOptions != null)
            {
                foreach (var item in Properties.u5kManServer.Default.AllowedResponsesToSipOptions)
                    AllowedSipResponses.Add(item);
            }

            //local_ua = new SipUA() { user = "MTTO", ip = Properties.u5kManServer.Default.MiDireccionIP, port = 7060 };
            //sips = new SipSupervisor(local_ua, Properties.u5kManServer.Default.SipOptionsTimeout);
            //sips.NotifyException += (ua, x) =>
            //{
            //    LogException<ExtEquSpv>("SipSupervisor" + ua.uri, x);
            //};
        }
        /// <summary>
        /// 
        /// </summary>
        protected void LocalDispose()
        {
            pSip.Dispose();
            LogDebug<ExtEquSpv>("ExtEquSpv Dispose...");
        }
        protected int AdjustedInterval(int nequipos, Decimal timeoutDefault)
        {
            Decimal equiposTimeout = (Decimal)(nequipos * 100);
            return (int)(equiposTimeout > timeoutDefault ? equiposTimeout : timeoutDefault);
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void Run()
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name);

            Decimal interval = Properties.u5kManServer.Default.SpvInterval;     // Tiempo de Polling (se ajusta con el número de equipos 100 msg por equipo,
            Decimal threadTimeout = 2 * interval / 3;                           // Tiempo de proceso individual.
            Decimal poolTimeout = 3 * interval / 4;                             // Tiempo máximo del Pool de Procesos.

            using (timer = new TaskTimer(TimeSpan.FromMilliseconds((double)Decimal.ToInt32(interval)), this.Cancel))
            {
                while (IsRunning())
                {
                    if (pData.IsMaster == true)
                    {
                        List<EquipoEurocae> localequ = null;    // new List<EquipoEurocae>();
                        try
                        {
                            // Copia de equipo configurados.
                            GlobalServices.GetWriteAccess(() => localequ = pData.Data.STDEQS.Select(eq => new EquipoEurocae(eq)).ToList());
                            List<Task> tasks = new List<Task>();
                            var equipos = localequ?.GroupBy(eq => eq.Ip1)
                                .ToDictionary(grp => grp.Key, grp => grp.ToList());

                            if (equipos != null)
                            {
                                var skipped = 0;
                                LogInfo<ExtEquSpv>($"Supervisando equipos y recursos externos ({equipos.Count}) ...");

                                foreach (var equipo in equipos)
                                {
                                    if (equipo.Value[0].IsPollingTime() == true)
                                    {
                                        tasks.Add(BackgroundTaskFactory.StartNew(equipo.Key, () =>
                                        {
                                            try
                                            {
                                                SupervisaEquipo(equipo.Key, equipo.Value);
                                            }
                                            catch (Exception x)
                                            {
                                                LogException<ExtEquSpv>("", x);
                                            }
                                        },
                                        (id, excep) => { },
                                        TimeSpan.FromMilliseconds((double)threadTimeout)));
                                        LogTrace<ExtEquSpv>($"PING Executed: {equipo.Key}");
                                    }
                                    else
                                    {
                                        skipped++;
                                        LogTrace<ExtEquSpv>($"PING Skipped : {equipo.Key}");
                                    }
                                }
                                LogInfo<ExtEquSpv>($"Waiting for ({tasks.Count}). Skipped {skipped} ...");
                                // Ajuste del tiempo
                                timer.Interval = TimeSpan.FromMilliseconds(AdjustedInterval(equipos.Count, interval));
                                var waitingResult = Task.WaitAll(tasks.ToArray(), TimeSpan.FromMilliseconds((double)poolTimeout));
                                LogInfo<ExtEquSpv>($"Fin de Supervision de equipos y recursos externos ({tasks.Count}, {waitingResult})...");
                            }
                        }
                        catch (Exception x)
                        {
                            if (x is ThreadAbortException)
                            {
                                Thread.ResetAbort();
                                break;
                            }
                            LogException<ExtEquSpv>("SupervisaEquiposExternos", x);
                        }
                        // Actualizo los datos..
                        if (localequ != null)
                        {
                            GlobalServices.GetWriteAccess(() =>
                            {
                                var toActualize = localequ
                                    .Where(e => pData.Data.EQUDIC.Keys.Contains(e.Key))  // Elimino los eliminados en una posible sectorizacion.
                                    .Where(e => pData.Data.EQUDIC[e.Key].Equals(e))      // Elimino los modificados en una posible sectorizacion.                                
                                    .Select(e => e).ToList();
                                // Copio el estado de los 'No afectados'
                                toActualize.ForEach(e => pData.Data.EQUDIC[e.Key].CopyFrom(e));
                                // Se calcula el estado final del subsistema.
                                SetEstadoGlobalEquipos(pData.Data, localequ);
                            });
                        }
                        tm.StopAndPrint((msg) => LogInfo<ExtEquSpv>(msg));
                    }
#if DEBUG1
                    Task.Delay(TimeSpan.FromMilliseconds((int)interval)).Wait();
#else
                    GoToSleepInTimer();
#endif
                }
            }
            LocalDispose();
            Dispose();
            LogInfo<ExtEquSpv>("Finalizado...");
        }
        /// <summary>
        /// 
        /// </summary>
        void SetEstadoGlobalEquipos(U5kManStdData gdata, List<EquipoEurocae> stdeqeu)
        {
            int equipos = stdeqeu.Count;
            int equipos_presentes = stdeqeu.Where(e => e.EstadoRed1 == std.Ok).ToList().Count;
            int equipos_error = stdeqeu.Where(e => e.EstadoRed1 == std.Ok && e.EstadoSip == std.Error).ToList().Count;
            int equipos_aviso = stdeqeu.Where(e => e.EstadoRed1 == std.Ok && e.EstadoSip == std.Aviso).ToList().Count;

            U5KStdGeneral gen = gdata.STDG;
            gen.stdGlobalExt =
                equipos_presentes == 0 ? std.NoInfo :
                equipos_error != 0 ? std.Error :
                equipos_aviso != 0 ? std.Aviso :
                equipos_presentes == equipos ? std.Ok : std.Aviso;
        }

        protected void SupervisaEquipo(string ip, List<EquipoEurocae> recursos)
        {
            LogTrace<EquipoEurocae>($"Supervisando Equipo en {ip}, {recursos.Count}");
            List<Task> stasks = new List<Task>();
            var presente = recursos[0].EstadoRed1 == std.Ok;
            var res = pPing.Ping(ip, presente).Result;
            
            foreach (var recurso in recursos)
            {
                if (recurso.ProcessResult(res))
                {
                    recurso.EstadoRed1 = recurso.EstadoRed2 = ChangeStd(recurso, res ? std.Ok : std.NoInfo); /** Provocará el histórico */
                    LogTrace<EquipoEurocae>($"Recurso {recurso.Id}, Estado Red => {recurso.EstadoRed1}");

                    if (recurso.EstadoRed1 == std.Ok)
                    {
                        /** Estado Agente SIP */
                        if (recurso.Tipo == 5)
                        {
                            /** Los Grabadores no tienen Agente SIP, Para que se muestre Ok, 
                                Ponemos que está bien */
                            recurso.EstadoSip = std.Ok;
                            LogTrace<EquipoEurocae>($"Recurso Grabacion {recurso.Id} => {recurso.EstadoSip}");
                        }
                        else
                        {
                            stasks.Add(Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    SupervisaRecurso(recurso);
                                }
                                catch (Exception x)
                                {
                                    LogException<ExtEquSpv>("", x);
                                }
                            }));
                        }
                    }
                    else
                    {
                        // RM#7285. Si no hay RED, se resetea el estado sip del recurso.
                        recurso.EstadoSip = std.Error;
                        recurso.LastOptionsResponse = "";
                    }
                    LogTrace<ExtEquSpv>($"Process {(res ? "Ok  " : "Fail")} executed: {recurso.sip_user}.");
                }
                else
                {
                    LogWarn<ExtEquSpv>($"Process Fail ignored : {recurso.sip_user}.");
                }
            }

            var waitingResult = Task.WaitAll(stasks.ToArray(), 9000);
            LogTrace<EquipoEurocae>($"Equipo en {ip}, Supervisado ({stasks.Count}, {waitingResult})");
        }

        protected void SupervisaRecurso(EquipoEurocae recurso)
        {
            LogTrace<EquipoEurocae>($"Supervisando recurso {recurso.sip_user}");
            var res = pSip.Ping(recurso.sip_user, recurso.Ip1, recurso.sip_port, recurso.Tipo == 2).Result;
            if (res.Item1 == true) // Presente
            {
                if (res.Item2 == null || res.Item2 == "Error")
                {
                    recurso.EstadoSip = std.Error;
                    recurso.LastOptionsResponse = "";
                    LogTrace<EquipoEurocae>($"{recurso.sip_user}. SipAgent Respuesta NULA.");
                }
                else
                {
                    var allowedReponse = AllowedSipResponses.Contains(res.Item2);
                    recurso.EstadoSip = allowedReponse ? std.Ok : std.Aviso;
                    recurso.LastOptionsResponse = res.Item2;
                    LogTrace<EquipoEurocae>($"{recurso.sip_user}. SipAgent response {recurso.LastOptionsResponse}, EstadoSip => {recurso.EstadoSip}");
                }
            }
            else
            {
                recurso.EstadoSip = std.Error;
                recurso.LastOptionsResponse = "";
                LogTrace<EquipoEurocae>($"{recurso.sip_user}. SipAgent no contesta...");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actual"></param>
        /// <param name="nuevo"></param>
        /// <returns></returns>
        private std ChangeStd(EquipoEurocae equipo, std nuevo)
        {
            if (equipo.EstadoRed1 != nuevo)
            {
                /** Generar evento */
                U5kBaseDatos.eTiposInci tinci = equipo.Tipo == 2 ? U5kBaseDatos.eTiposInci.TEH_EXTERNO_RADIO :
                    equipo.Tipo == 3 ? U5kBaseDatos.eTiposInci.TEH_EXTERNO_TELEFONIA :
                    equipo.Tipo == 5 ? U5kBaseDatos.eTiposInci.TEH_RECORDER : U5kBaseDatos.eTiposInci.TEH_SISTEMA;
                string id = equipo.Tipo == 5 ? equipo.Id : equipo.sip_user;
                RecordEvent<ExtEquSpv>(DateTime.Now,
                    nuevo == std.Ok ? U5kBaseDatos.eIncidencias.IEE_ENTRADA :
                    U5kBaseDatos.eIncidencias.IEE_CAIDA,
                    tinci,
                    id, Params());
            }
            return nuevo;
        }

        //private SipUA local_ua = null;
        //private SipSupervisor sips = null;
        private List<string> AllowedSipResponses = null;
    }
}
