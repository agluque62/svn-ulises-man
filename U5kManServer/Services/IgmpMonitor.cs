using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

using Utilities;
using NucleoGeneric;
using U5kBaseDatos;

namespace U5kManServer.Services
{
    public class IgmpMonitor : BaseCode
    {
        static public void Start(string ip, TimeSpan timeout, Func<bool> masterInfo)
        {
            LogInfo<IgmpMonitor>($"Starting IgmpMonitor.");
            if (Manager == null)
            {
                MasterInfo = masterInfo;
                Timeout = timeout;
                var ipa = IPHelper.SafeParse(ip);
                if (ipa != null)
                {
                    RawSocket = new RawSocket(ipa, System.Net.Sockets.ProtocolType.Igmp);
                    Manager = new IgmpManager();
                    Manager.IgmpMasterPresence += new EventHandler<IgmpMasterPresenceArgs>((o, e) =>
                    {
                        switch (e.Code)
                        {
                            case IgmpMasterPresenceEventCodes.impeError:
                                LogException<IgmpMonitor>("", e.Error);
                                break;
                            case IgmpMasterPresenceEventCodes.impeNotify:
                                if (MasterInfo())
                                {
                                    var inci = e.Active == false ? eIncidencias.IGRL_NBXMNG_ALARM : eIncidencias.IGRL_NBXMNG_EVENT;
                                    var msg = e.Active == true ? $"Detectado IGMP Querier en {e.Where}" : $"Perdido Contacto con IGMP Querier en {e.Where}";
                                    RecordEvent<IgmpMonitor>(DateTime.Now, inci, eTiposInci.TEH_SISTEMA, eTiposHw.MTTO.ToString(),
                                        new object[] { msg, "", "", "", "", "", "", "" });
                                }
                                else
                                {
                                    LogDebug<IgmpMonitor>($"IGMP Querier on {e.Where} => {e.Active}.");
                                }
                                break;
                        }
                    });
                    try
                    {
                        Manager.Start(RawSocket, Timeout);
                        Task.Run(() =>
                        {
                            Task.Delay(Timeout).Wait();
                            if (Manager?.Queriers.Count() == 0)
                            {
                                /** No hay Querier en el sistema */
                                if (MasterInfo())
                                    RecordEvent<IgmpMonitor>(DateTime.Now, eIncidencias.IGRL_NBXMNG_ALARM, eTiposInci.TEH_SISTEMA, eTiposHw.MTTO.ToString(),
                                        new object[] { "No se detecta IGMP Querier en el sistema.", "", "", "", "", "", "", "" });
                            }
                        });
                        LogInfo<IgmpMonitor>($"IgmpMonitor started.");
                    }
                    catch (Exception x)
                    {
                        LogException<IgmpMonitor>("", x);
                    }
                }
                else
                {
                    LogError<IgmpMonitor>($"Unable to parse IP {ip}.");
                }
            }
            else
            {
                LogError<IgmpMonitor>($"IgmpMonitor already started...");
            }
        }
        static public void Stop()
        {
            LogInfo<IgmpMonitor>($"Ending IgmpMonitor.");
            Manager?.Dispose();
            RawSocket?.Dispose();
            Manager = null;
            RawSocket = null;
            LogInfo<IgmpMonitor>($"IgmpMonitor Ended.");
        }
        static public string Status => Manager == null ? "" : Manager.ToString();
        static IRawSocket RawSocket { get; set; }
        static IIgmpManager Manager { get; set; }
        static Func<bool> MasterInfo { get; set; }
        static TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    }
}
