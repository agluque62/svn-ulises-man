using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;

using U5kBaseDatos;
using NucleoGeneric;

using Utilities;

namespace U5kManServer.WebAppServer
{
    public class U5kManWebAppData : BaseCode
    {
        static public string JSerialize<JObject>(JObject obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
        static public JObject JDeserialize<JObject>(string strData)
        {
            return JsonConvert.DeserializeObject<JObject>(strData);
        }
    }
    class U5kManWADResultado : U5kManWebAppData
    {
        public string res { get; set; }
    }
    public class U5kManWADInci : U5kManWebAppData
    {
        public class InciData
        {
            public string time { get; set; }
            public string inci { get; set; }
            public int id { get; set; }
        }
        public class InciRec
        {
            public string user { get; set; }
            public InciData inci { get; set; }
        }
        public List<InciData> lista = new List<InciData>();
        public int HashCode { get; set; }
        public string lang { get; set; }
        public U5kManWADInci(bool bGenerate = false)
        {
            if (bGenerate)
            {
                lock (U5kManService._last_inci)
                {
                    SafeExecute("listinc", () =>
                    {
                        foreach (U5kManLastInciList.eListaInci inci in U5kManService._last_inci._lista)
                        {
                            lista.Add(new InciData()
                            {
                                time = inci._fecha.ToString(),
                                inci = /*EncryptionHelper.CAE_cifrar(inci._desc)*/inci._desc,
                                id = (int)inci._id
                            });
                        }
                        // La Lista ya debe estar ordenada...
                        // HashCode = lista.GetHashCode();
                        HashCode = HashCodeGet();
                        lang = U5kManService.cfgSettings/*Properties.u5kManServer.Default*/.Idioma;
                    });
                }
            }
        }
        private int HashCodeGet()
        {
            int hash = 0;
            foreach (InciData item in lista)
            {
                hash += item.time.GetHashCode();
                hash += item.inci.GetHashCode();
            }
            return hash;
        }
        static public void Reconoce(string jStrInci)
        {
            SafeExecute("inci_ack", () =>
            {
                InciRec inci = JDeserialize<InciRec>(jStrInci);
                U5kManService.stReconoceAlarma(inci.user, DateTime.Parse(inci.inci.time), inci.inci.inci);
            });
        }
    }
    class U5kManWADStd : U5kManWebAppData
    {
        public class itemData
        {
            public string name { get; set; }
            public int enable { get; set; }
            public int std { get; set; }
            public int sel { get; set; }
            public string url { get; set; }
            public string ntp { get; set; }
        }
        public string version { get; set; }
        public string cfg { get; set; }
        public int hf { get; set; }
        public int recw { get; set; }
        public itemData sv1 { get; set; }
        public itemData sv2 { get; set; }
        public itemData cwp { get; set; }
        public itemData gws { get; set; }
        public itemData pbx { get; set; }
        public itemData ntp { get; set; }
        public bool sactaservicerunning { get; set; }
        public bool sactaserviceenabled { get; set; }
        public itemData sct1 { get; set;}
        public itemData sct2 { get; set; }
        public itemData ext { get; set; }
        public dynamic csi { get; set; }
        public string lang { get; set; }
        public int rd_status { get; set; }
        public int tf_status { get; set; }
        public string igmp_status { get; set; }
        public U5kManWADStd(U5kManStdData gdt, string user = "false", bool bGenerate = false)
        {
            if (bGenerate)
            {
                U5KStdGeneral stdg = gdt.STDG;

                version = SafeExecute<string>("version", () => U5kGenericos.Version);
                cfg = SafeExecute<string>("cfg", () => stdg.CfgId);
                hf = SafeExecute<int>("hf", () => U5kManService.cfgSettings.HayAltavozHF ? 1 : 0);
                recw = SafeExecute<int>("recw", () => U5kManService.cfgSettings.OpcOpeCableGrabacion ? 1 : 0);

                sv1 = SafeExecute<itemData>("sv1", () => new itemData()
                {
                    name = stdg.stdServ1.name,
                    enable = 1,
                    std = (int)stdg.stdServ1.Estado,
                    sel = (int)stdg.stdServ1.Seleccionado,
                    url = U5kManService.cfgSettings/*Properties.u5kManServer.Default*/.ServidorDual ?
                            String.Format("http://{0}/UlisesV5000/U5kCfg/Cluster/Default.aspx", U5kManServer.Properties.u5kManServer.Default.MySqlServer) : "",
                    ntp = SafeExecute<string>("sv1.ntp", () => stdg.stdServ1.NtpInfo.GlobalStatus)
                });
                sv2 = SafeExecute<itemData>("sv2", () => new itemData()
                {
                    name = stdg.stdServ2.name,
                    enable = stdg.bDualServ ? 1 : 0,
                    std = (int)stdg.stdServ2.Estado,
                    sel = (int)stdg.stdServ2.Seleccionado,
                    url = U5kManService.cfgSettings/*Properties.u5kManServer.Default*/.ServidorDual ?
                            String.Format("http://{0}/UlisesV5000/U5kCfg/Cluster/Default.aspx", U5kManServer.Properties.u5kManServer.Default.MySqlServer) : "",
                    ntp = SafeExecute<string>("sv2.ntp", () => stdg.stdServ2.NtpInfo.GlobalStatus)
                });

                cwp = SafeExecute<itemData>("cwp", () => new itemData()
                {
                    name = idiomas.strings.WAP_MSG_003 /* "Puestos de Operador"*/,
                    enable = 1,
                    std = (int)stdg.stdGlobalPos,
                    sel = 0,
                    url = ""
                });

                gws = SafeExecute<itemData>("gws", () => new itemData()
                {
                    name = idiomas.strings.WAP_MSG_004 /* "Pasarelas"*/,
                    enable = 1,
                    std = (int)stdg.stdScv1.Estado,
                    sel = 0,
                    url = ""
                });

                SafeExecute("nbx", () =>
                {
                    Services.CentralServicesMonitor.Monitor.DataGetForWebServer((csid) =>
                    {
                        csi = csid;
                    });
                });

                pbx = SafeExecute<itemData>("pbx", () => new itemData()
                {
                    name = stdg.stdPabx.name,
                    enable = stdg.HayPbx ? 1 : 0,
                    std = (int)stdg.stdPabx.Estado,
                    sel = 0,
                    url = U5kGenericos.PabxUrl(stdg.stdPabx.name)
                });

                ntp = SafeExecute<itemData>("ntp", () => new itemData()
                {
                    name = stdg.stdClock.name,
                    enable = stdg.HayReloj ? 1 : 0,
                    std = (int)stdg.stdClock.Estado,
                    sel = 0,
                    url = ""
                });

                sactaservicerunning = SafeExecute<bool>("sactaservicerunning", () => stdg.SactaService == std.Ok);
                sactaserviceenabled = SafeExecute<bool>("sactaserviceenabled", () => stdg.SactaServiceEnabled);
                sct1 = SafeExecute<itemData>("sct1", () => new itemData()
                {
                    name = "SACTA-1",
                    enable = stdg.HaySacta ? 1 : 0,
                    std = (int)stdg.stdSacta1,
                    sel = 0,
                    url = ""
                });

                sct2 = SafeExecute<itemData>("sct2", () => new itemData()
                {
                    name = "SACTA-2",
                    enable = stdg.HaySacta ? 1 : 0,
                    std = (int)stdg.stdSacta2,
                    sel = 0,
                    url = ""
                });

                ext = SafeExecute<itemData>("ext", () => new itemData()
                {
                    name = idiomas.strings.EquiposExternos,
                    enable = 1,
                    std = (int)stdg.stdGlobalExt,
                    sel = 0,
                    url = ""
                });

                lang = SafeExecute<string>("lang", () => U5kManService.cfgSettings/*Properties.u5kManServer.Default*/.Idioma);
                rd_status = SafeExecute<int>("rd_status", () =>
                {
                    var rs = Services.CentralServicesMonitor.Monitor.GlobalRadioStatus;
                    return rs == std.NoInfo ? -1 : rs == std.Alarma ? 2 : rs == std.Aviso ? 1 : 0;
                });

                ///** 20181010. De los datos obtenemos el estado de emergencia */
                tf_status = SafeExecute<int>("tf_status", () =>
                {
                    var tfs = Services.CentralServicesMonitor.Monitor.GlobalPhoneStatus;
                    return tfs == std.Ok ? 0 /** OK */ : tfs == std.Aviso ? 1 /** DEG */ : 2 /** EMG */;
                });
                igmp_status = SafeExecute<string>("igmp_status", () => Services.IgmpMonitor.Status);
            }
        }
    }
    class U5kManWADCwps : U5kManWebAppData
    {
        public class CWPData
        {
            public string name { get; set; }
            public string ip { get; set; }
            public int std { get; set; }
            public int panel { get; set; }
            public int jack_exe { get; set; }
            public int jack_ayu { get; set; }
            public int alt_r { get; set; }
            public int alt_t { get; set; }
            public int lan1 { get; set; }
            public int lan2 { get; set; }
            public int alt_hf { get; set; }
            public int rec_w { get; set; }
            public List<string> uris { get; set; }
            public string sect { get; set; }
            public string ntp { get; set; }
        }
        public List<CWPData> lista = new List<CWPData>();
        public U5kManWADCwps(U5kManStdData gdt, bool bGenerate)
        {
            if (bGenerate)
            {
                List<stdPos> stdpos = gdt.STDTOPS;
                foreach (stdPos pos in stdpos)
                {
                    SafeExecute($"CWP-{pos.name}", () =>
                    {
                        lista.Add(new CWPData()
                        {
                            name = pos.name,
                            ip = pos.ip,
                            std = (int)pos.stdg,
                            panel = pos.panel == std.Ok ? 1 : 0,
                            jack_exe = pos.jack_exe == std.Ok ? 1 : 0,
                            jack_ayu = pos.jack_ayu == std.Ok ? 1 : 0,
                            alt_r = pos.alt_r == std.Ok ? 1 : 0,
                            alt_t = pos.alt_t == std.Ok ? 1 : 0,
                            lan1 = pos.lan1 == std.Ok ? 1 : pos.lan1 == std.Error ? 2 : 0,
                            lan2 = pos.lan2 == std.Ok ? 1 : pos.lan2 == std.Error ? 2 : 0,
                            alt_hf = U5kManService.cfgSettings/*Properties.u5kManServer.Default*/.HayAltavozHF ? (pos.alt_hf == std.Ok ? 1 : 0) : -1,
                            rec_w = U5kManService.cfgSettings/*Properties.u5kManServer.Default*/.OpcOpeCableGrabacion ? (pos.rec_w == std.Ok ? 1 : 0) : -1,
                            uris = pos.uris,
                            sect = NormalizeSectId(pos.SectorOnPos, 16),
                            ntp = pos.NtpInfo.GlobalStatus
                        });
                    });
                }
            }
        }
        // Funcion Que limita el numero maximo de caracteres de una agrupacion a 16.
        String NormalizeSectId(String sectId, int longmax = 16)
        {
            String IdAgrupacion = sectId;
            int len = sectId.Length;
            if (len > longmax)
            {
                int mitad = longmax / 2;
                IdAgrupacion = sectId.Substring(0, mitad - 1) + ".." + sectId.Substring(len - (mitad - 1), mitad - 1);
            }
            return IdAgrupacion;
        }
    }
    class U5kManWADGws : U5kManWebAppData
    {
        public class GWData
        {
            public string name { get; set; }
            public string ip { get; set; }
            public int tipo { get; set; }
            public int std { get; set; }
            public int main { get; set; }
            public int lan1 { get; set; }
            public int lan2 { get; set; }
            public string ntp { get; set; }
            /** */
            public int cpu0 { get; set; }   // 0 NP, 1: Main, 2 Standby
            public int cpu1 { get; set; }   // 0 NP, 1: Main, 2 Standby
        };
        public List<GWData> lista = new List<GWData>();
        public int gdt { get; set; }
        public U5kManWADGws(U5kManStdData gdata, bool bGenerate = false)
        {
            if (bGenerate)
            {
                List<stdGw> stdgws = gdata.STDGWS;
                lista = stdgws
                    .Select(gw => SafeExecute<GWData>($"CGW-{gw.name}", () =>
                        new GWData()
                        {
                            name = gw.name,
                            ip = gw.ip,
                            tipo = gw.Dual ? 1 : 0,
                            std = (int)gw.std,
                            main = gw.Dual == false ? 0 : gw.gwA.Seleccionada ? 0 : gw.gwB.Seleccionada ? 1 : -1,
                            lan1 = (gw.Dual == false || gw.gwA.Seleccionada) ? (gw.gwA.lan1 == std.Ok ? 1 : 0) : (gw.gwB.lan1 == std.Ok ? 1 : 0),
                            lan2 = (gw.Dual == false || gw.gwA.Seleccionada) ? (gw.gwA.lan2 == std.Ok ? 1 : 0) : (gw.gwB.lan2 == std.Ok ? 1 : 0),
                            ntp = gw.cpu_activa.NtpInfo.GlobalStatus,
                            cpu0 = gw.Dual == false ? (gw.gwA.presente ? 1 : 0) : (gw.gwA.presente ? (gw.gwA.Seleccionada ? 1 : 2) : (0)),
                            cpu1 = gw.Dual == false ? (0) : (gw.gwB.presente ? (gw.gwB.Seleccionada ? 1 : 2) : (0))
                        })
                    )
                    .ToList();
                gdt = Properties.u5kManServer.Default.GatewaysDualityType;
            }
        }
    }
    class U5kManWADGwData : U5kManWebAppData
    {
        public class itemVersion
        {
            public string line { get; set; }
        }
        public class itemTar
        {
            public int cfg { get; set; }
            public int not { get; set; }
        }
        public class itemRec
        {
            public string name { get; set; }
            public int cfg { get; set; }
            public int not { get; set; }
            public int std { get; set; }
        }
        public class itemCpu
        {
            public string ip { get; set; }
            public string ntp { get; set; }
            public int lan1 { get; set; }
            public int lan2 { get; set; }
            public List<itemTar> tars { get; set; }
            public List<itemRec> recs = new List<itemRec>();
            public int sipMod { get; set; }
            public int snmpMod { get; set; }
            public int cfgMod { get; set; }
            public int fa { get; set; }
        }

