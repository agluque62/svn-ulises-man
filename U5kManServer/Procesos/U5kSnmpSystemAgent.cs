using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using System.Net;
using System.Threading;

using Lextm.SharpSnmpLib;

using U5kBaseDatos;
using Utilities;

namespace U5kManServer
{
    /// <summary>
    /// 
    /// </summary>
    delegate void Supervisor();

    /// <summary>
    /// 
    /// </summary>
    class U5kSnmpSystemAgent : NucleoGeneric.NGThread
    {
        enum SystemAgentState { NotInit, NotStarted, Operative }
        string ipServ = Properties.u5kManServer.Default.MiDireccionIP;  //  U5kGenericos.MiDireccionIP;
        /// <summary>
        /// 
        /// </summary>
        U5kScvMib _mib;

        /// <summary>
        /// Para gestionar los eventos de las Pasarelas...
        /// </summary>
        EventosRadio _evradio = new EventosRadio();
        EventosLC _evLcen = new EventosLC();
        EventosTLF _evTlf = new EventosTLF();
        EventosATS _evAts = new EventosATS();
        SnmpAgent snmpAgent = new SnmpAgent();

        SystemAgentState State { get; set; }
        public bool ReloadRequest { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public U5kSnmpSystemAgent()
        {
            Name = "U5kSnmpSystemAgent";
            State = SystemAgentState.NotInit;
            ReloadRequest = false;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Run()
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name);
            List<Action> functions = new List<Action>()
            {
                SupervisarPuestos,
                Supervisa_sysUpTime,
                SupervisarLineas,
                SupervisarParametrosGenerales,
                Supervisa_tablas_v2
            };

            Decimal interval = Properties.u5kManServer.Default.SpvInterval;
            //U5kGenericos.SetCurrentCulture();

            LogInfo<U5kSnmpSystemAgent>("U5kSnmpSystemAgent, running...");
            using (timer = new TaskTimer(new TimeSpan(0, 0, 0, 0, Decimal.ToInt32(interval)), this.Cancel))
            {
                while (IsRunning())
                {
                    switch (State)
                    {
                        case SystemAgentState.NotInit:
                            if (SnmpAgentInit())
                            {
                                State = SystemAgentState.NotStarted;
                            }
                            break;

                        case SystemAgentState.NotStarted:
                            if (SnmpAgentStart())
                            {
                                State = SystemAgentState.Operative;
                                ReloadRequest = false;
                            }
                            else
                            {
                                SnmpAgentStop();
                                State = SystemAgentState.NotInit;
                            }
                            break;

                        case SystemAgentState.Operative:

                            if (ReloadRequest == true)
                            {
                                SnmpAgentStop();
                                State = SystemAgentState.NotInit;
                                ReloadRequest = false;
                            }
                            else
                            {
                                functions.ForEach(f =>
                                {
                                    if (U5kManService._std.wrAccAcquire())
                                    {
                                        try
                                        {
                                            if (U5kManService._Master == true)
                                                f();
                                        }
                                        catch (Exception x)
                                        {
                                            LogException<U5kSnmpSystemAgent>("", x);
                                        }
                                        finally
                                        {
                                            U5kManService._std.wrAccRelease();
                                        }
                                    }
                                });
                            }
                            break;
                    }
                    GoToSleepInTimer();
                }
            }            
            SnmpAgentStop();
            LogInfo<U5kSnmpSystemAgent>("U5kSnmpSystemAgent stopped...");
        }

