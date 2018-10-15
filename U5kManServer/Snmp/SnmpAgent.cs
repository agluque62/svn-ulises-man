using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;

using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Pipeline;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Lextm.SharpSnmpLib.Objects;

using NucleoGeneric;
namespace U5kManServer
{
    /// <summary>
    /// 
    /// </summary>
    public class MibObject : ScalarObject, IComparable
    {
        string _oid = "";
        public override ISnmpData Data { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            MibObject orderToCompare = obj as MibObject;
            return string.Compare(_oid, orderToCompare._oid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        public MibObject(string oid)
            : base(oid)
        {
            _oid = oid;
            // LogManager.GetCurrentClassLogger().Info("OID: {0}", oid);
        }

        public String Oid { get { return _oid; } }
        
    }

    /// <summary>
    /// 
    /// </summary>
    sealed class SnmpIntObject : MibObject      // ScalarObject
    {
        /// <summary>
        /// 
        /// </summary>
        public int Value
        {
            get { return _data.ToInt32(); }
            set
            {
                if (Value != value)
                {
                    _data = new Integer32(value);

                    if ((_trapsEps != null) && (_trapsEps.Length > 0))
                    {
                        SnmpAgent.Trap(Variable.Id, _data, _trapsEps);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override ISnmpData Data
        {
            get { return _data; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _data = (Integer32)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="value"></param>
        /// <param name="trapsEps"></param>
        public SnmpIntObject(string oid, int value, params IPEndPoint[] trapsEps)
            : base(oid)
        {
            _data = new Integer32(value);
            _trapsEps = trapsEps;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static SnmpIntObject Get(string oid)
        {
            return SnmpAgent.Store.GetObject(new ObjectIdentifier(oid)) as SnmpIntObject;
        }

        #region Private Members
        /// <summary>
        /// 
        /// </summary>
        private Integer32 _data;
        private IPEndPoint[] _trapsEps;

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    sealed class SnmpStringObject : MibObject   // ScalarObject
    {
        /// <summary>
        /// 
        /// </summary>
        public string Value
        {
            get 
            {
                //NLog.LogManager.GetLogger("SnmpStringObject").Debug("Desde Value Get {0}", _data.ToString());
                return _data.ToString(); 
            }
            set
            {
                if (Value != value)
                {
                    _data = new OctetString(value);
                    if ((_trapsEps != null) && (_trapsEps.Length > 0))
                    {
                        SnmpAgent.Trap(Variable.Id, _data, _trapsEps);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void SendTrap(string value)
        {
            OctetString data = new OctetString(value);

            if ((_trapsEps != null) && (_trapsEps.Length > 0))
                SnmpAgent.Trap(Variable.Id, data, _trapsEps);
        }

        /// <summary>
        /// 
        /// </summary>
        public override ISnmpData Data
        {
            get { return _data; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _data = (OctetString)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="value"></param>
        /// <param name="trapsEps"></param>
        public SnmpStringObject(string oid, string value, params IPEndPoint[] trapsEps)
            : base(oid)
        {
            _data = new OctetString(value);
            _trapsEps = trapsEps;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public static SnmpStringObject Get(string oid)
        {
            return SnmpAgent.Store.GetObject(new ObjectIdentifier(oid)) as SnmpStringObject;
        }

        #region Private Members

        /// <summary>
        /// 
        /// </summary>
        private Lextm.SharpSnmpLib.OctetString _data;
        private IPEndPoint[] _trapsEps;

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    sealed class SnmpObjectIdObject : MibObject
    {
        public SnmpObjectIdObject(ObjectIdentifier oid)
            : base(".1.3.6.1.2.1.1.2.0")
        {
            _data = oid;
        }
        public override ISnmpData Data
        {
            get { return _data; }
        }
        private ObjectIdentifier _data;
    }

    /// <summary>
    /// 
    /// </summary>
    sealed class SnmpSysUpTimeObject : MibObject
    {
        public SnmpSysUpTimeObject()
            : base(".1.3.6.1.2.1.1.3.0")
        {
            _data = new TimeTicks((uint)Environment.TickCount / 10);
        }
        public override ISnmpData Data
        {
            get { return _data; }
            set { _data = new TimeTicks((uint)Environment.TickCount / 10); }
        }
        private TimeTicks _data;
    }

    /// <summary>
    /// 
    /// </summary>
    class SnmpLogger : BaseCode, Lextm.SharpSnmpLib.Pipeline.ILogger
    {
        private const string Empty = "-";

        /// <summary>
        /// 
        /// </summary>
        public SnmpLogger()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public void Log(ISnmpContext context)
        {
            //if (_logger.IsTraceEnabled) 
            //{                
            //    _logger.Trace(GetLogEntry(context));
            //}
            LogTrace<SnmpLogger>( GetLogEntry(context));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private /*static*/ string GetLogEntry(ISnmpContext context)
        {
            return string.Format(
                 CultureInfo.InvariantCulture,
                 "{0}-{1}:{2} {3} {4}", // {5} {6} {7} {8}", // {9}",
                 context.Request.TypeCode() == SnmpType.Unknown ? Empty : context.Request.TypeCode().ToString(),
                 context.Sender.Address,
                 context.Binding.Endpoint.Port,
                 context.Request.Parameters.UserName,
                 GetStem(context.Request.Pdu().Variables),
                 // DateTime.UtcNow,
                 context.Binding.Endpoint.Address,
                 (context.Response == null) ? Empty : context.Response.Pdu().ErrorStatus.ToErrorCode().ToString(),
                 context.Request.Version,
                 DateTime.Now.Subtract(context.CreatedTime).TotalMilliseconds);

            /*
            return string.Format(
                 CultureInfo.InvariantCulture,
                 "{0} {1} {2} {3} {4} {5} {6} {7} {8}", // {9}",
                 // DateTime.UtcNow,
                 context.Binding.Endpoint.Address,
                 context.Request.TypeCode() == SnmpType.Unknown ? Empty : context.Request.TypeCode().ToString(),
                 GetStem(context.Request.Pdu().Variables),
                 context.Binding.Endpoint.Port,
                 context.Request.Parameters.UserName,
                 context.Sender.Address,
                 (context.Response == null) ? Empty : context.Response.Pdu().ErrorStatus.ToErrorCode().ToString(),
                 context.Request.Version,
                 DateTime.Now.Subtract(context.CreatedTime).TotalMilliseconds);
             * */
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        private /*static*/ string GetStem(ICollection<Variable> variables)
        {
            if (variables.Count == 0)
            {
                return Empty;
            }

            StringBuilder result = new StringBuilder();
            foreach (Variable v in variables)
            {
                result.AppendFormat("{0};", v.Id);
            }

            if (result.Length > 0)
            {
                result.Length--;
            }

            return result.ToString();
        }
    }


    /// <summary>
    /// 
    /// </summary>
	/*static*/ class SnmpAgent : BaseCode
	{
        /// <summary>
        /// 
        /// </summary>
        // public /*static*/ event Action<string, ISnmpData, IPEndPoint, IPEndPoint> TrapReceived = delegate { };
        public /*static*/ event Action<string, string, ISnmpData, IPEndPoint, IPEndPoint> TrapReceived = delegate { };
       

        /// <summary>
        /// 
        /// </summary>
		public /*static*/ SynchronizationContext Context
		{
			get { return _context; }
			set { _context = value; }
		}

        /// <summary>
        /// 
        /// </summary>
        private static ObjectStore _store;
        public  static ObjectStore Store
		{
			get { return /*SnmpAgent.*/_store; }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
		public /*static*/ void Init(string ip)
		{
			// Init(ip, null, 161, 162);
            CreateAgent(ip);
		}



        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="trapMcastIp"></param>
        /// <param name="port"></param>
        /// <param name="trap"></param>
        /*static*/ TrapV1MessageHandler trapv1 = new TrapV1MessageHandler();
        /*static*/ TrapV2MessageHandler trapv2 = new TrapV2MessageHandler();
        /*static*/ InformRequestMessageHandler inform = new InformRequestMessageHandler();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="trapMcastIp"></param>
        /// <param name="port"></param>
        /// <param name="trap"></param>
        public /*static*/ void Init(string ip1, string trapMcastIp, int port, int trap)
		{
            string v2v3 = "v2,v3"; // "v2,v3";
			SnmpLogger logger = new SnmpLogger();
			ObjectStore objectStore = new ObjectStore();

			OctetString getCommunityPublic = new OctetString("public");                         // 
			OctetString setCommunityPublic = new OctetString("public");
            OctetString getCommunityPrivate = new OctetString("private");                       
            OctetString setCommunityPrivate = new OctetString("private");

            /** */
            trapv1 = new TrapV1MessageHandler();
            trapv2 = new TrapV2MessageHandler();
            inform = new InformRequestMessageHandler();

			IMembershipProvider[] membershipProviders = new IMembershipProvider[]
			{
				// new Version1MembershipProvider(getCommunity, setCommunity),
				new Version2MembershipProvider(getCommunityPublic, setCommunityPublic),
				new Version2MembershipProvider(getCommunityPrivate,setCommunityPublic),
				new Version3MembershipProvider()
			};
			IMembershipProvider composedMembershipProvider = new ComposedMembershipProvider(membershipProviders);
            /**/

			HandlerMapping[] handlerMappings = new HandlerMapping[]
			{
				new HandlerMapping("v1", "GET", new GetV1MessageHandler())
                ,new HandlerMapping(v2v3, "GET", new GetMessageHandler())
                ,new HandlerMapping("v1", "SET", new SetV1MessageHandler())
                ,new HandlerMapping(v2v3, "SET", new SetMessageHandler())
                ,new HandlerMapping("v1", "GETNEXT", new GetNextV1MessageHandler())
                ,new HandlerMapping(v2v3, "GETNEXT", new GetNextMessageHandler())
                ,new HandlerMapping(v2v3, "GETBULK", new GetBulkMessageHandler())
                ,new HandlerMapping("v1", "TRAPV1", trapv1)
                ,new HandlerMapping(v2v3, "TRAPV2", trapv2)
                ,new HandlerMapping(v2v3, "INFORM", inform)
                //,new HandlerMapping("*", "*", NullMessageHandler)
			};
			MessageHandlerFactory messageHandlerFactory = new MessageHandlerFactory(handlerMappings);

			User[] users = new User[]
			{
				new User(new OctetString("nucleodf"), DefaultPrivacyProvider.DefaultPair),
				new User(new OctetString("nucleodfa"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("ulisesv5000")))),
				new User(new OctetString("nucleodfap"), new DESPrivacyProvider(new OctetString("privacyphrase"), new MD5AuthenticationProvider(new OctetString("ulisesv5000"))))
			};
			UserRegistry userRegistry = new UserRegistry(users);

			EngineGroup engineGroup = new EngineGroup();
			Listener listener = new Listener() { Users = userRegistry };
			SnmpApplicationFactory factory = new SnmpApplicationFactory(logger, objectStore, composedMembershipProvider, messageHandlerFactory);

			_engine = new SnmpEngine(factory, listener, engineGroup);

            //_engine.Listener.AddBinding(new IPEndPoint(IPAddress.Parse(ip), port));
            //_engine.Listener.AddBinding(new IPEndPoint(IPAddress.Parse(ip), trap));
            _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, port));
            _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, trap));

            // TRAP's de los Recursos...
            IPEndPoint[] RecEP = new IPEndPoint[]
            {
                new IPEndPoint(IPAddress.Any, 16211),new IPEndPoint(IPAddress.Any, 16212),
                new IPEndPoint(IPAddress.Any, 16213),new IPEndPoint(IPAddress.Any, 16214),
                new IPEndPoint(IPAddress.Any, 16221),new IPEndPoint(IPAddress.Any, 16222),
                new IPEndPoint(IPAddress.Any, 16223),new IPEndPoint(IPAddress.Any, 16224),
                new IPEndPoint(IPAddress.Any, 16231),new IPEndPoint(IPAddress.Any, 16232),
                new IPEndPoint(IPAddress.Any, 16233),new IPEndPoint(IPAddress.Any, 16234),
                new IPEndPoint(IPAddress.Any, 16241),new IPEndPoint(IPAddress.Any, 16242),
                new IPEndPoint(IPAddress.Any, 16243),new IPEndPoint(IPAddress.Any, 16244)
            };
            foreach (IPEndPoint ep in RecEP)
                _engine.Listener.AddBinding(ep);
            //--------------------------------------------------------------------------------
            // _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, trap));
            _engine.ExceptionRaised += (sender, e) => LogException<SnmpAgent>("", e.Exception);
			_closed = false;
			_store = objectStore;
			_context = SynchronizationContext.Current ?? new SynchronizationContext();

            /** */
            trapv1.MessageReceived += TrapV1Received;
			trapv2.MessageReceived += TrapV2Received;
			inform.MessageReceived += InformRequestReceived;

			// OJO.. (new IPEndPoint(IPAddress.Parse(ip), 0)).SetAsDefault();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        public void CreateAgent(string ip)
        {
            Properties.u5kManServer cfg = Properties.u5kManServer.Default;
            string snmpVersion = cfg.Snmp_AgentVersion.ToLower()=="v3" ? "v3" : "v2";    // Por defecto será v2
            SnmpLogger logger = new SnmpLogger();
            ObjectStore objectStore = new ObjectStore();
            /** */
            HandlerMapping[] handlerMappings;
            IMembershipProvider[] membershipProviders;
            Listener listener;

            trapv1 = new TrapV1MessageHandler();
            trapv2 = new TrapV2MessageHandler();
            inform = new InformRequestMessageHandler();
            /** */
            trapv1.MessageReceived += TrapV1Received;
            trapv2.MessageReceived += TrapV2Received;
            inform.MessageReceived += InformRequestReceived;

            if (cfg.Snmp_AgentVersion.ToLower() == "v2") // Configuracion V2
            {
                membershipProviders = new IMembershipProvider[]
			    {
                    new Version1MembershipProvider(new OctetString(cfg.Snmp_V2AgentGetComm), new OctetString(cfg.Snmp_V2AgentSetComm)),
				    new Version2MembershipProvider(new OctetString(cfg.Snmp_V2AgentGetComm), new OctetString(cfg.Snmp_V2AgentSetComm)),
                    new Version2MembershipProvider(new OctetString("private"), new OctetString("private"))
			    };
                listener = new Listener();
                handlerMappings = new HandlerMapping[]
			    {
                     new HandlerMapping(snmpVersion, "GET",     new GetMessageHandler())
                    ,new HandlerMapping(snmpVersion, "SET",     new SetMessageHandler())
                    ,new HandlerMapping(snmpVersion, "GETNEXT", new GetNextMessageHandler())
                    ,new HandlerMapping(snmpVersion, "GETBULK", new GetBulkMessageHandler())
                    ,new HandlerMapping(snmpVersion, "TRAPV2",  trapv2)
                    ,new HandlerMapping(snmpVersion, "INFORM",  inform)
                    ,new HandlerMapping("v1", "GET", new GetV1MessageHandler())
                    ,new HandlerMapping("v1", "GETNEXT", new GetNextV1MessageHandler())
			    };
            }
            else // Configuracion V3.
            {
                membershipProviders = new IMembershipProvider[]
			    {
    				new Version3MembershipProvider()
			    };
                List<User> users = new List<User>();
                foreach (string user_cif in cfg.Snmp_V3Users)
                {
                    string user_str = Utilities.EncryptionHelper.CAE_descifrar(user_cif);
                    string[] user_param = user_str.Split(new string[]{"##"}, StringSplitOptions.None);
                    if (user_param.Length == 4)
                    {
                        OctetString username = new OctetString(user_param[0]);
                        IPrivacyProvider profile = user_param[1] == "1" ? new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString(user_param[2]))) :
                            user_param[1] == "2" ? new DESPrivacyProvider(new OctetString(user_param[3]), new MD5AuthenticationProvider(new OctetString(user_param[2]))) :
                            DefaultPrivacyProvider.DefaultPair;
                        users.Add(new User(username, profile));
                    }
                }
                if (users.Count == 0)
                    users.Add(new User(new OctetString("dfnucleo"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("ulisesv5000")))));
                //User[] users1 = new User[]
                //{
                //    new User(new OctetString("nucleodf"), DefaultPrivacyProvider.DefaultPair),
                //    new User(new OctetString("nucleodfa"), new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("ulisesv5000")))),
                //    new User(new OctetString("nucleodfap"), new DESPrivacyProvider(new OctetString("privacyphrase"), new MD5AuthenticationProvider(new OctetString("ulisesv5000"))))
                //};
                UserRegistry userRegistry = new UserRegistry(users.ToArray());
                listener = new Listener() { Users = userRegistry };
                handlerMappings = new HandlerMapping[]
			        {
                         new HandlerMapping(snmpVersion, "GET",     new GetMessageHandler())
                        ,new HandlerMapping(snmpVersion, "SET",     new SetMessageHandler())
                        ,new HandlerMapping(snmpVersion, "GETNEXT", new GetNextMessageHandler())
                        ,new HandlerMapping(snmpVersion, "GETBULK", new GetBulkMessageHandler())
                        ,new HandlerMapping(snmpVersion, "TRAPV2",  trapv2)
                        ,new HandlerMapping(snmpVersion, "INFORM",  inform)
			        };
            }
            /** */
            IMembershipProvider composedMembershipProvider = new ComposedMembershipProvider(membershipProviders);
            /** */
            MessageHandlerFactory messageHandlerFactory = new MessageHandlerFactory(handlerMappings);
            /** */
            EngineGroup engineGroup = new EngineGroup();
            SnmpApplicationFactory factory = new SnmpApplicationFactory(logger, objectStore, composedMembershipProvider, messageHandlerFactory);
            _engine = new SnmpEngine(factory, listener, engineGroup);
            _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, cfg.Snmp_AgentPort));
            _engine.Listener.AddBinding(new IPEndPoint(IPAddress.Any, cfg.Snmp_AgentListenTrapPort));
            _engine.ExceptionRaised += (sender, e) => LogException<SnmpAgent>("", e.Exception);
            _closed = false;
            _store = objectStore;
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
            ///** */
            //trapv1.MessageReceived += TrapV1Received;
            //trapv2.MessageReceived += TrapV2Received;
            //inform.MessageReceived += InformRequestReceived;
        }

        /// <summary>
        /// 
        /// </summary>
		public /*static*/ void Start()
		{
			_engine.Start();
		}

        /// <summary>
        /// 
        /// </summary>
		public /*static*/ void Close()
		{
            try
            {
                TrapReceived = delegate { };

                if (_engine != null && _engine.Active)
                {
                    _engine.Stop();
                    _engine.Dispose();
                }
            }
            catch(Exception x)
            {
                LogError<SnmpAgent>("Error Cerrando Engine SNMP: " + x.Message);
            }
            finally
            {
                _engine = null;
                _closed = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="oid"></param>
        /// <param name="handler"></param>
		public /*static*/ void GetValueAsync(IPEndPoint ep, string oid, Action<ISnmpData> handler)
		{
			GetValueAsync(ep,oid,handler,2000);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="oid"></param>
        /// <param name="handler"></param>
        /// <param name="timeout"></param>
		public /*static*/ void GetValueAsync(IPEndPoint ep, string oid, Action<ISnmpData> handler, int timeout)
		{
			ThreadPool.QueueUserWorkItem(delegate 
			{
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " GetValueAsync");
                
                List<Variable> vList = new List<Variable> { new Variable(new ObjectIdentifier(oid)) };

				try
				{
					IList<Variable> value = Messenger.Get(VersionCode.V2, ep, new OctetString("public"), vList, timeout);
					if ((value.Count == 1) && (value[0].Data.TypeCode != SnmpType.NoSuchInstance))
					{
						_context.Post(delegate 
						{
							if (!_closed)
							{
								handler(value[0].Data);
							}
						}, "SnmpAgent.ValueGetted");
					}
				}
				catch (Exception x) 
                {
                    LogException<SnmpAgent>( "", x);
                }
			});
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="oid"></param>
        /// <param name="data"></param>
        /// <param name="handler"></param>
		public /*static*/ void SetValueAsync(IPEndPoint ep, string oid, ISnmpData data, Action<ISnmpData> handler)
		{
			SetValueAsync(ep, oid, data, handler, 4000);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="oid"></param>
        /// <param name="data"></param>
        /// <param name="handler"></param>
        /// <param name="timeout"></param>
		public /*static*/ void SetValueAsync(IPEndPoint ep, string oid, ISnmpData data, Action<ISnmpData> handler, int timeout)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " SetValueAsync");

                List<Variable> vList = new List<Variable> { new Variable(new ObjectIdentifier(oid), data) };

				try
				{
					IList<Variable> value = Messenger.Set(VersionCode.V2, ep, new OctetString("private"), vList, timeout);
					if ((value.Count == 1) && (value[0].Data.TypeCode != SnmpType.NoSuchInstance))
					{
						_context.Post(delegate 
						{
							if (!_closed)
							{
								handler(value[0].Data);
							}
						}, "SnmpAgent.ValueSetted");
					}
				}
				catch (Exception x) 
                {
                    LogException<SnmpAgent>( "", x);
                }
			});
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="vList"></param>
        /// <param name="handler"></param>
        public /*static*/ void GetAsync(IPEndPoint ep, IList<Variable> vList, Action<IPEndPoint, IList<Variable>> handler)
		{
			GetAsync(ep, vList, handler, 4000);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="vList"></param>
        /// <param name="handler"></param>
        /// <param name="timeout"></param>
		public /*static*/ void GetAsync(IPEndPoint ep, IList<Variable> vList, Action<IPEndPoint, IList<Variable>> handler, int timeout)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
                U5kGenericos.TraceCurrentThread(this.GetType().Name + " GetAsync");
                try
				{
					IList<Variable> results = Messenger.Get(VersionCode.V2, ep, new OctetString("public"), vList, timeout);
					_context.Post(delegate
					{
						if (!_closed)
						{
							handler(ep, results);
						}
					}, "SnmpAgent.GetResult");
				}
				catch (Exception x) 
                {
                    LogException<SnmpAgent>( "", x);               
                }
			});
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="data"></param>
        /// <param name="eps"></param>
		public static void Trap(string oid, ISnmpData data, params IPEndPoint[] eps)
		{
			Trap(new ObjectIdentifier(oid), data, eps);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oid"></param>
        /// <param name="data"></param>
        /// <param name="eps"></param>
		public static void Trap(ObjectIdentifier oid, ISnmpData data, params IPEndPoint[] eps)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
                U5kGenericos.TraceCurrentThread("SnmpAgent Trap");
                try
				{
					List<Variable> vList = new List<Variable> { new Variable(oid, data) };

					foreach (IPEndPoint ep in eps)
					{
						Messenger.SendTrapV2(0, VersionCode.V2, ep, new OctetString("private"), new ObjectIdentifier("TODO"), 0, vList);
					}
				}
				catch (Exception x)
				{
                    LogException<SnmpAgent>( "", x);
                }
			});
		}

		#region Private Members
        /// <summary>
        /// 
        /// </summary>
        //private /*static*/ Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 
        /// </summary>
		private /*static*/ SnmpEngine _engine = null;
		private /*static*/ SynchronizationContext _context;
		private /*static*/ bool _closed;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private /*static*/ void TrapV1Received(object sender, TrapV1MessageReceivedEventArgs e)
		{
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " trapv1 received");
            _context.Post(delegate
			{
				if (!_closed)
				{
					var pdu = e.TrapV1Message.Pdu();
					// if (pdu.ErrorStatus.ToInt32() == 0)
					{
						foreach (var v in pdu.Variables)
						{
							TrapReceived(e.TrapV1Message.Enterprise.ToString(), v.Id.ToString(), v.Data, e.Sender, e.Binding.Endpoint);
						}
					}
				}
			}, "SnmpAgent.TrapV1Received");
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private /*static*/ void TrapV2Received(object sender, TrapV2MessageReceivedEventArgs e)
		{
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " trapv2 received");
            _context.Post(delegate
			{
				if (!_closed)
				{
					var pdu = e.TrapV2Message.Pdu();
                    //if (pdu.ErrorStatus.ToInt32() == 0)
					{
						foreach (var v in pdu.Variables)
						{
							TrapReceived(e.TrapV2Message.Enterprise.ToString(), v.Id.ToString(), v.Data, e.Sender, e.Binding.Endpoint);
						}
					}
				}
			}, "SnmpAgent.TrapV2Received");
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private /*static*/ void InformRequestReceived(object sender, InformRequestMessageReceivedEventArgs e)
		{
            U5kGenericos.TraceCurrentThread(this.GetType().Name + " Inform request received ");
            _context.Post(delegate
			{
				if (!_closed)
				{
					var pdu = e.InformRequestMessage.Pdu();
					// if (pdu.ErrorStatus.ToInt32() == 0)
					{
						foreach (var v in pdu.Variables)
						{
							TrapReceived(e.InformRequestMessage.Enterprise.ToString(), v.Id.ToString(), v.Data, e.Sender, e.Binding.Endpoint);
						}
					}
				}
			}, "SnmpAgent.InformReceived");
		}

		#endregion
	}
}