        public string name { get; set; }
        public string ip { get; set; }
        public int tipo { get; set; }
        public int std { get; set; }
        public int main { get; set; }
        public int fa { get; set; }
        public string versiones { get; set; }
        public List<itemCpu> cpus = new List<itemCpu>();
        public U5kManWADGwData(U5kManStdData gdata, string Name, bool bGenerate = false)
        {
            if (bGenerate)
            {
                List<stdGw> stdgws = gdata.STDGWS;
                stdGw gw = stdgws.Where(i => i.name == Name).FirstOrDefault();
                if (gw != null)
                {
                    /** Parametros Generales */
                    name = SafeExecute<string>($"CGW-name", () => gw.name);
                    ip = SafeExecute<string>($"CGW-{gw.name}-ip", () => gw.ip);
                    tipo = SafeExecute<int>($"CGW-{gw.name}-tipo", () => gw.Dual ? 1 : 0);
                    std = SafeExecute<int>($"CGW-{gw.name}-std", () => (int)gw.std);
                    main = SafeExecute<int>($"CGW-{gw.name}-main", () => gw.Dual == false ? 0 : gw.gwA.Seleccionada ? 0 : gw.gwB.Seleccionada ? 1 : -1);

                    /** Versiones */
                    versiones = SafeExecute<string>($"CGW-{gw.name}-version", () => FormatVersiones((gw.Dual == false || gw.gwA.Seleccionada) ? gw.gwA.version : gw.gwB.version));

                    /** Datos de Interfaces CPU 0/1 */
                    for (int cpu = 0; cpu < 2; cpu++)
                    {
                        stdPhGw pgw = cpu == 0 ? gw.gwA : gw.gwB;
                        SafeExecute($"CGW-{gw.name}-cpu {cpu}", () =>
                        {
                            cpus.Add(new itemCpu()
                            {
                                ip = pgw.ip,
                                ntp = pgw.NtpInfo.GlobalStatus,
                                lan1 = pgw.lan1 == U5kManServer.std.Ok ? 1 : 0,
                                lan2 = pgw.lan2 == U5kManServer.std.Ok ? 1 : 0,
                                tars = PhysicalGwTars(pgw),
                                recs = PhysicalGwResources(pgw),
                                cfgMod = pgw.CfgMod.Std == U5kManServer.std.Ok ? 1 : 0,
                                sipMod = pgw.SipMod.Std == U5kManServer.std.Ok ? 1 : 0,
                                snmpMod = pgw.SnmpMod.Std == U5kManServer.std.Ok ? 1 : 0,
                                fa = pgw.stdFA == U5kManServer.std.Ok ? 1 : 0
                            });

                        });
                    }
                    fa = SafeExecute<int>($"CGW-{gw.name}-main", () => gw.gwA.stdFA == U5kManServer.std.Ok || gw.gwB.stdFA == U5kManServer.std.Ok ? 1 : 0);
                }
            }
        }
        public static void MainStandByChange(string name)
        {
            SafeExecute($"M/S on {name}", () => U5kManService.stMainStandbyChange(name, 0));
        }
        protected string FormatVersiones(string strVer)
        {
            return strVer;
        }
        protected List<itemRec> PhysicalGwResources(stdPhGw pgw)
        {
            List<itemRec> recursos = new List<itemRec>();
            foreach (stdSlot tar in pgw.slots)
            {
                foreach (stdRec rec in tar.rec)
                {
                    recursos.Add(new itemRec()
                    {
                        name = rec.name,
                        cfg = (int)rec.tipo,
                        not = (int)(rec.presente ? rec.tipo_online : trc.rcNotipo),
                        /** Los subtipos 3 (TXHF) si estan presentes estan OK */
                        std = (int)(rec.presente ? (rec.Stpo==3 ? U5kManServer.std.Ok : rec.std_online) : U5kManServer.std.NoInfo)
                    });
                }
            }

            return recursos;
        }
        protected List<itemTar> PhysicalGwTars(stdPhGw pgw)
        {
            List<itemTar> tars = new List<itemTar>();
            foreach (stdSlot tar in pgw.slots)
            {
                tars.Add(new itemTar() { cfg = (tar.std_cfg == U5kManServer.std.Ok) ? 1 : 0, not = (tar.std_online == U5kManServer.std.Ok) ? 1 : 0 });
            }
            return tars;
        }
    }
    class U5kManWADExtEqu : U5kManWebAppData
    {
        public class itemEqu
        {
            public string equipo { get; set; }
            public string name { get; set; }
            public string ip1 { get; set; }
            public string ip2 { get; set; }
            public int std { get; set; }
            public int tipo { get; set; }
            public int lan1 { get; set; }
            public int lan2 { get; set; }
            public int modelo { get; set; }
            public int txrx { get; set; }
            public int std_sip { get; set; }
            public string uri { get; set; }
            public string lor { get; set; }

