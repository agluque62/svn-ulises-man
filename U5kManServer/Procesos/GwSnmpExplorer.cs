using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Net;

using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.CompilerServices;

using Lextm.SharpSnmpLib;

using U5kBaseDatos;
using U5kManServer.WebAppServer;
using Utilities;
using U5kManServer.Procesos;

namespace U5kManServer
{
    enum eGwPar
    {
        None = 0,
        GwStatus,
        Slot0Type, Slot1Type, Slot2Type, Slot3Type,
        Slot0Status, Slot1Status, Slot2Status, Slot3Status, LanStatus, MainOrStandby,
        ResourceType,
        RadioResourceType, RadioResourceStatus,
        IntercommResourceType, IntercommResourceStatus,
        LegacyPhoneResourceType, LegacyPhoneResourceStatus,
        ATSPhoneResourceType, ATSPhoneResourceStatus,
        Lan2Status
    };
    class GwHelper
    {
        public static string SlotStd2String(Int32 nslot, Int32 tipo, Int32 slotstd)
        {
            // BIT | 7 | 6 | 5 | 4 | 3 | 2 | 1 | 0 |
            // CAN | - | - | - | 3 | 2 | 1 | 0 | - |
            return tipo == 2 ? String.Format("{4}: [{0} {1} {2} {3}]",
                                (slotstd & Convert.ToInt32("00010", 2)) != 0 ? "0" : "-",
                                (slotstd & Convert.ToInt32("00100", 2)) != 0 ? "1" : "-",
                                (slotstd & Convert.ToInt32("01000", 2)) != 0 ? "2" : "-",
                                (slotstd & Convert.ToInt32("10000", 2)) != 0 ? "3" : "-",
                                nslot) : String.Format("{0}", nslot);
        }
        public static void SetToOutOfOrder(stdPhGw phgw)
        {
            phgw.SnmpDataReset();
            phgw.version = string.Empty;
        }
    }
    class GwExplorer : NucleoGeneric.NGThread
    {
        static string OidGet(string id, string _default)
        {
            foreach (string s in Properties.u5kManServer.Default.GwOids)
            {
                string[] p = s.Split(':');
                if (p.Count() == 2 && p[0] == id)
                    return p[1];
            }

            return _default;
        }
        static public Dictionary<eGwPar, string> _GwOids = new Dictionary<eGwPar, string>()
        {
            // General de la Pasarela.
            {eGwPar.GwStatus,OidGet("EstadoGw",".1.1.100.2.0")},
            {eGwPar.Slot0Type,OidGet("TipoSlot0",".1.1.100.31.1.1.0")},
            {eGwPar.Slot1Type,OidGet("TipoSlot1",".1.1.100.31.1.1.1")},
            {eGwPar.Slot2Type,OidGet("TipoSlot2",".1.1.100.31.1.1.2")},
            {eGwPar.Slot3Type,OidGet("TipoSlot3",".1.1.100.31.1.1.3")},
            {eGwPar.Slot0Status,OidGet("EstadoSlot0",".1.1.100.31.1.2.0")},
            {eGwPar.Slot1Status,OidGet("EstadoSlot1",".1.1.100.31.1.2.1")},
            {eGwPar.Slot2Status,OidGet("EstadoSlot2",".1.1.100.31.1.2.2")},
            {eGwPar.Slot3Status,OidGet("EstadoSlot3",".1.1.100.31.1.2.3")},
            {eGwPar.MainOrStandby,OidGet("PrincipalReserva","1.1.100.21.0")},
            {eGwPar.LanStatus,OidGet("EstadoLan","1.1.100.22.0")},
        
            // Para cada Recurso.
            {eGwPar.ResourceType,OidGet("TipoRecurso",".1.1.100.100.0")},

            {eGwPar.RadioResourceType,OidGet("TipoRecursoRadio",".1.1.200")},
            {eGwPar.RadioResourceStatus,OidGet("EstadoRecursoRadio",".1.1.200.2.0")},

            {eGwPar.IntercommResourceType,OidGet("TipoRecursoLC",".1.1.300")},
            {eGwPar.IntercommResourceStatus,OidGet("EstadoRecursoLC",".1.1.300.2.0")},

            {eGwPar.LegacyPhoneResourceType,OidGet("TipoRecursoTF",".1.1.400")},
            {eGwPar.LegacyPhoneResourceStatus,OidGet("EstadoRecursoTF",".1.1.400.2.0")},

            {eGwPar.ATSPhoneResourceType,OidGet("TipoRecursoATS",".1.1.500")},
            {eGwPar.ATSPhoneResourceStatus,OidGet("EstadoRecursoATS",".1.1.500.2.0")},

        };
        List<Variable> _GwVarList = new List<Variable>()
        {
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.GwStatus])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.MainOrStandby])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.LanStatus])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot0Type])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot1Type])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot2Type])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot3Type])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot0Status])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot1Status])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot2Status])),
            new Variable(new ObjectIdentifier(_GwOids[eGwPar.Slot3Status]))
        };

        const int RadioResource_AgentType = 2;
        const int IntercommResource_AgentType = 3;
        const int LegacyPhoneResource_AgentType = 4;
        const int ATSPhoneResource_AgentType = 5;

        Dictionary<int, string> _TypesOids = new Dictionary<int, string>()
        {
            { RadioResource_AgentType, _GwOids[eGwPar.RadioResourceType] },
            { IntercommResource_AgentType, _GwOids[eGwPar.IntercommResourceType] },
            { LegacyPhoneResource_AgentType, _GwOids[eGwPar.LegacyPhoneResourceType]},
            { ATSPhoneResource_AgentType, _GwOids[eGwPar.ATSPhoneResourceType] },
        };
        Dictionary<int, string> _StatusOids = new Dictionary<int, string>()
        {
            { RadioResource_AgentType, _GwOids[eGwPar.RadioResourceStatus] },
            { IntercommResource_AgentType, _GwOids[eGwPar.IntercommResourceStatus] },
            { LegacyPhoneResource_AgentType, _GwOids[eGwPar.LegacyPhoneResourceStatus] },
            { ATSPhoneResource_AgentType, _GwOids[eGwPar.ATSPhoneResourceStatus] },
        };
        Dictionary<trc, eIncidencias> ResourceActivationEventsCodes = new Dictionary<trc, eIncidencias>()
        {
            {trc.rcRadio, eIncidencias.IGW_CONEXION_RECURSO_RADIO},
            {trc.rcTLF, eIncidencias.IGW_CONEXION_RECURSO_TLF},
            {trc.rcLCE, eIncidencias.IGW_CONEXION_RECURSO_TLF},
            {trc.rcATS, eIncidencias.IGW_CONEXION_RECURSO_R2},
            {trc.rcNotipo, eIncidencias.IGNORE}
        };
        Dictionary<trc, eIncidencias> ResourceDeactivationEventsCodes = new Dictionary<trc, eIncidencias>()
        {
            {trc.rcRadio, eIncidencias.IGW_DESCONEXION_RECURSO_RADIO},
            {trc.rcTLF, eIncidencias.IGW_DESCONEXION_RECURSO_TLF},
            {trc.rcLCE, eIncidencias.IGW_DESCONEXION_RECURSO_TLF},
            {trc.rcATS, eIncidencias.IGW_DESCONEXION_RECURSO_R2},
            {trc.rcNotipo, eIncidencias.IGNORE}
        };

        IProcessData DataS = null;
        IProcessPing PingS = null;
        IProcessSip SipS = null;
        IProcessSnmp SnmpS = null;
        IProcessHttp HttpS = null;
        public GwExplorer(
            IProcessData pdata = null, 
            IProcessPing pping = null, 
            IProcessSip psip = null,
            IProcessSnmp psnmp = null,
            IProcessHttp phttp = null)
        {
            DataS = pdata ?? new RunTimeData();
            PingS = pping ?? new RuntimePingService();
            SipS  = psip ?? new RuntimeSipService();
            SnmpS = psnmp ?? new RuntimeSnmpService();
            HttpS = phttp ?? new RuntimeHttpService();
            Name = "GwSnmpExplorer";
            SnmpS.TrapReceived += SnmpTrapReceived;
        }
        protected override void Run()
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name);

            LogInfo<GwExplorer>("Arrancado...");

            Decimal interval = Properties.u5kManServer.Default.SpvInterval;     // Tiempo de Polling,
            Decimal threadTimeout = 2 * interval / 3;                           // Tiempo de proceso individual.
            Decimal poolTimeout = 3 * interval / 4;                             // Tiempo máximo del Pool de Procesos.            
            
            // 20200805. Control del Polling a pasarelas.
            var taskControl = new PollingHelper();
            using (timer = new TaskTimer(TimeSpan.FromMilliseconds((double)interval), this.Cancel))
            {
                while (IsRunning())
                {
                    if (DataS.IsMaster == true)
                    {
                        GlobalServices.GetWriteAccess(() =>
                        {
                            // limpiar pollingControl con las Pasarelas que puedan desaparecer de la configuracion.
                            taskControl.DeleteNotPresent(DataS.Data.STDGWS.Select(g => g.name).ToList());
                            // Relleno los datos...
                            DataS.Data.STDGWS.ForEach(gw =>
                            {
                                if (taskControl.IsTaskActive(gw.name) == false)
                                {
                                    var newGw = new stdGw(gw);
                                    var task = Task.Factory.StartNew(() =>
                                    {
                                        try
                                        {
                                            LogTrace<GwExplorer>($"Exploracion {newGw.name} iniciada.");
                                            ExploraGw(newGw);
                                            /// Copio los datos obtenidos a la tabla...
                                            GlobalServices.GetWriteAccess(() =>
                                            {
                                                if (DataS.Data.GWSDIC.ContainsKey(newGw.name))
                                                {
                                                    /** 20200813. Solo actualiza el estado si no se ha cambiado en medio la configuracion */
                                                    if (DataS.Data.GWSDIC[newGw.name].Equals(newGw))
                                                    {
                                                        DataS.Data.GWSDIC[newGw.name].CopyFrom(newGw);
                                                    }
                                                    else
                                                    {
                                                        LogWarn<GwExplorer>($"Exploracion {newGw.name}. Resultado Exploracion ignorado. Cambio de configuracion.");
                                                    }
                                                }
                                                else
                                                {
                                                    LogWarn<GwExplorer>($"Exploracion {newGw.name}. Resultado Exploracion ignorado.  Pasarela Eliminada");
                                                }
                                            });
                                        }
                                        catch (Exception x)
                                        {
                                            LogException<GwExplorer>($"In ({newGw.name}, {newGw.ip}) Exception when monitoring", x);
                                        }
                                        finally
                                        {
                                            LogTrace<GwExplorer>($"Exploracion de {newGw.name} finalizada.");
                                        }
                                    }, TaskCreationOptions.LongRunning);
                                    taskControl.SetTask(gw.name, task);
                                }
                                else
                                {
                                    // TODO. Algun tipo de supervision si nunca vuelve...
                                    LogWarn<GwExplorer>($"Exploracion de Pasarela {gw.name} no finalizada en Tiempo ...");
                                }
                            });
                        });
                    }
                    GoToSleepInTimer();
                }
            }
            Dispose();
            LogInfo<GwExplorer>("Finalizado...");
        }

        void SlotTypeSet(/*stdGw gw, */stdPhGw pgw, int nslot, stdSlot slot, int tipo, int estado)
        {
            std current = tipo == 2 ? std.Ok : std.NoInfo;
            estado = tipo == 2 ? (estado | 0x01) : (estado & 0xFFFE);

            if (slot.lastResMsc != estado || current != slot.std_online)
            {
                if (current == std.Ok)
                {
                    //RecordEvent<GwExplorer>(DateTime.Now, eIncidencias.IGW_CONEXION_IA4, eTiposInci.TEH_TIFX, pgw.name, /*nslot, */
                    //    Params(GwHelper.SlotStd2String(nslot, 2, estado)));
                    PushEvent(pgw, eIncidencias.IGW_CONEXION_IA4, eTiposInci.TEH_TIFX, pgw.name, /*nslot, */
                        Params(GwHelper.SlotStd2String(nslot, 2, estado)));
                }
                else
                {
                    //RecordEvent<GwExplorer>(DateTime.Now, eIncidencias.IGW_DESCONEXION_IA4, eTiposInci.TEH_TIFX, pgw.name, /*nslot, */
                    //    Params(GwHelper.SlotStd2String(nslot, 2, estado)));
                    PushEvent(pgw, eIncidencias.IGW_DESCONEXION_IA4, eTiposInci.TEH_TIFX, pgw.name, /*nslot, */
                        Params(GwHelper.SlotStd2String(nslot, 2, estado)));
                    slot.Reset();
                }
                slot.lastResMsc = estado;
                slot.std_online = current;
            }
        }
        void SlotStateSet(/*stdGw gw, */stdPhGw pgw, int nslot, stdSlot slot, int estado)
        {
            /** El primer bit es el estado de la tarjeta */
            estado = (estado >> 1);

            for (int irec = 0; irec < 4; irec++)
            {
                stdRec rec = slot.rec[irec];
                bool presente = ((estado >> irec) & 1) == 1 ? true : false;

                // 20180111. Quitamos este historico....
                // ChangeStatus(rec.presente ? std.Ok : std.NoInfo,
                //    presente ? std.Ok : std.NoInfo,
                //    0,
                //    eIncidencias.IGW_EVENTO,
                //    eTiposInci.TEH_TIFX,
                //    pgw.name,
                //    presente ? idiomas.strings.GWS_ResInterfazSi : idiomas.strings.GWS_ResInterfazNo,   // "Interfaz de Recurso Disponible" : "Interfaz de Recurso no Disponible",
                //    nslot, irec);

                // 20180111. Quitamos el historico y solo actualizamos las variables locales...
                //eIncidencias inci = rec.tipo == itf.rcRadio ? eIncidencias.IGW_DESCONEXION_RECURSO_RADIO :
                //    rec.tipo == itf.rcAtsN5 || rec.tipo == itf.rcAtsR2 ? eIncidencias.IGW_DESCONEXION_RECURSO_R2 :
                //    eIncidencias.IGW_DESCONEXION_RECURSO_TLF;

                //rec.std_online = ChangeStatus(rec.std_online,
                //                              (presente == false ? std.NoInfo : rec.std_online),
                //                              0,
                //                              inci,
                //                              eTiposInci.TEH_TIFX, pgw.name, rec.name);
                rec.presente = presente;
                rec.std_online = (presente == false ? std.NoInfo : rec.std_online);
            }
        }
        void SlotRecursoTipoAgenteSet(/*stdGw gw, */stdPhGw pgw, stdRec rec, int tipo)
        {
            rec.tipo_online = (trc)tipo;              // Todo. Generar Incidencia si procede. Esta Incidencia no Existe.
        }
        void SlotRecursoTipoInterfazSet(/*stdGw gw, */stdPhGw pgw, stdRec rec, int tipo)
        {
            rec.tipo_itf = (itf)tipo;               // Todo. Generar Incidencia si procede. Esta Incidencia no Existe.
        }
        void SlotRecursoEstadoSet(/*stdGw gw, */stdPhGw pgw, stdRec rec, int estado, trc tipo)
        {
            if (rec.presente)
            {
                if (tipo == trc.rcRadio)
                {
                    /** Para evitar los Fuera de Servicio por falta de sesion radio... */
                    std current = estado == 1 ? std.Ok : std.Error;
                    if (rec.std_online != current)
                    {
                        rec.std_online = current;
                        PushEvent(pgw, estado != 0 ? ResourceActivationEventsCodes[tipo] : ResourceDeactivationEventsCodes[tipo],
                              eTiposInci.TEH_TIFX, pgw.name, Params(rec.name));
                    }
                }
                else
                {
                    var newstd = estado == 1 ? std.Ok : std.Error;
                    if (rec.std_online != newstd)
                    {
                        PushEvent(pgw,
                            estado == 1 ? ResourceActivationEventsCodes[tipo] : ResourceDeactivationEventsCodes[tipo],
                            eTiposInci.TEH_TIFX, pgw.name, Params(rec.name));
                        rec.std_online = newstd;
                    }
                }
            }
            else
            {
                rec.std_online = std.NoInfo;
            }
        }
        void PhGwPrincipalReservaSet(/*stdGw gw, */stdPhGw pgw, int estado)
        {
            var newSel = estado == 1;
            if (pgw.Seleccionada != newSel)
            {
                PushEvent(pgw, eIncidencias.IGW_PRINCIPAL_RESERVA, eTiposInci.TEH_TIFX, pgw.name,
                    Params(pgw.name, estado == 0 ? idiomas.strings.GWS_Reserva : idiomas.strings.GWS_Principal));
                pgw.Seleccionada = newSel;
            }
        }
        void PhGwLanStatusSet(/*stdGw gw, */stdPhGw pgw, int status)
        {
            var oldlan1 = pgw.lan1;
            var oldlan2 = pgw.lan2;

            int bond = (status & 0x4) >> 2;
            int eth1 = (status & 0x2) >> 1;
            int eth0 = (status & 0x1);

            pgw.lan1 = eth0 == 1 ? std.Ok : std.Error;
            pgw.lan2 = bond == 0 ? std.NoInfo : eth1 == 1 ? std.Ok : std.Error;

            /** 20220224. Historicos de Activacion / Desactivacion LAN */
            if (oldlan1 != pgw.lan1)
            {
                PushEvent(pgw,
                    pgw.lan1 == std.Error ? eIncidencias.IGW_CAIDA : eIncidencias.IGW_ENTRADA,
                    eTiposInci.TEH_TIFX,
                    pgw.name,
                    Params("Lan1"));
            }
            if (oldlan2 != pgw.lan2)
            {
                PushEvent(pgw,
                    pgw.lan2 == std.Error ? eIncidencias.IGW_CAIDA : eIncidencias.IGW_ENTRADA,
                    eTiposInci.TEH_TIFX,
                    pgw.name,
                    Params("Lan2"));
            }
        }

        bool TestTipoNotificado(int tipo)
        {
            return tipo ==
                RadioResource_AgentType ||
                tipo == IntercommResource_AgentType ||
                tipo == LegacyPhoneResource_AgentType ||
                tipo == ATSPhoneResource_AgentType ? true : false;
        }

        enum GwGlobalTransitions { ToInactive, ToActiveNoError, ToActiveError };
        void ManageGlobalTransition(std previus, std actual, Action<GwGlobalTransitions> respond)
        {
            var AllowedTransitions = new List<System.Tuple<std, std, GwGlobalTransitions>>()
            {
                new System.Tuple<std, std, GwGlobalTransitions>(std.NoInfo, std.Ok, GwGlobalTransitions.ToActiveNoError),
                new System.Tuple<std, std, GwGlobalTransitions>(std.NoInfo, std.Error, GwGlobalTransitions.ToActiveError),
                new System.Tuple<std, std, GwGlobalTransitions>(std.Error, std.NoInfo, GwGlobalTransitions.ToInactive),
                new System.Tuple<std, std, GwGlobalTransitions>(std.Error, std.Ok, GwGlobalTransitions.ToActiveNoError),
                new System.Tuple<std, std, GwGlobalTransitions>(std.Ok, std.NoInfo, GwGlobalTransitions.ToInactive),
                new System.Tuple<std, std, GwGlobalTransitions>(std.Ok, std.Error, GwGlobalTransitions.ToActiveError)
            };
            var found = AllowedTransitions.Where(t => t.Item1 == previus && t.Item2 == actual).FirstOrDefault();
            if (found != null)
                respond(found.Item3);
        }
        void GwActualizaEstado(stdGw gw)
        {
            std std_old = gw.std;

            gw.presente = gw.Dual == false ? gw.gwA.presente : (gw.gwA.presente == true || gw.gwB.presente == true) ? true : false;
            if (gw.presente == false)
                gw.Reset();
            gw.std = gw.presente == false ? std.NoInfo : gw.Errores == true ? std.Error : std.Ok;

            ManageGlobalTransition(std_old, gw.std, (transition) =>
            {
                switch (transition)
                {
                    case GwGlobalTransitions.ToInactive:
                        RecordEvent<GwExplorer>(
                            DateTime.Now, eIncidencias.IGW_CAIDA, 
                            eTiposInci.TEH_TIFX, gw.name, 
                            Params(idiomas.strings.GW_GLOBAL_MODULE));
                        break;
                    case GwGlobalTransitions.ToActiveNoError:
                        RecordEvent<GwExplorer>(
                            DateTime.Now, eIncidencias.IGW_ENTRADA,
                            eTiposInci.TEH_TIFX, gw.name,
                            Params(idiomas.strings.GW_GLOBAL_MODULE + " sin Errores."));
                        break;
                    case GwGlobalTransitions.ToActiveError:
                        RecordEvent<GwExplorer>(
                            DateTime.Now, eIncidencias.IGW_ENTRADA, 
                            eTiposInci.TEH_TIFX, gw.name, 
                            Params(idiomas.strings.GW_GLOBAL_MODULE + " con Errores."));
                        break;
                }
            });
        }

        void ExploraGw(object obj)
        {
            stdGw gw = (stdGw)obj;

            if (gw.gwA.IpConn.IsPollingTime() == true)
            {
                LogTrace<GwExplorer>($"POLL Executed: {gw.gwA.name}");
                ExplorePhGw(gw.gwA);
            }
            else
            {
                LogTrace<GwExplorer>($"POLL Skipped : {gw.gwA.name}");
            }

            if (gw.Dual)
            {
                if (gw.gwB.IpConn.IsPollingTime() == true)
                {
                    LogTrace<GwExplorer>($"POLL Executed: {gw.gwB.name}");
                    ExplorePhGw(gw.gwB);
                }
                else
                {
                    LogTrace<GwExplorer>($"POLL Skipped : {gw.gwB.name}");
                }
            }

            /** Actualiza los Parametros Globales de la Pasarela */
            GwActualizaEstado(gw);

        }
        void ExplorePhGw(object obj)
        {
            // Obtengo una copia del estado de la pasarela.
            stdPhGw phgw = new stdPhGw((stdPhGw)obj);
            phgw.events = new Queue<object>();

            try
            {
                var resPing = PingS.Ping(phgw.ip, phgw.presente).Result;
                if (phgw.IpConn.ProcessResult(resPing))
                {
                    phgw.IpConn.Std = resPing ? std.Ok : std.NoInfo;
                    LogTrace<GwExplorer>($"GwPing {(resPing ? "Ok  " : "Fail")} executed: {phgw.name}.");
                    if (phgw.IpConn.Std == std.Ok)
                    {
                        // Supervision del Modulo SIP...
                        SipModuleTest(phgw, (res, newStd) =>
                        {
                            if (phgw.SipMod.ProcessResult(res) == true)
                            {
                                phgw.SipMod.Std = newStd;
                                LogTrace<GwExplorer>($"GwSip_ {(res ? "Ok  " : "Fail")} executed: {phgw.name}.");
                            }
                            else
                            {
                                LogWarn<GwExplorer>($"GwSip_ Fail ignored : {phgw.name}.");
                            }
                        });

                        if (phgw.SipMod.Std == std.Ok)
                        {
                            // Supervision del Modulo de Configuracion...
                            CfgModuleTest(phgw, (res, newStd, mensaje) =>
                            {
                                if (phgw.CfgMod.ProcessResult(res) == true)
                                {
                                    phgw.CfgMod.Std = newStd;
                                    if (res)
                                        LogTrace<GwExplorer>($"GwCfg_ Ok  executed: {phgw.name}.");
                                    else
                                    {
                                        LogTrace<GwExplorer>($"GwCfg_ Fail EXECUTED: {phgw.name}\n   <{mensaje}>.");
                                    }
                                }
                                else
                                {
                                    LogTrace<GwExplorer>($"GwCfg_ Fail IGNORED: {phgw.name}\n   <{mensaje}>.");
                                }
                            });

                            // Supervision del Modulo SNMP....
                            SnmpModuleExplore(phgw, (res) =>
                            {
                                if (phgw.SnmpMod.ProcessResult(res) == true)
                                {
                                    if (res == true)
                                    {
                                        phgw.SnmpMod.Std = std.Ok;
                                    }
                                    else
                                    {
                                        phgw.SnmpMod.Std = std.NoInfo;
                                        phgw.SnmpDataReset();
                                    }
                                    LogTrace<GwExplorer>($"GwSnmp {(res ? "Ok  " : "Fail")} executed: {phgw.name}.");
                                }
                                else
                                {
                                    LogWarn<GwExplorer>($"GwSnmp Fail ignored : {phgw.name}.");
                                }
                            });
                        }
                        else
                        {
                            // No se ha Respondido al SIP...
                        }
                    }
                    else
                    {
                        // No se ha respondido al PING...
                    }
                }
                else
                {
                    LogWarn<GwExplorer>($"GwPing Fail ignored : {phgw.name}.");
                }
            }
            catch (Exception x)
            {
                LogException<GwExplorer>($"In ({phgw.name}, {phgw.ip}) Exception when Exploring", x);
                if (phgw.IpConn.ProcessResult(false) == true)
                {
                    phgw.IpConn.Std = std.NoInfo;
                }
            }
            finally
            {
                ConsolidateData((stdPhGw)obj, phgw);
                phgw.events.Clear();
            }
        }
        void SipModuleTest(stdPhGw phgw, Action<bool, std> response)
        {
            try
            {
                int timeout = Properties.u5kManServer.Default.SipOptionsTimeout;
                SipUA locale_ua = new SipUA() { user = "MTTO", ip = Properties.u5kManServer.Default.MiDireccionIP, port = 0 };
                SipUA remote_ua = new SipUA() { user = phgw.name, ip = phgw.ip, port = 5060, radio = true };
                SipSupervisor sips = new SipSupervisor(locale_ua, timeout);
                if (sips.SipPing(remote_ua))
                {
                    if (remote_ua.last_response == null || remote_ua.last_response.Result == "Error")
                    {
                        response(false, std.NoInfo);
                    }
                    else
                    {
                        response(true, (remote_ua.last_response.Result == "200" || remote_ua.last_response.Result == "503") ? std.Ok : std.Error);
                    }
                }
                else
                {
                    /** No Responde */
                    response(false, std.NoInfo);
                }
            }
            catch (Exception x)
            {
                // Error en los OPTIONS...
                LogException<GwExplorer>($"In ({phgw.name}, {phgw.ip}) Exception when SipModuleTest", x);
                response(false, std.NoInfo);
            }
        }
        void CfgModuleTest(stdPhGw phgw, Action<bool, std, string> response)
        {
            std stdRes = std.NoInfo;
            var mensaje = "";
            try
            {
                string page = "http://" + phgw.ip + ":8080/test";
                var timeout = TimeSpan.FromMilliseconds(Properties.u5kManServer.Default.HttpGetTimeout);
                var httpRes = HttpS.Get(page, timeout).Result;
                if (httpRes.Item1)
                {
                    stdRes = httpRes.Item2.Contains("Handler por Defecto") ? std.Ok : std.Error;
                }
                else
                {
                    stdRes = std.NoInfo;
                    mensaje = httpRes.Item2;
                }
                /** Obtiene la version unificada */
                if (stdRes == std.Ok && (phgw.version == string.Empty || phgw.version == idiomas.strings.GWS_VersionError))
                {
                    var versionPage = $"http://{phgw.ip}:8080/mant/lver";
                    httpRes = HttpS.Get(versionPage, timeout).Result;
                    phgw.version = httpRes.Item1 ? httpRes.Item2 : idiomas.strings.GWS_VersionError;
                }
                /** Obtiene la información NTP */
                if (stdRes == std.Ok)
                {
                    var ntpPage = $"http://{phgw.ip}:8080/ntpstatus";
                    httpRes = HttpS.Get(ntpPage, timeout).Result;
                    if (httpRes.Item1)
                    {
                        var status = (U5kManWebAppData.JDeserialize<stdGw.RemoteNtpClientStatus>(httpRes.Item2)).lines;
                        status = NormalizeNtpStatusList(status);
                        phgw.NtpInfo.Actualize(phgw.name, status);
                        LogTrace<GwExplorer>($"{phgw.name}, NtpInfo OUT     => <<{phgw.NtpInfo}>>");
                    }
                }
            }
            catch (Exception x)
            {
                // Error en Modulo de Configuracion Local...
                stdRes = std.NoInfo;
                LogException<GwExplorer>($"In ({phgw.name}, {phgw.ip}) Exception when CfgModuleTest", x);
            }
            finally
            {
                //GetVersion_unificada(phgw);
            }
            response(stdRes != std.NoInfo, stdRes, mensaje);
        }
        void SnmpModuleExplore(stdPhGw phgw, Action<bool> response)
        {
            try
            {
#if !_EXPLORE_ALL_AT_ONCE_
                SnmpExploraGwStdGen(phgw);
                for (int slot = 0; slot < 4; slot++)
                {
                    SnmpExploraSlot(new KeyValuePair<stdPhGw, int>(phgw, slot));
                }
#else
                ExploreEverythingAtOnce(phgw);
#endif
                response(true);
            }
            catch (Exception x)
            {
                // Error en la Exploracion SNMP....
                response(false);
                LogException<GwExplorer>($"In ({phgw.name}, {phgw.ip}) Exception when SnmpModuleTest", x);
            }
        }
        void ConsolidateData(stdPhGw last, stdPhGw current)
        {
            try
            {
                // Calcular Presente....
                current.presente = (current.IpConn.Std == std.Ok && current.SipMod.Std != std.NoInfo);
                // Calcular Estado General.... En current.std se encuentra el estado leido. last.std debe tener tambien los errores de recurso...
                current.std = current.presente == false ? std.NoInfo : current.Errores == true ? std.Error : std.Ok;

                // Historicos de Activacion / Desactivacion de Modulos...
                if (current.CfgMod.Std != last.CfgMod.Std)
                {
                    eIncidencias inci = current.CfgMod.Std == std.NoInfo ? eIncidencias.IGW_CAIDA : eIncidencias.IGW_ENTRADA;
                    RecordEvent<GwExplorer>(DateTime.Now, inci, eTiposInci.TEH_TIFX, current.name, Params(idiomas.strings.GW_CFGL_MODULE));
                }
                if (current.SipMod.Std != last.SipMod.Std)
                {
                    eIncidencias inci = current.SipMod.Std == std.NoInfo ? eIncidencias.IGW_CAIDA : eIncidencias.IGW_ENTRADA;
                    RecordEvent<GwExplorer>(DateTime.Now, inci, eTiposInci.TEH_TIFX, current.name, Params(idiomas.strings.GW_SIP_MODULE));
                }
                if (current.SnmpMod.Std != last.SnmpMod.Std)
                {
                    eIncidencias inci = current.SnmpMod.Std == std.NoInfo ? eIncidencias.IGW_CAIDA : eIncidencias.IGW_ENTRADA;
                    RecordEvent<GwExplorer>(DateTime.Now, inci, eTiposInci.TEH_TIFX, current.name, Params(idiomas.strings.GW_SNMP_MODULE));
                }
                /** Habilita el registro de los eventos surgidos en los Pollings */
                PopEvents(current);

                // Genera historicos de activacion / desactivacion de la pasarela...
                if (current.presente != last.presente)
                {
                    var who = idiomas.strings.GW_CPU_MODULE + (current.Seleccionada ? " Activa " : " Reserva ");
                    eIncidencias inci = current.presente == false ? eIncidencias.IGW_CAIDA : eIncidencias.IGW_ENTRADA;
                    RecordEvent<GwExplorer>(DateTime.Now, inci, eTiposInci.TEH_TIFX, current.name, Params(who));

                    if (current.presente == false)
                    {
                        /** 20200811 Reset de los estados de Módulos */
                        current.SipMod.Std = std.NoInfo;
                        current.CfgMod.Std = std.NoInfo;
                        current.SnmpMod.Std = std.NoInfo;

                        /** Reset Estado GW fisica */
                        GwHelper.SetToOutOfOrder(current);
                    }
                }
            }
            catch (Exception x)
            {
                // Error en la consolidacion.
                LogException<GwExplorer>($"In ({last.name}, {last.ip}) Exception when ConsolidateData", x);
            }
            finally
            {
                last.CopyFrom(current);
            }
        }
        void SnmpExploraGwStdGen(stdPhGw pgw)
        {
            IPEndPoint gwep = new IPEndPoint(IPAddress.Parse(pgw.ip), pgw.snmpport);
            OctetString community = new OctetString("public");
            List<Variable> vIn = new List<Variable>()
            {
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.2.0")),   // Estado Hw.
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.6.0")),   // Estado LAN1
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.7.0")),   // Estado LAN2
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.8.0")),   // Estado P/R,
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.4.0")),   // Estado FA,
                new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.1.0")),   // Identificador. Habilita el envio de TRAPS
            };
            IList<Variable> vOut = SnmpS.GetData(pgw, vIn).Result;
            // estadoGeneral. 0: No Inicializado, 1: Ok, 2: Fallo, 3: Aviso.
            int stdGeneral = SnmpS.ToInt(vOut[0].Data);
            // stdLAN1. 0: No Presente, 1: Ok, 2: Error.
            int stdLan1 = SnmpS.ToInt(vOut[1].Data);
            // stdLAN2. 0: No Presente, 1: Ok, 2: Error.
            int stdLan2 = SnmpS.ToInt(vOut[2].Data);
            // stdCpuLocal. 0: No Presente. 1: Principal, 2: Reserva, 3: Arrancando
            int stdPR = SnmpS.ToInt(vOut[3].Data);
            // stdFA. 0: No Presente. 1: Ok, 2: Error
            int stdFA = SnmpS.ToInt(vOut[4].Data);
            pgw.std = stdGeneral == 0 ? std.NoInfo : stdGeneral == 1 ? std.Ok : std.Error;

            int stdLan = (stdLan1 == 1 ? 0x01 : 0x00) | (stdLan2 == 1 ? 0x02 : 0x00);

            PhGwLanStatusSet(pgw, (0x04 | stdLan));                 // En este tipo de Pasarelas BOND configurado...
            PhGwPrincipalReservaSet(pgw, stdPR == 1 ? 1 : 0);       // Solo se marca PPAL si está en PPAL en cualquier otro caso se marca RSVA

            pgw.stdFA = stdFA == 0 ? std.NoInfo : stdFA == 1 ? std.Ok : stdFA == 2 ? std.Error : std.NoExiste;
        }
        void SnmpExploraSlot(object obj)
        {
            KeyValuePair<stdPhGw, int> objIn = (KeyValuePair<stdPhGw, int>)obj;
            stdPhGw gw = objIn.Key;
            int nslot = objIn.Value;
            stdSlot slot = gw.slots[nslot];
            IPEndPoint gwep = new IPEndPoint(IPAddress.Parse(gw.ip), gw.snmpport);
            OctetString community = new OctetString("public");
            try
            {
                string oidbase = ".1.3.6.1.4.1.7916.8.3.1.3.2.1.";
                List<Variable> vIn = new List<Variable>()
                {
                    new Variable(new ObjectIdentifier(oidbase+"2."+(nslot+1).ToString())),   // Tipo. 0: Error, 1: IA4, 2: IQ1
                    new Variable(new ObjectIdentifier(oidbase+"3."+(nslot+1).ToString())),   // Status,
                    new Variable(new ObjectIdentifier(oidbase+"4."+(nslot+1).ToString())),   // Canal-0
                    new Variable(new ObjectIdentifier(oidbase+"5."+(nslot+1).ToString())),   // Canal-1
                    new Variable(new ObjectIdentifier(oidbase+"6."+(nslot+1).ToString())),   // Canal-2
                    new Variable(new ObjectIdentifier(oidbase+"7."+(nslot+1).ToString()))    // Canal-3
                };

                IList<Variable> vOut = SnmpS.GetData(gw, vIn).Result;
                int stipo = SnmpS.ToInt(vOut[0].Data);                            // 0: Error, 1: IA4, 2: IQ1
                int status = SnmpS.ToInt(vOut[1].Data);                           // 0: No presente, 1: Presente

                stipo = status == 0 ? 0 : (stipo == 1 ? 2 : 0);

                int can0 = SnmpS.ToInt(vOut[2].Data);                             // 0: Desconectada. 1: Conectada
                int can1 = SnmpS.ToInt(vOut[3].Data);                             // 0: Desconectada. 1: Conectada
                int can2 = SnmpS.ToInt(vOut[4].Data);                             // 0: Desconectada. 1: Conectada
                int can3 = SnmpS.ToInt(vOut[5].Data);                             // 0: Desconectada. 1: Conectada

                int std = (can0 << 1) | (can1 << 2) | (can2 << 3) | (can3 << 4);

                SlotTypeSet(gw, nslot, gw.slots[nslot], stipo, std);
                SlotStateSet(gw, nslot, gw.slots[nslot], std);

                for (int rec = 0; rec < 4; rec++)
                {
                    if (slot.rec[rec].presente == true)
                    {
                        SnmpExploraRecurso(new KeyValuePair<stdPhGw, int>(gw, nslot * 4 + rec));
                    }
                    else
                    {
                        Reset_ExploraRecurso(gw, nslot, rec);
                    }
                }
            }
            catch (Exception x)
            {
                    LogException<GwExplorer>(String.Format(" Explorando Slot. CGW {0}.{1}",
                    obj == null ? "null" : ((KeyValuePair<stdPhGw, int>)obj).Key.ip,
                    obj == null ? "null" : ((KeyValuePair<stdPhGw, int>)obj).Value.ToString()), x);
            }
        }
        void SnmpExploraRecurso(object obj)
        {
            KeyValuePair<stdPhGw, int> objIn = (KeyValuePair<stdPhGw, int>)obj;
            stdPhGw gw = objIn.Key;
            int nres = objIn.Value;
            int nslot = nres / 4;
            int ires = nres % 4;
            stdRec rec = gw.slots[nslot].rec[ires];
            IPEndPoint gwep = new IPEndPoint(IPAddress.Parse(gw.ip), gw.snmpport);
            OctetString community = new OctetString("public");

            if (gw.name == "" && nslot == 0 && ires == 1)
                LogTrace<GwExplorer>(String.Format("Presencia (1) Slot 0, Recurso 1: {0}", gw.slots[0].rec[1].presente));

            try
            {
                string oidbase = ".1.3.6.1.4.1.7916.8.3.1.4.2.1.";
                List<Variable> vIn = new List<Variable>()
                {
                    new Variable(new ObjectIdentifier(oidbase+"3."+(nres+1).ToString())),   // Tipo
                    new Variable(new ObjectIdentifier(oidbase+"6."+(nres+1).ToString())),   // Status Hardware,
                    new Variable(new ObjectIdentifier(oidbase+"15."+(nres+1).ToString())),  // Status Interfaz.
                };

                IList<Variable> vOut = SnmpS.GetData(gw, vIn).Result;

                int ntipo = SnmpS.ToInt(vOut[0].Data);   // 0: RD, 1: LC, 2: BC, 3: BL, 4: AB, 5: R2, 6: N5, 7: QS, 9: NP, 13: PPEM 
                if (ntipo == 9)
                {
                    // 20170630. El código 9 no es no presente sino NO CONFIGURADO
                    // Reset_ExploraRecurso(gw, nslot, ires);
                    // rec.presente = false;
                    rec.tipo_itf = itf.rcNotipo;
                    rec.tipo_online = trc.rcNotipo;
                    rec.std_online = std.NoInfo;
                }
                else if ((ntipo >= 0 && ntipo < 9) || ntipo == 13)
                {
                    int AgentType = NotifiedAgentType(ntipo);

                    SlotRecursoTipoAgenteSet(gw, rec, AgentType);
                    /*
                            rcRadio = 0, 
                            rcLCE = 1, 
                            rcPpBC = 2, 
                            rcPpBL = 3, 
                            rcPpAB = 4, 
                            rcAtsR2 = 5, 
                            rcAtsN5 = 6, 
                            rcPpEM = 13, 
                            rcPpEMM = 51, 
                            rcNotipo = -1 
                     * */
                    SlotRecursoTipoInterfazSet(gw, rec, ntipo);

                    int estado = SnmpS.ToInt(vOut[2].Data);   // 0: NP, 1: OK, 2: Fallo, 3: Degradado
                    SlotRecursoEstadoSet(gw, rec, estado, (trc)AgentType);
                }
                else if (ntipo != 9 && ntipo != -1)
                {
                    LogWarn<GwExplorer>(String.Format("Error Explorando Recurso {0}:{1}: Tipo Notificado <{2}> Erroneo.",
                                gw.ip, nres, ntipo));
                }
            }
            catch (Exception x)
            {
                LogException<GwExplorer>(String.Format(" Explorando recurso en {0}: Rec:{1}-{2}", gw.ip, nres, rec.name), x);
            }
        }
        void SnmpTrapReceived(object from, TrapBus.TrapEventArgs args)
        {
            LogTrace<GwExplorer>($"Trap Received => {args}");
            if (DataS.IsMaster == true)
            {
                GlobalServices.GetWriteAccess(() =>
                {
                    // Busco si es una Pasarela.
                    var ipfrom = args.From?.Address.ToString();
                    var gw = DataS.Data.STDGWS
                        .Where(g => g.ip == ipfrom || g.gwA.ip == ipfrom || g.gwB.ip == ipfrom)
                        .FirstOrDefault();
                    if (gw != null)
                    {
                        LogInfo<GwExplorer>($"GW Trap Received => {args}");
                        var pgw = gw.gwA.ip == ipfrom ? gw.gwA : gw.gwB;
                        ProcessTrap(gw, pgw, args.TrapOid, args.VarOid, args.VarData);
                    }
                });
            }
        }
        void ProcessTrap(stdGw gw, stdPhGw pgw, string oidEnt, string oidvar, ISnmpData data)
        {
            switch (oidEnt)
            {
                case ".1.3.6.1.4.1.7916.8.3.2.1.1":         // Cambio de Configuracion.
                    break;

                case ".1.3.6.1.4.1.7916.8.3.2.1.2":         // Cambio de Estado.
                    break;

                case ".1.3.6.1.4.1.7916.8.3.2.1.3":         // Se genera cuando cambia un parametro del grupo tarjeta
                    break;

                case ".1.3.6.1.4.1.7916.8.3.2.1.4":         // Se genera cuando cambia parametro del grupo interfaz
                    break;

                case ".1.3.6.1.4.1.7916.8.3.2.1.5":         // Evento de Historicos.
                    if (oidvar == ".1.3.6.1.4.1.7916.8.3.2.1.7.0")
                    {
                        LogTrace<GwExplorer>(String.Format("GWU-HISTORICO: <<<{0}>>>", data.ToString()));

                        using (var hist = new Redan2UlisesHist(data.ToString()))
                        {
                            hist.UlisesInci((ok, date, inci, parametros) =>
                            {
                                if (ok)
                                {
                                    var settings = Properties.u5kManServer.Default;
                                    var workingDate = settings.GwsDatesAreUtc ? date.ToLocalTime() : date;
                                    var deviation = DateTime.Now - workingDate;

                                    if (deviation < TimeSpan.FromSeconds(-settings.GwsHistMaxSecondsInAdvance) ||
                                        deviation > TimeSpan.FromHours(settings.GwsHistMaxHoursDelayed))
                                    {
                                        var msg = $" Historico fuera de sincronismo: GW => [{pgw.name},{pgw.ip}], " +
                                            $"GW UTC date => {date}, Local date => {DateTime.Now}, " +
                                            $"Inci => {inci}";
                                        LogWarn<GwExplorer>(msg);
                                        RecordEvent<GwExplorer>(DateTime.Now,
                                            eIncidencias.IGRL_U5KI_SERVICE_ERROR,
                                            eTiposInci.TEH_SISTEMA, "SPV",
                                            new Object[] { "Supervision Pasarelas", msg });
                                    }
                                    else
                                    {
                                        RecordEvent<GwExplorer>(workingDate, (eIncidencias)inci.id, (eTiposInci)inci.tipo, inci.idhw, parametros.ToArray());
                                    }
                                }
                                else
                                    LogWarn<GwExplorer>(String.Format("GWU-HISTORICO NO CONVERTIDO: <<<{0}>>>", data.ToString()));
                            });
                        }
                    }
                    break;

                default:
                    LogWarn<GwExplorer>(String.Format("Recibido TRAP-GW OID-Desconocida de {0}, OID={1}", gw?.ip, oidEnt));
                    break;
            }
        }
