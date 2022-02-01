using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Utilities;
using NucleoGeneric;

namespace U5kManServer.WebAppServer
{
    public enum cmdSync
    {
        Incidencias,
        Opciones,
        InfoLanes,
        InfoNtpClient,
        OpcionesSnmp,
        OpcionesSacta,
        VersionsDetails,
        Testing
    }
    public class MainStandbySyncServer : BaseCode, IDisposable
    {
        public int SyncListenerSpvPeriod { get; set; } = 5;
        public MainStandbySyncServer()
        {
        }
        public void Start(String srcIp, String grpIp, int port)
        {
            LogDebug<MainStandbySyncServer>($"Starting {srcIp}, {grpIp}:{port}");
            ThreadSync = new EventQueue();
            ThreadSync?.Start();
            Ip_propia = IPAddress.Parse(srcIp);

            ExecutiveThreadCancel = new CancellationTokenSource();
            ExecutiveThread = Task.Run(() =>
            {
                DateTime lastListenerTime = DateTime.MinValue;
                DateTime lastRefreshTime = DateTime.MinValue;
                // Supervisar la cancelacion.
                while (ExecutiveThreadCancel.IsCancellationRequested == false)
                {
                    Task.Delay(TimeSpan.FromMilliseconds(100)).Wait();
                    if (DateTime.Now - lastListenerTime >= TimeSpan.FromSeconds(SyncListenerSpvPeriod))
                    {
                        ThreadSync?.Enqueue("", () =>
                        {
                            if (Listener == null)
                            {
                                try
                                {
                                    Ip_grp = new IPEndPoint(IPAddress.Parse(grpIp), port);
                                    LogDebug<MainStandbySyncServer>($"{Id} Starting Listening on {Ip_grp}");

                                    Listener = new UdpSocket(srcIp, port/* Port*/);
                                    Listener.MaxReceiveThreads = 1;
                                    Listener.NewDataEvent += OnNewData;
                                    Listener.Base.JoinMulticastGroup(IPAddress.Parse(grpIp), Ip_propia);
                                    Listener.Base.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 16);
                                    Listener.BeginReceive();

                                    LogDebug<MainStandbySyncServer>($"{Id} Listening {Ip_grp}");
                                }
                                catch (Exception x)
                                {
                                    LogException<MainStandbySyncServer>($"{Id},{Ip_grp}", x);
                                    ResetLan();
                                }
                            }
                        });
                        // Supervisar la disponibilidad del Listener.
                        lastListenerTime = DateTime.Now;
                    }
                }
            });
            LogDebug<MainStandbySyncServer>($"{Id} Started...");
        }
        public void Stop()
        {
            LogDebug<MainStandbySyncServer>($"{Id} Ending SyncManager");

            ExecutiveThreadCancel?.Cancel();
            ExecutiveThread?.Wait(TimeSpan.FromSeconds(5));
            Listener?.Dispose();
            Listener = null;
            
            ThreadSync?.Stop();
            ThreadSync = null;

            LogDebug<MainStandbySyncServer>($"{Id} SyncManager Ended");
        }
        public void Dispose()
        {
        }
        public void Sync(cmdSync cmd, String data)
        {
            ThreadSync?.Enqueue("", () =>
            {
                LogDebug<MainStandbySyncServer>($"{Id} Sending data => {Ip_grp}, cmd: {cmd}, data: {GeneralHelper.ToShow(data, 100)}");
                byte[] bData = (new byte[] { (byte)cmd }).Concat(Encoding.ASCII.GetBytes(data)).ToArray();
                SendData(bData);
                /** Para que salga la trama */
                Task.Delay(TimeSpan.FromMilliseconds(50)).Wait();
            });
        }
        public void QueryVersionData()
        {
            versync.Reset();
            Sync(cmdSync.VersionsDetails, "");
            versync.WaitOne(2500);
        }