        /// <summary>
        /// Carga la base de OID's del Agente.
        /// </summary>
        bool SnmpAgentInit()
        {
            try
            {
                _mib = new U5kScvMib();

                snmpAgent.Init(ipServ);            // Poner la IP del Servidor....
                snmpAgent.TrapReceived += new Action<string, string, ISnmpData, IPEndPoint, IPEndPoint>(RecibidoTrap);

                LogInfo<U5kSnmpSystemAgent>("Agente SNMP. configurado");
                return true;
            }
            catch(Exception x)
            {
                LogError<U5kSnmpSystemAgent>("Error Inicializando Agente SNMP: " + x.Message);
                SnmpAgentStop();
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        bool SnmpAgentStart()
        {
            try
            {
                _mib.Load();
                snmpAgent.Start();
                LogInfo<U5kSnmpSystemAgent>("Agente SNMP. Arrancado");
                return true;
            }
            catch (Exception x)
            {
                LogError<U5kSnmpSystemAgent>("Error Arrancando Agente SNMP: " + x.Message);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void SnmpAgentStop()
        {
            try
            {
                snmpAgent.Close();
                _mib.Dispose();
                _mib = null;
                LogInfo<U5kSnmpSystemAgent>("Agente SNMP. Detenido...");
            }
            catch(Exception x)
            {
                LogError<U5kSnmpSystemAgent>("Error Arrancando Agente SNMP: " + x.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>        
        void SupervisarParametrosGenerales()
        {
            /** Supervisa la Configurcion */
            _mib.SupervisaCfgSettings();
        }

        /// <summary>
        /// 
        /// </summary>
        void Supervisa_sysUpTime()
        {
            _mib.sysUpTime();
        }

        /// <summary>
        /// 
        /// </summary>
        void Supervisa_tablas_v2()
        {
            _mib.sysUpTime();
            // 
            _mib.MttoV2Tick();
        }

        /// <summary>
        /// 
        /// </summary>
        void Supervisa_Mib2()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        void SupervisarPuestos()
        {
            List<stdPos> Posiciones = U5kManService._std.STDTOPS.OrderBy(e => e.name).ToList();
            for (int npos = 0; npos < Posiciones.Count; npos++)
            {
                stdPos pos = Posiciones[npos];
                _mib.itfPosicion(npos, pos.stdpos, pos.name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void SupervisarLineas()
        {
            /** */
            mib2OperStatus globalStatusTlf = mib2OperStatus.down;
            mib2OperStatus globalStatusRad = mib2OperStatus.down;
            mib2OperStatus globalStatusRec = mib2OperStatus.down;

            /** Estados de Interfaces LEGACY */
            List<stdGw> stdgws = U5kManService._std.STDGWS;
            foreach (stdGw gw in stdgws)
            {
                foreach (stdSlot slot in gw.gwA.slots)
                {
                    if (slot != null)
                    {
                        foreach (stdRec rc in slot.rec)
                        {
                            if (rc != null)
                            {
                                mib2OperStatus itemStatus = MibHelper.std2mib2OperStatus(rc.std_online);
                                if (rc.tipo == itf.rcRadio) // TODO. repensar los estado..
                                {
                                    globalStatusTlf = itemStatus == mib2OperStatus.up ? itemStatus : globalStatusTlf;
                                }
                                else if (rc.tipo != itf.rcNotipo)
                                {
                                    globalStatusRad = itemStatus == mib2OperStatus.up ? itemStatus : globalStatusRad;
                                }
                            }
                        }
                    }
                }
            }

            /** Estados de Interfaces IP */
            List<U5kManStdEquiposEurocae.EquipoEurocae> stdeqeu = U5kManService._std.STDEQS;
            foreach (U5kManStdEquiposEurocae.EquipoEurocae equipo in stdeqeu)
            {
                mib2OperStatus itemStatus = MibHelper.std2mib2OperStatus(equipo.EstadoGeneral);
                if (equipo.Tipo == 3 )
                    globalStatusTlf = itemStatus == mib2OperStatus.up ? itemStatus : globalStatusTlf;
                else if (equipo.Tipo == 2)
                    globalStatusRad = itemStatus == mib2OperStatus.up ? itemStatus : globalStatusRad;
                else if (equipo.Tipo == 5)
                    globalStatusRec = itemStatus == mib2OperStatus.up ? itemStatus : globalStatusRec;
            }

            _mib.itfTelefonia = globalStatusTlf;
            _mib.itfRadio = globalStatusRad;
            _mib.itfRecorder = globalStatusRec;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="data"></param>
        /// <param name="ipfrom"></param>
        void RecibidoTrap(string oidtrap, string oidvar, ISnmpData data, IPEndPoint ipfrom, IPEndPoint ipto)
        {
            if (U5kManService._std.wrAccAcquire())
            {
                try
                {
                    if (U5kManService._Master == true)
                    {
                        List<stdPos> stdpos = U5kManService._std.STDTOPS;
#if DEBUG1
                ipfrom.Address = IPAddress.Parse("10.12.60.129");
#endif
                        stdPos pos = stdpos.Find(r => r.ip == ipfrom.Address.ToString());                                                                   // Busco si es una posicion.
                        List<stdGw> stdgws = U5kManService._std.STDGWS;
                        stdGw gw = stdgws.Find(r => r.ip==ipfrom.Address.ToString() || r.gwA.ip == ipfrom.Address.ToString() || r.gwB.ip == ipfrom.Address.ToString());      // Busco si es una Pasarela.
                        if (oidvar.StartsWith(Properties.u5kManServer.Default.HfEventOids) ||
                            oidvar.StartsWith(Properties.u5kManServer.Default.CfgEventOid))
                        {
                            RecibidoEventoExterno(oidvar, ((OctetString)data).ToString());
                        }
                        else
                        {
                            //            if (gw != null && gw.stdgw != std.NoInfo)                                                                              // Proviene de una Pasarela.
                            if (gw != null)
                            {
                                stdPhGw pgw = gw.gwA.ip == ipfrom.Address.ToString() ? gw.gwA : gw.gwB;
                                eGwPar par = GwExplorer._GwOids.FirstOrDefault(x => x.Value == oidvar).Key;

                                /** Pasarelas no Unificadas */
                                if (par != eGwPar.None)
                                {
                                    //GwSnmpExplorer.RecibidoTrapGw(ipto.Port, gw, pgw, par, data);
                                }
                                else if (EventosRecursos.IsEventRadio(oidvar) == true)
                                {
                                    if (_evradio.AutomataEventos(pgw, ipto.Port, oidvar, data) == true)
                                    {
                                        RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, (eIncidencias)_evradio.lastInci, eTiposInci.TEH_TIFX, gw.name, _evradio.lastParameters);
                                    }
                                }
                                else if (EventosRecursos.IsEventLcen(oidvar) == true)
                                {
                                    if (_evLcen.AutomataEventos(pgw, ipto.Port, oidvar, data) == true)
                                    {
                                        RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, (eIncidencias)_evLcen.lastInci, eTiposInci.TEH_TIFX, gw.name, _evLcen.lastParameters);
                                    }
                                }
                                else if (EventosRecursos.IsEventTlf(oidvar) == true)
                                {
                                    if (_evTlf.AutomataEventos(pgw, ipto.Port, oidvar, data) == true)
                                    {
                                        RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, (eIncidencias)_evTlf.lastInci, eTiposInci.TEH_TIFX, gw.name, _evTlf.lastParameters);
                                    }
                                }
                                else if (EventosRecursos.IsEventAts(oidvar) == true)
                                {
                                    if (_evAts.AutomataEventos(pgw, ipto.Port, oidvar, data) == true)
                                    {
                                        RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, (eIncidencias)_evAts.lastInci, eTiposInci.TEH_TIFX, gw.name, _evAts.lastParameters);
                                    }
                                }
                                else if (oidtrap.Contains(".1.3.6.1.4.1.7916.8.3.2.1") == true)
                                {
                                    /** Pasarelas Unificadas */
                                    GwExplorer.RecibidoTrapGw_unificada(gw, pgw, oidtrap, oidvar, data);
                                    U5kManService._std.STDGWS = stdgws;
                                }
                                else
                                {
                                    LogWarn<U5kSnmpSystemAgent>(String.Format("Recibido TrapGW {0}:{1} oid={2}. Comando No esperado", ipfrom.Address.ToString(), ipto.Port, oidvar));
                                }
                            }
                            else if (pos != null /*&& pos.stdpos != std.NoInfo*/)          // Proviene de un Puesto.
                            {
                                eTopPar par = TopSnmpExplorer._OidPos.FirstOrDefault(x => x.Value == oidvar).Key;
                                if (par != eTopPar.None)
                                {
                                    RecibidoTrapPosicion(pos, par, data);
                                    U5kManService._std.STDTOPS = stdpos;
                                }
                                else
                                {
                                    LogWarn<U5kSnmpSystemAgent>(String.Format("Recibido TrapPOS {0}:{1} oid={2}. Comando No esperado", ipfrom.Address.ToString(), ipfrom.Port, oidvar));
                                }
                            }
                            else
                            {
                                LogWarn<U5kSnmpSystemAgent>(String.Format("Recibido TRAP No Esperado de {0}.{1}={2}", ipfrom, oidvar, 0));
                            }
                        }
                    }
                }
                catch (Exception x)
                {
                    LogException<U5kSnmpSystemAgent>("", x);
                }
                finally
                {
                    U5kManService._std.wrAccRelease();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="parametro"></param>
        /// <param name="valor"></param>
        void RecibidoTrapPosicion(stdPos pos, eTopPar parametro, ISnmpData data)
        {
            LogTrace<U5kSnmpSystemAgent>(String.Format("Trap POS {0}, {1}", pos.ip, parametro));
            switch (parametro)
            {
                case eTopPar.Top:
                    TopSnmpExplorer.EstadoPosicionSet(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : std.Ok);
                    break;

                case eTopPar.EstadoPanel:
                    TopSnmpExplorer.PosicionPanelSet(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : std.Ok);
                    break;

                case eTopPar.EstadoJacksEjecutivo:
                    TopSnmpExplorer.PosicionEjecutivoSet(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : std.Ok);
                    break;

                case eTopPar.EstadoJacksAyudante:
                    TopSnmpExplorer.PosicionAyudanteSet(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : std.Ok);
                    break;

                case eTopPar.EstadoAltavozRadio:
                    TopSnmpExplorer.PosicionALRSet(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : std.Ok);
                    break;

                case eTopPar.EstadoAltavozLC:               // Entero
                    TopSnmpExplorer.PosicionALTSet(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : std.Ok);
                    break;

                case eTopPar.EstadoLan1:
                    TopSnmpExplorer.PosicionLan1Set(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : ((Integer32)data).ToInt32() == 1 ? std.Ok : std.Error);
                    break;

                case eTopPar.EstadoLan2:
                    TopSnmpExplorer.PosicionLan2Set(pos.name, pos, ((Integer32)data).ToInt32() == 0 ? std.NoInfo : ((Integer32)data).ToInt32() == 1 ? std.Ok : std.Error);
                    break;

                case eTopPar.EstadoSync:
                    TopSnmpExplorer.PosicionSyncStatusSet(pos.name, pos, ((OctetString)data).ToString());
                    break;

                case eTopPar.SeleccionPaginaRadio:          // Entero.
                    RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_PAGINA_FRECUENCIAS, 0, pos.name, Params(((Integer32)data).ToInt32(), pos.name));
                    break;

                case eTopPar.EstadoPtt:                     // String... 'SECT_1 PTT-ON en Sector, SECT_0 PTT-OFF en Sector...
                    {
                        string[] _str = ((OctetString)data).ToString().Split('_');
                        if (_str.Length == 2)
                            RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_PTT, 0, pos.name, Params(pos.name, _str[0] == "0" ? "OFF" : "ON", _str[1]));
                    }
                    break;

                case eTopPar.LlamadaEntrante:               // STRING
                    RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_LLAMADA_ENTRANTE, eTiposInci.TEH_TOP, pos.name, Params(((OctetString)data).ToString(), pos.name));
                    break;
                case eTopPar.LlamadaSaliente:               // STRING
                    RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_LLAMADA_SALIENTE, eTiposInci.TEH_TOP, pos.name, Params(((OctetString)data).ToString(), pos.name));
                    break;
                case eTopPar.LlamadaEstablecida:            // STRING
                    RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_LLAMADA_ESTABLECIDA, eTiposInci.TEH_TOP, pos.name, Params(((OctetString)data).ToString(), pos.name));
                    break;
                case eTopPar.LlamadaFinaliza:               // STRING
                    RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_LLAMADA_FIN, eTiposInci.TEH_TOP, pos.name, Params(((OctetString)data).ToString(), pos.name));
                    break;

                case eTopPar.FacilidadTelefonia:            // String
                    RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_FACILIDAD_SELECCIONADA, eTiposInci.TEH_TOP, pos.name, Params(((OctetString)data).ToString(), pos.name));
                    break;

                case eTopPar.Briefing:                      // STRING
                    {
                        string[] _str = ((OctetString)data).ToString().Split('_');
                        if (_str.Length == 2)
                            RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_SESION_BREIFING, eTiposInci.TEH_TOP, pos.name, Params(_str[1] == "0" ? "OFF" : "ON", _str[0]));
                    }
                    break;

                case eTopPar.Replay:                        // STRING
                    {
                        string[] _str = ((OctetString)data).ToString().Split('_');
                        if (_str.Length == 2)
                            RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, eIncidencias.ITO_FUNCION_REPLAY, eTiposInci.TEH_TOP, pos.name, Params(_str[1] == "0" ? "OFF" : "ON", _str[0]));
                    }
                    break;

                default:
                    LogInfo<U5kSnmpSystemAgent>(String.Format("Recibido TRAP-TOP OID-No Esperado de {0}.{1}", pos.ip, parametro.ToString()));
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="valor"></param>
        void RecibidoEventoExterno(string oid, string valor)
        {
            int nInci;
            string idhw = oid.StartsWith(Properties.u5kManServer.Default.HfEventOids) ? "NBX" :
                oid.StartsWith(Properties.u5kManServer.Default.CfgEventOid) ? "CFG" : "???";

            string[] _oid_val = oid.Split('.');
            if (int.TryParse(_oid_val[_oid_val.Length - 1], out nInci))
            {
                List<string> _params = valor.Split(',').ToList();

                /** El primer Parámetro es el numero de Incidencia */
                _params.RemoveAt(0);

                /** Si viene del NODEBOX, el segundo parámetro es el ID-Hardware*/
                if (idhw == "NBX")
                {
                    idhw = _params[0];
                    _params.RemoveAt(0);
                }

                /** Completo con un máximo de 8 parámetros.... */
                for (int i = _params.Count; i < 8; i++)
                    _params.Add("");
                RecordEvent<U5kSnmpSystemAgent>(DateTime.Now, (eIncidencias)nInci, eTiposInci.TEH_SISTEMA, idhw, _params.ToArray());
            }
            else
            {
                LogError<U5kSnmpSystemAgent>(String.Format("RecibidoEventoExterno. Error de Formato. OID={0}, VAL={1}", oid, valor));
            }

        }

#if DEBUG
        public void TestTrap()
        {
            RecibidoTrap("", ".1.1.100.1.3051",
                new OctetString("3051,[GEAR INIT] Error de configuraci?n en el Canal de la emision. Se asigna el valor por defecto.  Node: RX_UHF_M1 {IP=192.168.2.101} {S=Initial} {F=243.000} {ReB=} {ReT=}"),
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 162),
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 621));
        }
#endif
    }
}