#if _EXPLORE_ALL_AT_ONCE_

        /** 20200813. Version para solo generar un GET */
        List<Variable> vInAll = new List<Variable>()
        {
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.2.0")),    // 0 => Estado Hw.
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.6.0")),    // 1 => Estado LAN1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.7.0")),    // 2 => Estado LAN2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.8.0")),    // 3 => Estado P/R,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.4.0")),    // 4 => Estado FA,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.1.1.0")),    // 5 => Identificador. Habilita el envio de TRAPS
                                                                                    // 6 => Slot 0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.1")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.1")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.1")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.1")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.1")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.1")),   // Canal-3
                                                                                    // 12 => Slot 1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.2")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.2")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.2")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.2")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.2")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.2")),   // Canal-3
                                                                                    // 18 => Slot 2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.3")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.3")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.3")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.3")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.3")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.3")),   // Canal-3
                                                                                    // 24 => Slot 3
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.2.4")),   // Tipo. 0: Error, 1: IA4, 2: IQ1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.3.4")),   // Status,
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.4.4")),   // Canal-0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.5.4")),   // Canal-1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.6.4")),   // Canal-2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.3.2.1.7.4")),   // Canal-3
                                                                                    // 30 => Recurso 0
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.1")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.1")),  // Status Interfaz.
                                                                                    // 32 => Recurso 1
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.2")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.2")),  // Status Interfaz.
                                                                                    // 34 => Recurso 2
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.3")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.3")),  // Status Interfaz.
                                                                                    // 36 => Recurso 3
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.4")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.4")),  // Status Interfaz.
                                                                                    // 38 => Recurso 4
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.5")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.5")),  // Status Interfaz.
                                                                                    // 40 => Recurso 5
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.6")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.6")),  // Status Interfaz.
                                                                                    // 42 => Recurso 6
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.7")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.7")),  // Status Interfaz.
                                                                                    // 44 => Recurso 7
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.8")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.8")),  // Status Interfaz.
                                                                                    // 46 => Recurso 8
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.9")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.9")),  // Status Interfaz.
                                                                                    // 48 => Recurso 9
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.10")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.10")),  // Status Interfaz.
                                                                                    // 50 => Recurso 10
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.11")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.11")),  // Status Interfaz.
                                                                                    // 52 => Recurso 11
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.12")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.12")),  // Status Interfaz.
                                                                                    // 54 => Recurso 12
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.13")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.13")),  // Status Interfaz.
                                                                                    // 56 => Recurso 13
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.14")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.14")),  // Status Interfaz.
                                                                                    // 58 => Recurso 14
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.15")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.15")),  // Status Interfaz.
                                                                                    // 60 => Recurso 15
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.3.16")),   // Tipo
            new Variable(new ObjectIdentifier(".1.3.6.1.4.1.7916.8.3.1.4.2.1.15.16")),  // Status Interfaz.
        };
        void ExploreEverythingAtOnce(stdPhGw pgw)
        {
            IPEndPoint gwep = new IPEndPoint(IPAddress.Parse(pgw.ip), pgw.snmpport);
            OctetString community = new OctetString("public");
            SnmpClient snmpc = new SnmpClient();

            IList<Variable> vOut = snmpc.Get(VersionCode.V2, gwep, community, vInAll, pgw.SnmpTimeout, pgw.SnmpReintentos);

            // Análisis de Parámetros Generales.
            // estadoGeneral. 0: No Inicializado, 1: Ok, 2: Fallo, 3: Aviso.
            int stdGeneral = snmpc.Integer(vOut[0].Data);
            // stdLAN1. 0: No Presente, 1: Ok, 2: Error.
            int stdLan1 = snmpc.Integer(vOut[1].Data);
            // stdLAN2. 0: No Presente, 1: Ok, 2: Error.
            int stdLan2 = snmpc.Integer(vOut[2].Data);
            // stdCpuLocal. 0: No Presente. 1: Principal, 2: Reserva, 3: Arrancando
            int stdPR = snmpc.Integer(vOut[3].Data);
            // stdFA. 0: No Presente. 1: Ok, 2: Error
            int stdFA = snmpc.Integer(vOut[4].Data);
            pgw.std = stdGeneral == 0 ? std.NoInfo : stdGeneral == 1 ? std.Ok : std.Error;

            int stdLan = (stdLan1 == 1 ? 0x01 : 0x00) | (stdLan2 == 1 ? 0x02 : 0x00);
            PhGwLanStatusSet(pgw, (0x04 | stdLan));                 // En este tipo de Pasarelas BOND configurado...

            PhGwPrincipalReservaSet(pgw, stdPR == 1 ? 1 : 0);       // Solo se marca PPAL si está en PPAL en cualquier otro caso se marca RSVA

            pgw.stdFA = stdFA == 0 ? std.NoInfo : stdFA == 1 ? std.Ok : stdFA == 2 ? std.Error : std.NoExiste;

            // Análisis de Slots
            for (int slot = 0; slot<4; slot++)
            {
                int ibase = 6 + slot * 6;
                int stipo = snmpc.Integer(vOut[ibase+0].Data);                            // 0: Error, 1: IA4, 2: IQ1
                int status = snmpc.Integer(vOut[ibase+1].Data);                           // 0: No presente, 1: Presente

                stipo = status == 0 ? 0 : (stipo == 1 ? 2 : 0);

                int can0 = snmpc.Integer(vOut[ibase + 2].Data);                             // 0: Desconectada. 1: Conectada
                int can1 = snmpc.Integer(vOut[ibase + 3].Data);                             // 0: Desconectada. 1: Conectada
                int can2 = snmpc.Integer(vOut[ibase + 4].Data);                             // 0: Desconectada. 1: Conectada
                int can3 = snmpc.Integer(vOut[ibase + 5].Data);                             // 0: Desconectada. 1: Conectada

                int std = (can0 << 1) | (can1 << 2) | (can2 << 3) | (can3 << 4);

                SlotTypeSet(pgw, slot, pgw.slots[slot], stipo, std);
                SlotStateSet(pgw, slot, pgw.slots[slot], std);
            }

            // Análisis de Recursos.
            for (int nres=0; nres<16; nres++)
            {
                int ibase = 30 + nres * 2;
                int nslot = nres / 4;
                int ires = nres % 4;
                stdRec rec = pgw.slots[nslot].rec[ires];
                int ntipo = snmpc.Integer(vOut[ibase+0].Data);   // 0: RD, 1: LC, 2: BC, 3: BL, 4: AB, 5: R2, 6: N5, 7: QS, 9: NP, 13: PPEM 
                if (ntipo == 9)
                {
                    // 20170630. El código 9 no es no presente sino NO CONFIGURADO
                    // Reset_ExploraRecurso(gw, nslot, ires);
                    // rec.presente = false;
                    rec.tipo_itf = itf.rcNotipo;
                    rec.tipo_online = trc.rcNotipo;
                    rec.std_online = std.NoInfo;
                }
                else if ((ntipo >= 0 && ntipo < 9) || ntipo == 13)
                {
                    int TipoNotificado = ntipo == 0 ? RadioResource_AgentType :
                        ntipo == 1 ? IntercommResource_AgentType :
                        (ntipo < 5 || ntipo == 13) ? LegacyPhoneResource_AgentType : ATSPhoneResource_AgentType;

                    SlotRecursoTipoAgenteSet(pgw, rec, TipoNotificado);
                    /*
                            rcRadio = 0, 
                            rcLCE = 1, 
                            rcPpBC = 2, 
                            rcPpBL = 3, 
                            rcPpAB = 4, 
                            rcAtsR2 = 5, 
                            rcAtsN5 = 6, 
                            rcPpEM = 13, 
                            rcPpEMM = 51, 
                            rcNotipo = -1 
                     * */
                    SlotRecursoTipoInterfazSet(pgw, rec, ntipo);

                    int estado = snmpc.Integer(vOut[ibase+1].Data);   // 0: NP, 1: OK, 2: Fallo, 3: Degradado
                    SlotRecursoEstadoSet(pgw, rec, estado, (trc)TipoNotificado);
                }
                else if (ntipo != 9 && ntipo != -1)
                {
                    LogWarn<GwExplorer>(String.Format("Error Explorando Recurso {0}:{1}: Tipo Notificado <{2}> Erroneo.",
                                pgw.ip, nres, ntipo));
                }
            }
        }