            public override string ToString()
            {
                return $"{equipo}##{name}##{uri}##{std}";
            }
            public string hash => EncryptionHelper.StringMd5Hash(ToString());
        }
        public List<itemEqu> lista = new List<itemEqu>();
        public string hash { get; set; } = string.Empty;
        public U5kManWADExtEqu(U5kManStdData gdata, bool bGenerate = false)
        {
            if (bGenerate)
            {
                lista = gdata.STDEQS.Select(equipo => SafeExecute<itemEqu>($"EXEQ-{equipo.Id}-{equipo.sip_user}", () =>
                new itemEqu()
                    {
                        equipo = equipo.Id,
                        name = equipo.sip_user ?? equipo.Id,
                        ip1 = equipo.Ip1,
                        ip2 = equipo.Ip2,
                        std = (int)equipo.EstadoGeneral,
                        tipo = equipo.Tipo,
                        modelo = equipo.Modelo,
                        txrx = equipo.RxTx,
                        lan1 = (int)equipo.EstadoRed1,
                        lan2 = (int)equipo.EstadoRed2,
                        std_sip = (int)equipo.EstadoSip,
                        uri = String.Format("sip:{0}@{1}:{2}", equipo.sip_user, equipo.Ip1, equipo.sip_port),
                        lor = equipo.LastOptionsResponse
                    })                
                ).ToList();
                hash = EncryptionHelper.StringMd5Hash(lista.Select(i => i.hash).Aggregate("", (p, s) => p + s));
            }
        }
    }
    class U5kManWADExtAtsDst : U5kManWebAppData // No se utiliza el REST asociado en el cliente
    {
        public class itemDst
        {
            public string name { get; set; }
            public string centro { get; set; }
            public string ip1 { get; set; }
            public string ip2 { get; set; }
            public string ats { get; set; }
            public string uri { get; set; }
            public int std { get; set; }
            public int lan1 { get; set; }
            public int lan2 { get; set; }
            public int std_sip { get; set; }
        }
        public List<itemDst> lista = new List<itemDst>();
        public U5kManWADExtAtsDst(U5kManStdData gdata, bool bGenerate = false)
        {
            if (bGenerate)
            {
                foreach (EquipoEurocae equipo in gdata.STDEQS)
                {
                    lista.Add(new itemDst()
                    {
                        name = equipo.Id,
                        centro = equipo.fid,
                        ip1 = equipo.Ip1,
                        ip2 = equipo.Ip2,
                        ats = equipo.sip_user,
                        uri = String.Format("sip:{0}@{1}:{2}", equipo.sip_user, equipo.Ip1, equipo.sip_port),
                        std = (int)equipo.EstadoGeneral,
                        lan1 = (int)equipo.EstadoRed1,
                        lan2 = (int)equipo.EstadoRed2,
                        std_sip = (int)equipo.EstadoSip
                    });
                }
            }
        }
    }
    class U5kManWADPbx : U5kManWebAppData
    {
        public class itemPabx
        {
            public string name { get; set; }
            public int std { get; set; }
        }
        public List<itemPabx> lista = new List<itemPabx>();
        public U5kManWADPbx(U5kManStdData gdata, bool bGenerate = false)
        {
            if (bGenerate)
            {
                lista = gdata.STDPBXS
                    .Select(d => SafeExecute<itemPabx>($"pbxsub-{d.Id}", () => new itemPabx() { name = d.Id, std = (int)d.Estado }))
                    .ToList();
            }
        }
    }
    class U5kManWADDbCwps : U5kManWebAppData
    {
        public class itemDbCWP
        {
            public string id { get; set; }
        }
        public List<itemDbCWP> lista = new List<itemDbCWP>();
        public U5kManWADDbCwps(U5kManStdData gdata, bool bGenerate = false)
        {
            if (bGenerate)
            {
                List<stdPos> stdpos = gdata.STDTOPS;
                foreach (stdPos pos in stdpos)
                {
                    lista.Add(new itemDbCWP()
                    {
                        id = pos.name
                    });
                }
            }
        }
    }
    class U5kManWADDbGws : U5kManWebAppData
    {
        public class itemDbGW
        {
            public string id { get; set; }
        }
        public List<itemDbGW> lista = new List<itemDbGW>();
        public U5kManWADDbGws(U5kManStdData gdata, bool bGenerate)
        {
            if (bGenerate)
            {
                List<stdGw> stdgws = gdata.STDGWS;
                lista = stdgws.Select(gw => new itemDbGW() { id = gw.name }).ToList();
            }
        }
    }
    class U5kManAllhard : U5kManWebAppData
    {
        public class itemHard
        {
            public string Id { get; set; }
            public int tipo { get; set; }
        }
        public List<itemHard> items { get; set; }
        public U5kManAllhard(U5kManStdData gdata )
        {
            items = new List<itemHard>();
            /** Añado los Operadores */
            items.AddRange(gdata.STDTOPS.Select(item => new itemHard() {Id = item.name, tipo = 0 }).ToList()); 
            /** Añado las Pasarelas */
            items.AddRange(gdata.STDGWS.Select(gw => new itemHard() { Id = gw.name, tipo = 1 }).ToList());
            /** Añado los equipos*/
            items.AddRange(gdata.STDEQS.Select(eq => new itemHard() { Id = eq.sip_user ?? eq.Id, tipo = eq.Tipo }).ToList());
        }
    }
    class U5kManWADDbMNItems : U5kManWebAppData
    {
        public class itemDb
        {
            public string id { get; set; }
        }
        public List<itemDb> lista = new List<itemDb>();
        public U5kManWADDbMNItems(bool bGenerate)
        {
            if (bGenerate)
            {
                List<string> _lista = SafeExecute<List<string>>("DB-MN-LIST", () => U5kManService.bdtListaItemsMN());
                foreach (String str in _lista)
                {
                    lista.Add(new itemDb() { id = str });
                }
            }
        }
    }
    class U5kManWADDbInci : U5kManWebAppData
    {
        public List<U5kIncidenciaDescr> lista = new List<U5kIncidenciaDescr>();
        public U5kManWADDbInci(bool bGenerate = false)
        {
            if (bGenerate)
            {
                try
                {
                    lista = SafeExecute<List<U5kIncidenciaDescr>>("DB-INCI-LIST",
                        () => U5kManService.stListaIncidencias((idioma) => { LogDebug<U5kManWADDbInci>("Lista de Incidencias en " + idioma); })
                        );
                }
                catch (Exception x)
                {
                    LogException<U5kManWADDbInci>( "", x);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            foreach (U5kIncidenciaDescr inci in lista)
            {
                try
                {
                    U5kManService.bdtUpdateIncidencia(inci);
                }
                catch (Exception x)
                {
                    LogException<U5kManWADDbInci>( "", x);
                }
            }

            // Hay que Reiniciar el Servicio Temporizadamente...
            U5kGenericos.ResetService = true;
        }
    }
    class U5kManWADDbHist : U5kManWebAppData
    {
        public class Filtro
        {
            public DateTime dtDesde { get; set; }
            public DateTime dtHasta { get; set; }
            public int tpMat { get; set; }
            public string Mat { get; set; }
            public string txt { get; set; }
            public string limit { get; set; }
            public List<string> Inci { get; set; }

            public string SqlQuery
            {
                get
                {
                    string strConsulta = string.Format("SELECT * FROM HISTORICOINCIDENCIAS WHERE ({0}{1}{2}{3}{4}) ORDER BY FECHAHORA DESC LIMIT " + limit /*1000"*/,
                        /** Fecha Hora */
                        FiltroFechas,
                        FiltroTipoHardware,
                        FiltroIdHardware,
                        FiltroIncidencias(),
                        FiltroRegexpTextoExt
                        );
                    return strConsulta;
                }
            }
            protected string FiltroFechas
            {
                get
                {
                    DateTime dtDesdeF = dtDesde.ToLocalTime();    // new DateTime(dtDesde.Year, dtDesde.Month, dtDesde.Day);
                    DateTime dtHastaF = dtHasta.ToLocalTime();    // new DateTime(dtHasta.Year, dtHasta.Month, dtHasta.Day, 23, 59, 59);
                    string filtro2 = string.Format("(  FECHAHORA BETWEEN '{0:yyyy-MM-dd HH:mm}' AND '{1:yyyy-MM-dd HH:mm}')",
                                    dtDesdeF, dtHastaF);
                    return filtro2;
                }
            }
            protected string FiltroTipoHardware
            {
                get
                {
                    string strFiltro = "";
                    switch (tpMat)
                    {
                        case 1:           // Operadores.
                            strFiltro = " AND (TIPOHW = 0)";
                            break;
                        case 2:           // Pasarelas..
                            strFiltro = " AND (TIPOHW = 1)";
                            break;
                        default:            // Resto...
                            strFiltro = ""; // " AND (TIPOHW = 4)";
                            break;
                    }
                    return strFiltro;
                }
            }
            protected string FiltroIdHardware
            {
                get
                {
                    if (Mat == "" ||
                        Mat == idiomas.strings.WAP_MSG_005 /* Todas */ ||
                        Mat == idiomas.strings.WAP_MSG_014 /* Todos */)
                        return "";
                    return string.Format(" AND (IDHW LIKE '{0}%')", Mat);
                }
            }
            protected string FiltroGrupoIncidencias
            {
                get
                {
                    string strFiltro = "";
                    switch (tpMat)
                    {
                        case 0:          // Generales... (i.id >= 50 && i.id < 1000)
                            strFiltro = " AND ( (IDINCIDENCIA >= 50 AND IDINCIDENCIA < 1000) )";
                            break;
                        case 1:           // Operadores. (i.id >= 1000 && i.id < 2000)
                            strFiltro = " AND ( (IDINCIDENCIA >= 1000 AND IDINCIDENCIA < 2000) )";
                            break;
                        case 2:           // Pasarelas.. (i.id >= 2000 && i.id < 3000)
                            strFiltro = " AND ( (IDINCIDENCIA >= 2000 AND IDINCIDENCIA < 3000) )";
                            break;
                        case 3:           // HF. (i.id < 50) 
                            strFiltro = " AND ( (IDINCIDENCIA < 50) )";
                            break;
                        case 4:           // M+N.. (i.id >= 3050 && i.id < 3100)
                            strFiltro = " AND ( (IDINCIDENCIA >= 3050 AND IDINCIDENCIA < 3200) )";
                            break;
                        case 5:           // Equipo Externos
                            strFiltro = " AND ( (IDINCIDENCIA >= 3000 AND IDINCIDENCIA < 3050) )";
                            break;
                        case 6:             // Solo Incidencias que son alarmas.
                            strFiltro = HistThread.SqlFilterForAlarms;
                            break;
                        case 8:
                            strFiltro = " AND (IDHW LIKE 'ProxySacta%')";
                            break;
                        default:
                            strFiltro = " AND ( (IDINCIDENCIA < 5000) )";
                            break;
                    }
                    return strFiltro;
                }
            }
            protected string FiltroIncidencias()
            {
                if (Inci.Count > 0)
                {
                    string filtro = " AND (";
                    foreach (string valor in Inci)
                    {
                        if (valor == "-1")
                            return FiltroGrupoIncidencias;

                        filtro += string.Format("IDINCIDENCIA = {0} OR ", valor);
                    }
                    filtro = filtro.Substring(0, filtro.Length - 3) + ")";
                    return filtro;
                }
                // Si no hay incidencias Seleccionadas o Se ha seleccionado 'todas'... Retorna Grupo de Incidencias..
                return FiltroGrupoIncidencias;
            }
            protected string FiltroTexto
            {
                get
                {
                    string strFiltro = "";
                    if (txt != null && txt != string.Empty)
                    {
                        string[] campos = txt.Split(';');
                        strFiltro = " AND (";
                        foreach (string campo in campos)
                        {
                            strFiltro += String.Format("Descripcion LIKE '%{0}%' AND ", campo.Trim());
                        }
                        strFiltro = strFiltro.Substring(0, strFiltro.Length - 4) + ")";
                    }
                    return strFiltro;
                }
            }
            protected string FiltroRegexpTexto
            {
                get
                {
                    string strFiltro = "";
                    if (txt != null && txt != string.Empty)
                    {
                        strFiltro = " AND (Descripcion REGEXP '" + txt + "')";
                    }
                    return strFiltro;
                }
            }
            protected string FiltroRegexpTextoExt
            {
                get
                {
                    string strFiltro = "";
                    if (txt != null && txt != string.Empty)
                    {
                        string[] campos = txt.Split('&');
                        strFiltro = " AND (";
                        foreach (string campo in campos)
                        {
                            strFiltro += String.Format("Descripcion REGEXP '{0}' AND ", campo.Trim());
                        }
                        strFiltro = strFiltro.Substring(0, strFiltro.Length - 4) + ")";
                    }
                    return strFiltro;
                }
            }
        }
        public List<U5kHistLine> lista = null;
        public U5kManWADDbHist(string jFiltro)
        {
            Filtro f = JDeserialize<Filtro>(jFiltro);
            string strQuery = f.SqlQuery;

            lista = SafeExecute<List<U5kHistLine>>("HIS-QUERY", () => U5kManService.bdtConsultaHistorico(strQuery));
            LogDebug<U5kManWADDbHist>("HistQuery: " + strQuery);
        }
    }
    class U5kManWADDbEstadistica : U5kManWebAppData
    {
        class Filtro
        {
            public DateTime desde { get; set; }
            public DateTime hasta { get; set; }
            public U5kEstadisticaTiposElementos tipo { get; set; }
            public List<string> elementos { get; set; }

            public void Normalize()
            {
                desde = desde.ToLocalTime();
                hasta = hasta.ToLocalTime();

                if (hasta < desde)
                    hasta = desde;
            }
        }
        public U5kEstadisticaResultado res = null;
        public U5kManWADDbEstadistica(string jfiltro)
        {
            SafeExecute("STAT-PRE", () =>
            {
                Filtro f = JDeserialize<Filtro>(jfiltro);
                f.Normalize();
                res = SafeExecute<U5kEstadisticaResultado>("STAT-EXE", 
                    () => U5kEstadisticaProc.Estadisticas.Calcula(f.desde, f.hasta, f.tipo, f.elementos));
            });
        }
    }
    class U5kManWADSnmpOptions : U5kManWebAppData
    {
        public class snmpOption
        {
            public string id { get; set; }
            public int tp { get; set; }
            public List<string> opt { get; set; }
            public object val { get; set; }
            public string key { get; set; }
            public int show { get; set; }
        }
        public List<snmpOption> snmpOptions = new List<snmpOption>()
        {
            new snmpOption()
                {
                    id=idiomas.strings.VERSION,
                    tp=1, 
                    opt = new List<string>()
                    {
                        idiomas.strings.V2,
                        idiomas.strings.V3
                    }, 
                    show=0,
                    key="Snmp_AgentVersion",
                    val=""
                },
            new snmpOption()
                {
                    id=idiomas.strings.SNMPPORT,
                    tp=0, 
                    opt = null, 
                    show=0,
                    key="Snmp_AgentPort", 
                    val=""
                },
            new snmpOption()
                {
                    id=idiomas.strings.SNMPTRAPPORT,
                    tp=0, 
                    opt = null, 
                    show=0,
                    key="Snmp_AgentListenTrapPort", 
                    val=""
                },
            new snmpOption()
                {
                    id=idiomas.strings.IDGETCOMM,
                    tp=0, 
                    opt = null, 
                    show=1,
                    key="Snmp_V2AgentGetComm", 
                    val=""
                },
            new snmpOption()
                {
                    id=idiomas.strings.IDSETCOMM,
                    tp=0, 
                    opt = null, 
                    show=1,
                    key="Snmp_V2AgentSetComm", 
                    val=""
                },
            new snmpOption()
                {
                    id=idiomas.strings.USERS,
                    tp=2, 
                    opt = new List<string>(), 
                    show=2,
                    key="Snmp_V3Users", 
                    val=""
                }
        };
        public U5kManWADSnmpOptions(bool bGenerate = false)
        {
            if (bGenerate)
            {
                SafeExecute("SnmpOptGet", () =>
                {
                    foreach (snmpOption item in snmpOptions)
                    {
                        item.val = PropertyGet(item.key);
                    }
                });
            }
        }
        public void Save()
        {
            SafeExecute("", () =>
            {
                foreach (snmpOption opt in snmpOptions)
                {
                    PropertySet(opt.key, opt.val);
                }
                U5kManServer.Properties.u5kManServer.Default.Save();
            });
            // Hay que Reiniciar el Servicio Temporizadamente...
            U5kGenericos.ResetService = true;
        }
        protected object PropertyGet(string key)
        {
            U5kManServer.Properties.u5kManServer Prop = U5kManServer.Properties.u5kManServer.Default;
            switch (key)
            {
                case "Snmp_AgentVersion":
                    return Prop.Snmp_AgentVersion=="v3" ? "1" : "0";
                case "Snmp_AgentPort":
                    return Prop.Snmp_AgentPort.ToString();
                case "Snmp_AgentListenTrapPort":
                    return Prop.Snmp_AgentListenTrapPort.ToString();
                case "Snmp_V2AgentGetComm":
                    return Prop.Snmp_V2AgentGetComm;
                case "Snmp_V2AgentSetComm":
                    return Prop.Snmp_V2AgentSetComm;
                case "Snmp_V3Users":
                    return Prop.Snmp_V3Users;
            }
            return "";
        }
        protected void PropertySet(string key, object val)
        {
            U5kManServer.Properties.u5kManServer Prop = U5kManServer.Properties.u5kManServer.Default;
            switch (key)
            {
                case "Snmp_AgentVersion":
                    Prop.Snmp_AgentVersion = val.ToString() == "0" ? "v2" : "v3";
                    break;
                case "Snmp_AgentPort":
                    Prop.Snmp_AgentPort = Int32.Parse(val as String);
                    break;
                case "Snmp_AgentListenTrapPort":
                    Prop.Snmp_AgentListenTrapPort = Int32.Parse(val as String);
                    break;
                case "Snmp_V2AgentGetComm":
                    Prop.Snmp_V2AgentGetComm = val as String;
                    break;
                case "Snmp_V2AgentSetComm":
                    Prop.Snmp_V2AgentSetComm = val as String;
                    break;
                case "Snmp_V3Users":
                    Prop.Snmp_V3Users.Clear();
                    List<string> users = (val as JArray).ToObject<List<string>>();
                    Prop.Snmp_V3Users.AddRange(users.ToArray());
                    break;
            }
        }
    }
    class U5kManWADOptions : U5kManWebAppData
    {
        public class itemOptions
        {
            public string id { get; set; }
            public string val { get; set; }
            public int tp { get; set; }
            public List<string> opt { get; set; }
            public string key { get; set; }
        }
        public string version { get; set; }
        public string bdt { get; set; }
        public List<itemOptions> lconf = new List<itemOptions>();
        class itemProperty
        {
            public string id { get; set; }
            public int tp { get; set; }
            public List<string> opt { get; set; }
        }
        Dictionary<string, itemProperty> prop = new Dictionary<string, itemProperty>()
        {
            {
                "Idioma",
                new itemProperty()
                {                        
                    id=idiomas.strings.Idioma/*"Idioma"*/,                     
                    tp=1,                     
                    opt=new List<string>()                    
                    {                    
                        "es",                        
                        "fr",                        
                        "en"                         
                    }                    
                }
            },
            {
                "ServidorDual",
                new itemProperty()                
                {
                    id=idiomas.strings.ServidorDual /*"Servidor Dual"*/, 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            {
                "HayReloj",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_008 /*"Patron de Reloj"*/, 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            //{
            //    "HayPabx",
            //    new itemProperty()
            //    {
            //        id=idiomas.strings.WAP_MSG_009 /*"PABX Interna"*/, 
            //        tp=1, 
            //        opt=new List<string>()
            //        {
            //            "False",
            //            "True"
            //        }
            //    }
            //},
            //{
            //    "GwsUnificadas",
            //    new itemProperty()
            //    {
            //        id=idiomas.strings.WAP_MSG_016 /*"PABX Interna"*/, 
            //        tp=1, 
            //        opt=new List<string>()
            //        {
            //            idiomas.strings.WAP_OPT_007,
            //            idiomas.strings.WAP_OPT_008
            //        }
            //    }
            //},
            {
                "HaySacta",
                new itemProperty()
                {
                    id="SACTA", 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            {
                "HaySactaProxy",
                new itemProperty()
                {
                    id="Proxy SACTA",
                    tp=1,
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            {
                "HayAltavozHF",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_015,      // "AltavozHF", 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            {
                "OpcOpCableGrabacion",
                new itemProperty()
                {
                    id="Cables de Grabacion" /*idiomas.strings.WAP_MSG_015*/,      // "AltavozHF", TODO
                    tp=1,
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            {
                "SonidoAlarmas",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_010 /*"Sonido"*/, 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True"
                    }
                }
            },
            {
                "GenerarHistorico",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_011 /*"Generar Historico"*/, 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True" 
                    }
                }
            },
            {
                "PttAndSqhOnBdt",
                new itemProperty()
                {
                    id= idiomas.strings.EVENTOSPTT, // "Almacenar Eventos PTT / SQH",
                    tp=1, 
                    opt=new List<string>()
                    {
                        "False",
                        "True" 
                    }
                }
            },
            {
                "DiasEnHistorico",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_012 /*"Dias en Historico"*/, 
                    tp=1, 
                    opt=new List<string>()
                    {
                        idiomas.strings.WAP_OPT_001 /*"1 Semana"*/,
                        idiomas.strings.WAP_OPT_002 /*"2 Semanas"*/, 
                        idiomas.strings.WAP_OPT_003 /*"1 Mes"*/, 
                        idiomas.strings.WAP_OPT_004 /*"3 Meses"*/, 
                        idiomas.strings.WAP_OPT_005 /*"6 Meses"*/, 
                        idiomas.strings.WAP_OPT_006 /*"1 Año"*/ 
                    }
                }
            },
            {
                "LineasIncidencias",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_013 /*"Incidencias sin Reconocer"*/, 
                    tp=1, 
                    opt=new List<string>()
                    {
                        "8", 
                        "16", 
                        "32", 
                        "64" 
                    }
                }
            },
            {
                "WebInactivityTimeout",
                new itemProperty()
                {
                    id=idiomas.strings.WAP_MSG_017 /*"Tiempo Maximo de Inactivad de sesion (minutos)"*/,
                    tp=1,
                    opt=new List<string>()
                    {
                        "5",
                        "15",
                        "30",
                        "60"
                    }
                }
            },
        };
        public U5kManWADOptions(U5kManStdData gdata, bool bGenerate = false)
        {
            if (bGenerate)
            {
                SafeExecute("GENOPT-GET", () =>
                {
                    version = U5kGenericos.Version;
                    bdt = gdata.STDG.CfgId;        // .cfgVersion;
                    foreach (KeyValuePair<string, itemProperty> p in prop)
                    {
                        lconf.Add(new itemOptions()
                        {
                            key = p.Key,
                            id = p.Value.id,
                            tp = p.Value.tp,
                            opt = p.Value.opt,
                            val = PropertyGet(p.Key)    // U5kManServer.Properties.u5kManServer.Default.
                        });
                    }
                });
            }
        }
        public void Save()
        {
            SafeExecute("GENOPT-SET", () =>
            {
                foreach (itemOptions opt in lconf)
                {
                    PropertySet(opt.key, opt.val);
                }
                U5kManService.cfgSettings.Save();
            });
            // Hay que Reiniciar el Servicio Temporizadamente...
            U5kGenericos.ResetService = true;
        }
        protected string PropertyGet(string key)
        {
            var Prop = U5kManService.cfgSettings;
            switch (key)
            {
                case "Idioma":
                    return Prop.Idioma == "es" ? "0" : Prop.Idioma == "fr" ? "1" : "2";
                case "ServidorDual":
                    return Prop.ServidorDual ? "1" : "0";
                case "HayReloj":
                    return Prop.HayReloj ? "1" : "0";
                case "HaySacta":
                    return Prop.HaySacta ? "1" : "0";
                case "HaySactaProxy":
                    return Prop.HaySactaProxy ? "1" : "0";
                case "HayAltavozHF":
                    return Prop.HayAltavozHF ? "1" : "0";
                case "OpcOpCableGrabacion":
                    return Prop.OpcOpeCableGrabacion ? "1" : "0";
                case "SonidoAlarmas":
                    return Prop.SonidoAlarmas ? "1" : "0";
                case "GenerarHistorico":
                    return Prop.GenerarHistoricos ? "1" : "0";
                case "DiasEnHistorico":
                    return Prop.DiasEnHistorico <= 7 ? "0" :
                        Prop.DiasEnHistorico <= 14 ? "1" :
                        Prop.DiasEnHistorico <= 30 ? "2" :
                        Prop.DiasEnHistorico <= 92 ? "3" :
                        Prop.DiasEnHistorico <= 184 ? "4" : "5";
                case "LineasIncidencias":
                    return Prop.LineasIncidencias <= 8 ? "0" :
                        Prop.LineasIncidencias <= 16 ? "1" :
                        Prop.LineasIncidencias <= 32 ? "2" : "3";
                case "PttAndSqhOnBdt":
                    return Prop.Historico_PttSqhOnBdt == false ? "0" : "1";
                case "WebInactivityTimeout":
                    return Prop.WebInactivityTimeout <= 5 ? "0" :
                        Prop.WebInactivityTimeout <= 15 ? "1" :
                        Prop.WebInactivityTimeout <= 30 ? "2" : "3";
                default:
                    return "0";
            }
        }
        protected void PropertySet(string key, string val)
        {
            var Prop = U5kManService.cfgSettings;
            switch (key)
            {
                case "Idioma":
                    Prop.Idioma = val == "0" ? "es" : val == "1" ? "fr" : "en";
                    break;
                case "ServidorDual":
                    Prop.ServidorDual = (val == "1");
                    break;
                case "HayReloj":
                    Prop.HayReloj = (val == "1");
                    break;
                case "HaySacta":
                    Prop.HaySacta = (val == "1");
                    break;
                case "HaySactaProxy":
                    Prop.HaySactaProxy = (val == "1");
                    break;
                case "HayAltavozHF":
                    Prop.HayAltavozHF = (val == "1");
                    break;
                case "OpcOpCableGrabacion":
                    Prop.OpcOpeCableGrabacion = (val == "1");
                    break;
                case "SonidoAlarmas":
                    Prop.SonidoAlarmas = (val == "1");
                    break;
                case "GenerarHistorico":
                    Prop.GenerarHistoricos = (val == "1");
                    break;
                case "DiasEnHistorico":
                    Prop.DiasEnHistorico = val == "0" ? 7 :
                        val == "1" ? 14 :
                        val == "2" ? 30 :
                        val == "3" ? 92 :
                        val == "4" ? 184 : 365;
                    break;
                case "LineasIncidencias":
                    Prop.LineasIncidencias = val == "0" ? 8 :
                        val == "1" ? 16 :
                        val == "2" ? 32 : 64;
                    break;
                case "PttAndSqhOnBdt":
                    Prop.Historico_PttSqhOnBdt = val == "1";
                    break;
                case "WebInactivityTimeout":
                    Prop.WebInactivityTimeout =
                        val == "0" ? 5 :
                        val == "1" ? 15 :
                        val == "2" ? 30 : 60;
                    break;
            }
        }
    }
    class UG5kVersion : U5kManWebAppData // No utilizada
    {
        public class Linea
        {
            public string res { get; set; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Version  : " + version + "\n");
            sb.Append("Config   : " + cfgsver + "\n");
            sb.Append("Snmp     : " + snmpver + "\n");
            sb.Append("Grabador : " + recsver + "\n");
            sb.Append("VOIP: " + "\n");
            foreach (Linea line in lines)
            {
                sb.Append("    " + line.res + "\n");
            }

            return sb.ToString();
        }
        public string version { get; set; }
        public string cfgsver { get; set; }
        public string snmpver { get; set; }
        public string recsver { get; set; }
        public List<Linea> lines { get; set; }
    }
    class UG5KExtVersion : U5kManWebAppData // No utilizada
    {
        public class Component
        {
            public class FileDesc
            {
                public string Path { get; set; }
                public string Date { get; set; }
                public string Size { get; set; }
                public int Modo { get; set; }
            }
            public string Name { get; set; }
            public string Id { get; set; }
            public List<FileDesc> Files { get; set; }
        }
        public string Version { get; set; }
        public string Fecha { get; set; }
        public List<Component> Components { get; set; }
        public override string ToString()
        {
            StringWriter strWriter = new StringWriter();

            strWriter.WriteLine("Version: {0}. Fecha: {1}", Version, Fecha);
            foreach (Component comp in Components)
            {
                strWriter.WriteLine();
                strWriter.WriteLine("{0,8}", comp.Id);
                foreach (Component.FileDesc file in comp.Files)
                {
                    strWriter.WriteLine("{0,30}: ({1,10}-{2,8})", Path.GetFileName(file.Path), file.Date, file.Size);
                }
            }
            return strWriter.ToString();
        }
    }
    public class SactaConfig : U5kManWebAppData
    {
        /* Estructura de la configuracion.
            {
                "TickPresencia": 5000,
                "TimeoutPresencia": 30000,
                "sacta": {
                    "Domain": 1,
                    "Center": 107,
                    "GrpUser": 110,
                    "SpiUsers": "111,112,113,114,7286,7287,7288,7289,15000",
                    "SpvUsers": "86,87,88,89,7266,7267,7268,7269,34000",
                    "lan1": {
                        "ipmask": "192.168.0.71",
                        "mcast": "225.12.101.1",
                        "udpport": 19204
                    },
                    "lan2": {
                        "ipmask": "192.168.1.71",
                        "mcast": "225.212.101.1",
                        "udpport": 19204
                    }
                },
                "scv": {
                    "Domain": 1,
                    "Center": 107,
                    "User": 10,
                    "Interfaz": "192.168.0.212",
                    "udpport": 15100,
                    "Ignore": "305"
                }
            }
         * */
        public static void RemoteConfigPatch(string local, string remote, Action<bool, string> result)
        {
            try
            {
                var localConfig = JsonHelper.SafeJObjectParse(local);
                if (localConfig != null)
                {
                    var remoteConfig = JsonHelper.SafeJObjectParse(remote);
                    if (remoteConfig != null)
                    {
                        if (remoteConfig["sacta"]==null || remoteConfig["sacta"]["lan2"]==null || remoteConfig["sacta"]["lan2"]["ipmask"] == null)
                        {
                            result(false, $"Erroneous remote settings => {remote}");
                        }
                        else if (localConfig["sacta"] == null || localConfig["sacta"]["lan2"] == null || localConfig["sacta"]["lan2"]["ipmask"] == null)
                        {
                            result(false, $"Erroneous local settings => {local}");
                        }
                        else
                        {
                            remoteConfig["sacta"]["lan2"]["ipmask"] = localConfig["sacta"]["lan2"]["ipmask"];
                            result(true, JsonHelper.ToString(remoteConfig, false));
                        }
                    }
                    else
                    {
                        result(false, $"Erroneous remote settings => {remote}");
                    }
                }
                else
                {
                    result(false, $"Erroneous local settings => {local}");
                }
            }
            catch(Exception x)
            {
                LogException<SactaConfig>("", x);
                result(false, $"Error => {x}");
            }
        }
    }
}