        private void SendData(byte[] data)
        {
            try
            {
                Listener?.Send(Ip_grp, data);
            }
            catch(Exception x)
            {
                LogException<MainStandbySyncServer>($"{Id} Exception", x);
                ResetLan();
            }
        }
        private void OnNewData(object sender, DataGram dg)
        {
            cmdSync cmd = (cmdSync)dg.Data[0];
            string strData = System.Text.Encoding.Default.GetString(dg.Data, 1, dg.Data.Count() - 1);
            
            LogDebug<MainStandbySyncServer>($"{Id} Recibido Mensaje {cmd}, data => {GeneralHelper.ToShow(strData, 100)}");

            ThreadSync?.Enqueue("", () =>
            {
                try
                {
                    if (Ip_propia.ToString() != dg.Client.Address.ToString())
                    {
                        switch (cmd)
                        {
                            case cmdSync.Incidencias:
                                LogDebug<MainStandbySyncServer>($"{Id} Procesando Comando {cmd}, data => {GeneralHelper.ToShow(strData, 100)}");
                                U5kManWADDbInci incis = U5kManWebAppData.JDeserialize<U5kManWADDbInci>(strData);
                                incis.Save();
                                break;
                            case cmdSync.Opciones:
                                LogDebug<MainStandbySyncServer>($"{Id} Procesando Comando {cmd}, data => {GeneralHelper.ToShow(strData, 100)}");
                                U5kManWADOptions opts = U5kManWebAppData.JDeserialize<U5kManWADOptions>(strData);
                                opts.Save();
                                break;
                            case cmdSync.OpcionesSnmp:
                                LogDebug<MainStandbySyncServer>($"{Id} Procesando Comando {cmd}, data => {strData}");
                                U5kManWADSnmpOptions snmpopts = U5kManWebAppData.JDeserialize<U5kManWADSnmpOptions>(GeneralHelper.ToShow(strData, 100));
                                snmpopts.Save();
                                break;
                            case cmdSync.OpcionesSacta:
                                LogDebug<MainStandbySyncServer>($"Processing Remote Sacta Config Received => {GeneralHelper.ToShow(strData, 100)}, Sacta Proxy=>{U5kManService.cfgSettings.HaySactaProxy}");
                                ServicioInterfazSacta sacta_srv = new ServicioInterfazSacta(U5kManServer.Properties.u5kManServer.Default.MiDireccionIP);
                                if (U5kManService.cfgSettings.HaySactaProxy)
                                {
                                    var local = sacta_srv.SactaConfGet();
                                    SactaConfig.RemoteConfigPatch(local, strData, (error, newData) =>
                                    {
                                        if (!error)
                                        {
                                            sacta_srv.SactaConfSet(newData);
                                        }
                                        else
                                        {
                                            LogError<MainStandbySyncServer>($"On MainStandbySyncServer Error patching sacta config => {newData}");
                                        }
                                    });
                                }
                                else
                                {
                                    sacta_srv.SactaConfSet(strData);
                                }
                                break;

                            case cmdSync.InfoLanes:
                                LogDebug<MainStandbySyncServer>($"{Id} Procesando Comando {cmd}, data => {GeneralHelper.ToShow(strData, 100)}");
                                GlobalServices.GetWriteAccess((data) =>
                                {
                                    StdServ srv = data.STDG.RemoteServer;
                                    if (srv != null)
                                    {
                                        srv.string2lanes = strData;
                                    }
                                });
                                break;

                            case cmdSync.InfoNtpClient:
                                LogDebug<MainStandbySyncServer>($"{Id} Procesando Comando {cmd}, data => {GeneralHelper.ToShow(strData, 100)}");
                                GlobalServices.GetWriteAccess((data) =>
                                {
                                    StdServ srv = data.STDG.RemoteServer;
                                    if (srv != null)
                                    {
                                        srv.NtpInfo.Actualize(strData);
                                    }
                                });
                                break;
                            case cmdSync.VersionsDetails:
                                LogDebug<MainStandbySyncServer>($"{Id} Procesando Comando {cmd}, data => {GeneralHelper.ToShow(strData, 100)}");
                                GlobalServices.GetWriteAccess((data) =>
                                {
                                    if (strData == ""/*U5kManService._Master*/)
                                    {
                                        // Es una petición. Se debe generar los datos para el MASTER.
                                        Sync(cmdSync.VersionsDetails, VersionDetails.SwVersions.ToString());
                                        LogDebug<MainStandbySyncServer>($"{Id} Informacion de Versiones Enviada.");
                                    }
                                    else 
                                    {
                                        // Es una Respuesta.
                                        if (data != null)
                                        {
                                            StdServ srv = data.STDG.RemoteServer;
                                            if (srv != null)
                                            {
                                                srv.jversion = strData;
                                            }
                                        }
                                        SignalVersionDataReceived();
                                        LogDebug<MainStandbySyncServer>($"{Id} Informacion de Versiones Recibida.");
                                    }
                                });
                                break;
                            default:
                                LogDebug<MainStandbySyncServer>($"{Id} Comando Desconocido. No se procesa.");
                                break;
                        }
                    }
#if DEBUG
                    else
                    {
                        LogDebug<MainStandbySyncServer>($"{Id} Recibido Mensaje propio.");
                    }
#endif
                }
                catch (Exception x)
                {
                    LogException<MainStandbySyncServer>($"{Id} Exception", x);
                    ResetLan();
                }
            });

        }
        private void SignalVersionDataReceived()
        {
            versync.Set();
        }
        void ResetLan()
        {
            LogDebug<MainStandbySyncServer>($"{Id} Reseting Listener");
            Listener?.Dispose();
            Listener = null;
        }

        /// <summary>
        /// 
        /// </summary>
        string Id => $"On SyncServer ({Ip_propia}):";
        Task ExecutiveThread { get; set; } = null;
        CancellationTokenSource ExecutiveThreadCancel { get; set; } = null;
        private UdpSocket Listener { get; set; } = null;
        private IPAddress Ip_propia { get; set; } = null;
        private IPEndPoint Ip_grp { get; set; } = null;
        private EventQueue ThreadSync { get; set; } = null;

        /** */
        private static ManualResetEvent versync = new ManualResetEvent(false);

    }
}