#endif
        void Reset_ExploraRecurso(stdPhGw gw, int nslot, int rec)
        {
            SlotRecursoTipoAgenteSet(gw, gw.slots[nslot].rec[rec], (int)trc.rcNotipo);
            SlotRecursoTipoInterfazSet(gw, gw.slots[nslot].rec[rec], (int)itf.rcNotipo);
            SlotRecursoEstadoSet(gw, gw.slots[nslot].rec[rec], (int)std.NoInfo, trc.rcNotipo);
        }
        List<string> NormalizeNtpStatusList(List<string> input)
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

        void PushEvent(stdPhGw cpu, eIncidencias inci, eTiposInci thw, string idhw, object[] parametros,
            [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        {
            var itemEvent = new Tuple<eIncidencias, eTiposInci, string, object[], int, string>(inci, thw, idhw, parametros, lineNumber, caller);
            cpu.events.Enqueue(itemEvent);
        }
        void PopEvents(stdPhGw cpu)
        {
            while (cpu.events.Count() > 0)
            {
                var itemEvent = (Tuple<eIncidencias, eTiposInci, string, object[], int, string>)cpu.events.Dequeue();
                RecordEvent<GwExplorer>(DateTime.Now, itemEvent.Item1, itemEvent.Item2, itemEvent.Item3, itemEvent.Item4, itemEvent.Item5, itemEvent.Item6);
            }
        }
        int NotifiedAgentType(int ntipo)
        {
            int TipoNotificado = ntipo == 0 ? RadioResource_AgentType :
                ntipo == 1 ? IntercommResource_AgentType  :    
                (ntipo < 5 || ntipo == 13) ? LegacyPhoneResource_AgentType : ATSPhoneResource_AgentType;
            return TipoNotificado;
        }

    }   // clase

} // namespace.
