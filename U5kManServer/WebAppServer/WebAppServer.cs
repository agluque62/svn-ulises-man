using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Globalization;
using System.Net;

using NucleoGeneric;
using Utilities;
using U5kBaseDatos;
using static U5kManServer.WebAppServer.HttpServer;
using Lextm.SharpSnmpLib.Messaging;
using System.Web.Services.Description;
using System.Runtime.Remoting.Contexts;
using static U5kManServer.U5kManService;

namespace U5kManServer.WebAppServer
{

    public delegate void wasRestCallBack(HttpListenerContext context, StringBuilder sb, U5kManStdData gdt);
    public interface IHttpServer 
    {
        bool IsEnabled { get; set; }
        Action<string, Action<bool, string, string>> AuthenticateUser { get; set; }
        void Start(int port, CfgServer cfg);
        void Stop();
        void Logout(HttpListenerContext context);
    }
    public class CfgServer
    {
        public string DefaultUrl { get; set; }
        public string DefaultDir { get; set; }
        public string LoginUrl { get; set; }
        public string LogoutUrl { get; set; }
        public bool HtmlEncode { get; set; }
        //public int SessionDuration { get; set; }
        public Dictionary<string, wasRestCallBack> CfgRest { get; set; }
        public string LoginErrorTag { get; set; }
        public List<string> SecureUris { get; set; }
    }
    public class InactivityDetectorClass : IDisposable
    {
        public TimeSpan IdleTime { get; set; } = TimeSpan.FromMinutes(2);
        public bool Tick4Idle(bool modeClick = false)
        {
            if (modeClick)
            {
                var Elapsed = DateTime.Now - Control.When;
                return Elapsed > IdleTime ? true : false;
            }
            else if (LastRestReceived.Count() > 0)
            {
                var LastReceivedOn = LastRestReceived.Values.Max();
                var Elapsed = DateTime.Now - LastReceivedOn;
                // true => Detectada inactividad.
                return Elapsed > IdleTime ? true : false;
            }
            return false;
        }
        public bool OnRestReceived(string rest)
        {
            var LastKey = Key;
            LastRestReceived[rest] = DateTime.Now;
            LastRestReceived = LastRestReceived.OrderByDescending(n => n.Value).Take(3).ToDictionary(n => n.Key, n => n.Value);
            return Key == LastKey; // true => Detectada inactividad. No se refresca el tiempo de expiracion de sesión.
        }
        public bool OnRestReceived(HttpListenerContext contex)
        {
            var lastCount = Control.Clicks as string;
            var notCount = contex.Request.Headers["Click-counter"];
            if (notCount != null)
            {
                Control = new { Clicks = notCount, When = DateTime.Now };
            }
            var currentCount = Control.Clicks as string;
            return lastCount == currentCount;
        }
        public void Dispose()
        {
        }
        string Key => string.Join("", LastRestReceived.Keys.OrderBy(n => n));
        //string Clicks { get; set; } = "";
        dynamic Control { get; set; } = new { Clicks = "0", When = DateTime.Now };
        Dictionary<string, DateTime> LastRestReceived { get; set; } = new Dictionary<string, DateTime>();
    }
    public class SessionsControl : BaseCode
    {
        public class Session
        {
            public string Key { get; set; } = "";
            public DateTime Expires { get; set; }
            public string ClicksCount { get; set; }
            public DateTime LastActivityTime { get; set; }
            public TimeSpan Time2Renew { get; set; }
            public override string ToString()
            {
                return $"([{Key}],[{When(Expires)}],[{ClicksCount}],[{When(LastActivityTime)}]; ";
            }
            string When(DateTime date) => date.TimeOfDay.ToString(@"hh\:mm\:ss");
            public string UserId => Key?.Split('#').FirstOrDefault() ?? "???";
        }
        public int MaxSessions { get; set; } = Properties.u5kManServer.Default.WebMaxSessions;
        public TimeSpan IdleTime { get; set; } = TimeSpan.FromSeconds(45);
        public void GetAccess(TimeSpan Duration, string userid, string userprofile, Action<bool, Cookie> respond)
        {
            Cleanup();
            if (SessionsCount() < MaxSessions)
            {
                var key = $"{userid}#{userprofile}#{GenetareKey()}";
                lock (Locker)
                {
                    Sessions.Add(new Session()
                    {
                        Key = key,
                        Expires = DateTime.Now + Duration,
                        LastActivityTime = DateTime.Now,
                        Time2Renew = Duration,
                        ClicksCount = "0"
                    });
                }
                respond(true, new Cookie()
                {
                    Name = "login",
                    Value = key,
                    Expires = DateTime.Now + Duration
                });
            }
            else
            {
                respond(false, null);
            }
        }
        public bool GetPermission(HttpListenerRequest request, Action<Cookie> respond)
        {
            Cleanup();
            var keyAccess = request.Cookies.Cast<Cookie>().Where(c => c.Name == "login").Select(c => c.Value).FirstOrDefault();
            lock (Locker)
            {
                var session = Sessions.Where(s => s.Key == keyAccess).FirstOrDefault();
                if (session != null)
                {
                    var newClickCount = request.Headers["Click-counter"];
                    if (newClickCount != null)
                    {
                        if (newClickCount != session.ClicksCount)
                        {
                            session.Expires = DateTime.Now + session.Time2Renew;
                            session.ClicksCount = newClickCount;
                            respond(new Cookie()
                            {
                                Name = "login",
                                Value = session.Key,
                                Expires = DateTime.Now + session.Time2Renew
                            });
                        }
                    }
                    session.LastActivityTime = DateTime.Now;
                    return true;
                }
            }
            return false;
        }
        public void Tick4Idle() => Cleanup();
        public void Logout(HttpListenerRequest request, Action<string> user)
        {
            Cleanup();
            var keyAccess = request.Cookies.Cast<Cookie>().Where(c => c.Name == "login").Select(c => c.Value).FirstOrDefault();
            lock (Locker)
            {
                Session ses = Sessions.Where(s => s.Key == keyAccess).Select(s => s).FirstOrDefault();
                user(ses?.UserId);
                // Limpia la tabla...
                Sessions = Sessions.Where(s => s.Key != keyAccess).Select(s => s).ToList();
            }
        }
        public override string ToString()
        {
            return $"Sesiones: {String.Join("", Sessions.Select(s => s.ToString()))}";
        }
        void Cleanup()
        {
            List<Session> Expired = null;
            List<Session> Inactives = null;
            lock (Locker)
            {
                Expired = Sessions.Where(s => s.Expires <= DateTime.Now).Select(s => s).ToList();
                /** Limpia las Sesiones que han expirado */
                Sessions = Sessions.Where(s => s.Expires > DateTime.Now).Select(s => s).ToList();

                Inactives = Sessions.Where(s => s.LastActivityTime + IdleTime <= DateTime.Now).Select(s => s).ToList();
                /** Limpia las Sesiones Inactivas */
                Sessions = Sessions.Where(s => s.LastActivityTime + IdleTime > DateTime.Now).Select(s => s).ToList();
            }
            Task.Run(() =>
            {
                Expired.ForEach((s) =>
                {
                    var msg = $"La Sesion de {s.UserId} ha expirado";
                    RecordEvent<HttpServer>(DateTime.Now, eIncidencias.IGRL_NBXMNG_EVENT, eTiposInci.TEH_SISTEMA, "MTTO",
                        new object[] { msg, "", "", "", "", "", "", "" });
                });
                Inactives.ForEach((s) =>
                {
                    var msg = $"La Sesion de {s.UserId} se ha cancelado por inactividad.";
                    RecordEvent<HttpServer>(DateTime.Now, eIncidencias.IGRL_NBXMNG_EVENT, eTiposInci.TEH_SISTEMA, "MTTO",
                        new object[] { msg, "", "", "", "", "", "", "" });
                });
            });
        }
        int SessionsCount()
        {
            lock (Locker)
            {
                return Sessions.Count;
            }
        }
        string GenetareKey(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var randomString = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[RandomGenerator.Next(s.Length)]).ToArray());
            return randomString;
        }
        private List<Session> Sessions { get; set; } = new List<Session>();
        private object Locker { get; set; } = new object();
        private Random RandomGenerator { get; set; } = new Random((int)DateTime.Now.Ticks);
    }

    public abstract class HttpServerBase: BaseCode
    {
        protected void ProcessFile(HttpListenerResponse response, string file, string tag = "", string valor = "")
        {
            if (tag != "")
            {
                string str = File.ReadAllText(file).Replace(tag, valor);
                byte[] content = Encoding.ASCII.GetBytes(str);
                response.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                byte[] content = File.ReadAllBytes(file);
                response.OutputStream.Write(content, 0, content.Length);
            }
            response.Close();
        }
        protected void Render(string msg, HttpListenerResponse res)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
            res.ContentLength64 = buffer.Length;

            using (System.IO.Stream outputStream = res.OutputStream)
            {
                outputStream.Write(buffer, 0, buffer.Length);
            }
        }
        protected string Encode(string entrada)
        {
            if (Config?.HtmlEncode == true)
            {
                char[] chars = entrada.ToCharArray();
                StringBuilder result = new StringBuilder(entrada.Length + (int)(entrada.Length * 0.1));

                foreach (char c in chars)
                {
                    int value = Convert.ToInt32(c);
                    if (value > 127)
                        result.AppendFormat("&#{0};", value);
                    else
                        result.Append(c);
                }

                return result.ToString();
            }
            return entrada;
        }
        protected void SetRequestRootDirectory()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string rootDirectory = Path.GetDirectoryName(exePath);
            Directory.SetCurrentDirectory(rootDirectory);
        }
        protected wasRestCallBack FindRest(string url)
        {
            if (Config?.CfgRest == null)
                return null;

            if (Config.CfgRest.ContainsKey(url))
                return Config?.CfgRest[url];

            string[] urlComp = url.Split('/');
            foreach (KeyValuePair<string, wasRestCallBack> item in Config?.CfgRest)
            {
                string[] keyComp = item.Key.Split('/');
                if (keyComp.Count() != urlComp.Count())
                    continue;

                bool encontrado = true;
                for (int index = 0; index < urlComp.Count(); index++)
                {
                    if (urlComp[index] != keyComp[index] && keyComp[index] != "*")
                        encontrado = false;
                }

                if (encontrado == true)
                    return item.Value;
            }
            return null;
        }
        #region Autenticacion        
        protected string DisableCause { get; set; }
        protected SessionsControl Sessions { get; set; } = new SessionsControl();
        protected bool IsAuthenticated(HttpListenerContext context)
        {
            // Control de los Post de Login
            if (context.Request.RawUrl.ToLower().Contains(Config?.LoginUrl.ToLower()))
            {
                if (context.Request.HttpMethod == "POST")
                {
                    // Autenticar.
                    if (!context.Request.HasEntityBody)
                    {
                        context.Response.Redirect(Config?.LoginUrl);
                        return false;
                    }
                    /** Leer los datos asociados */
                    using (System.IO.Stream body = context.Request.InputStream) // here we have data
                    {
                        using (System.IO.StreamReader reader = new System.IO.StreamReader(body, context.Request.ContentEncoding))
                        {
                            var data = reader.ReadToEnd();
                            // Llamar a la rutina de AUT de la aplicacion.
                            //AuthenticateUser?.Invoke(WebUtility.UrlDecode(data), (accepted, userId, userPrf) =>
                            IsUserAuthenticated(WebUtility.UrlDecode(data), (accepted, userId, userPrf) =>
                            {
                                if (accepted)
                                {
                                    // Miro si hay sesiones.
                                    var sessionDuration = TimeSpan.FromMinutes(U5kManService.cfgSettings.WebInactivityTimeout);
                                    Sessions.GetAccess(sessionDuration, userId, userPrf, (haysesiones, cookie) =>
                                    {
                                        if (haysesiones)
                                        {
                                            Task.Run(() =>
                                            {
                                                var msg = $"Usuario {userId}, {userPrf}, Inicia session";
                                                RecordEvent<HttpServer>(DateTime.Now, eIncidencias.IGRL_NBXMNG_EVENT, eTiposInci.TEH_SISTEMA, "MTTO",
                                                    new object[] { msg, "", "", "", "", "", "", "" });
                                            });
                                            LogTrace<HttpServer>($"Set Cookie => {cookie}=>{cookie.Expires}");
                                            context.Response.Cookies.Add(cookie);
                                            context.Response.Redirect(Config?.DefaultUrl);
                                        }
                                        else
                                        {
                                            ProcessFile(context.Response, (Config?.DefaultDir + Config?.LoginUrl).Substring(1),
                                                Config.LoginErrorTag, Config.LoginErrorTag + "Maximo numero de Sesiones alcanzado");
                                        }
                                    });
                                }
                                else
                                {
                                    ProcessFile(context.Response, (Config?.DefaultDir + Config?.LoginUrl).Substring(1),
                                        Config.LoginErrorTag, Config.LoginErrorTag + userId);
                                }
                            });
                        }
                    }
                    return false;
                }
                return true;
            }
            // Control de lo que tengo que dejar pasar
            if (ContainsSecureUri(context.Request))
            {
                return true;
            }
            else
            {
                var allowed = Sessions.GetPermission(context.Request, (cookie) =>
                {
                    context.Response.Cookies.Add(cookie);
                });
                if (allowed)
                {
                    return true;
                }
            }
            // Redireccionar.
            context.Response.Redirect(Config?.LoginUrl);
            return false;
        }
        protected bool ContainsSecureUri(HttpListenerRequest request)
        {
            var path = request.RawUrl.Split('?').FirstOrDefault();
            return path != null && Config.SecureUris.Contains(path);
        }
        protected abstract void IsUserAuthenticated(string queryData, Action<bool, string, string> response);
        #endregion Autentificacion
        protected void ProcessRequest(HttpListenerContext context)
        {
            Logrequest(context);
            if (IsAuthenticated(context))
            {
                string url = context.Request.Url.LocalPath;
                if (url == "/") context.Response.Redirect(Config?.DefaultUrl);
                else
                {
                    wasRestCallBack cb = FindRest(url);
                    if (cb != null)
                    {
                        StringBuilder sb = new System.Text.StringBuilder();
                        // TODO. De momento no cojo el semaforo....
                        GlobalServices.GetWriteAccess((gdt) =>
                        {
                            cb(context, sb, gdt);
                        }, false);
                        context.Response.ContentType = FileContentType(".json");
                        Render(Encode(sb.ToString()), context.Response);
                    }
                    else
                    {
                        url = Config?.DefaultDir + url;
                        if (url.Length > 1 && File.Exists(url.Substring(1)))
                        {
                            /** Es un fichero lo envio... */
                            string file = url.Substring(1);
                            string ext = Path.GetExtension(file).ToLowerInvariant();

                            context.Response.ContentType = FileContentType(ext);
                            ProcessFile(context.Response, file);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                        }
                    }
                }
            }
        }
        protected void Logrequest(HttpListenerContext context)
        {
            LogDebug<HttpsServer>($"HTTP Request: {context.Request.HttpMethod} {context.Request.Url.OriginalString}");
            if (context.Request.QueryString.Count > 0)
            {
                var array = (from key in context.Request.QueryString.AllKeys
                             from value in context.Request.QueryString.GetValues(key)
                             select string.Format("{0}={1}", key, value)).ToArray();

                LogDebug<HttpsServer>($"Query: {String.Join("##", array)}");
            }
        }

        protected Dictionary<string, string> _filetypes = new Dictionary<string, string>()
        {
            {".css","text/css"},
            {".jpeg","image/jpg"},
            {".jpg","image/jpg"},
            {".htm","text/html"},
            {".html","text/html"},
            {".ico","image/ico"},
            {".js","text/json"},
            {".json","text/json"},
            {".txt","text/text"},
            {".bmp","image/bmp"}
        };
        protected string FileContentType(string ext)
        {
            if (_filetypes.ContainsKey(ext))
                return _filetypes[ext];
            return "text/text";
        }
        protected Object locker { get; set; } = new Object();
        protected CfgServer Config { get; set; }
    }

    public class HttpServer : HttpServerBase, IHttpServer
    {
        #region Public
        public bool IsEnabled { get; set; }
        public int SyncListenerSpvPeriod { get; set; } = 5;
        public Action<string, Action<bool, string, string>> AuthenticateUser { get; set; } = null;
        public HttpServer()
        {
            SetRequestRootDirectory();
            IsEnabled = true;
            DisableCause = "";
        }
        public void Start(int port, CfgServer cfg)
        {
            lock (locker)
            {
                if (Listener != null)
                    Stop();
                Config = cfg;

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
                            // Supervisar la disponibilidad del Listener.
                            lock (locker)
                            {
                                if (Listener == null)
                                {
                                    try
                                    {
                                        LogDebug<HttpServer>($"{Id} Starting HttpListener");
                                        Listener = new HttpListener();
                                        Listener.Prefixes.Add("http://*:" + port.ToString() + "/");
                                        //Listener.Prefixes.Add("https://*:" + port.ToString() + "/");
                                        Listener.Start();
                                        Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                                        LogDebug<HttpServer>($"{Id} HttpListener Started");
                                    }
                                    catch (Exception x)
                                    {
                                        LogException<HttpServer>($"{Id} ", x, false);
                                        ResetListener();
                                    }
                                }
                                Sessions.Tick4Idle();
                                LogTrace<HttpServer>($"{Sessions}");
                            }
                            lastListenerTime = DateTime.Now;
                        }
                    }
                });
            }
        }
        public void Stop()
        {
            lock (locker)
            {
                if (Listener != null)
                {
                    Listener.Close();
                    ExecutiveThreadCancel?.Cancel();
                    ExecutiveThread?.Wait(TimeSpan.FromSeconds(5));
                    Listener = null;
                    Config = null;
                }
            }
        }
        public void Logout(HttpListenerContext context)
        {
            Sessions.Logout(context.Request, (user) =>
            {
                Task.Run(() =>
                {
                    var msg = $"Usuario {user}, Finaliza session";
                    RecordEvent<HttpServer>(DateTime.Now, eIncidencias.IGRL_NBXMNG_EVENT, eTiposInci.TEH_SISTEMA, "MTTO",
                        new object[] { msg, "", "", "", "", "", "", "" });
                });
            });
        }
        #endregion

        #region private methods
        void GetContextCallback(IAsyncResult result)
        {
            lock (locker)
            {
                if (Listener == null || Listener.IsListening == false)
                    return;
                
                ConfigCultureSet();

                HttpListenerContext context = Listener.EndGetContext(result);

                try
                {
                    ProcessRequest(context);
                }
                catch (Exception x)
                {
                    LogException<HttpServer>("", x);
                    context.Response.StatusCode = 500;
                }
                finally
                {
                    context.Response.Close();
                    if (Listener != null && Listener.IsListening)
                        Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                }
            }
        }
        void ResetListener()
        {
            LogDebug<HttpServer>($"{Id} Reseting Listener");

            Listener?.Close();
            Listener = null;

            LogDebug<HttpServer>($"{Id} Listener Reset");
        }
        protected override void IsUserAuthenticated(string queryData, Action<bool, string, string> response) => 
            AuthenticateUser?.Invoke(queryData, response);
        #endregion

        #region Private properties

        string Id => $"On WebAppServer:";
        HttpListener Listener { get; set; } = null;
        Task ExecutiveThread { get; set; } = null;
        CancellationTokenSource ExecutiveThreadCancel { get; set; } = null;
        #endregion
    }

    public class HttpsServer : HttpServerBase, IHttpServer
    {
        #region public attr
        public bool httpOnly { get; set; } = false;
        public bool IsEnabled { get; set; }
        public Action<string, Action<bool, string, string>> AuthenticateUser { get; set; } = null;
        #endregion
        public int SyncListenerSpvPeriod { get; set; } = 5;

        public HttpsServer()
        {
            SetRequestRootDirectory();
            IsEnabled = true;
            disableCause = "";
            cts = new CancellationTokenSource();
            httpListener = new HttpListener();
            httpsListener = new HttpListener();
        }
        public void Start(int port, CfgServer cfg)
        {
            LogInfo<HttpsServer>($"Starting HttpsServer...");
            basePort = port;
            Config = cfg;
            httpListenerTask = Task.Run(() => HttpProcess());
            httpsListenerTask = Task.Run(() => HttpsProcess());
            supervisionTask = Task.Run(() => SupervisionProcess());
            LogInfo<HttpsServer>($"HttpsServer started on port {port}, httpOnly = {httpOnly}.");
        }
        public void Stop()
        {
            LogInfo<HttpsServer>($"Stopping HttpsServer...");
            cts?.Cancel();
            httpListener?.Abort();
            httpsListener?.Abort();
            Task.WaitAll(httpListenerTask, httpsListenerTask, supervisionTask);
            LogInfo<HttpsServer>($"HttpsServer Stopped.");
        }
        public void Logout(HttpListenerContext context)
        {
            Sessions.Logout(context.Request, (user) =>
            {
                Task.Run(() =>
                {
                    var msg = $"Usuario {user}, Finaliza session";
                    RecordEvent<HttpServer>(DateTime.Now, eIncidencias.IGRL_NBXMNG_EVENT, eTiposInci.TEH_SISTEMA, "MTTO",
                        new object[] { msg, "", "", "", "", "", "", "" });
                });
            });
        }
        protected override void IsUserAuthenticated(string queryData, Action<bool, string, string> response) =>
            AuthenticateUser?.Invoke(queryData, response);

        #region private methods

        void HttpProcess()
        {
            LogInfo<HttpsServer>($"Starting Listening on port {basePort} for HTTP");
            HttpListenerContext context = null;
            httpListener.Prefixes.Add($"http://*:{basePort}/");
            httpListener.Start();
            while (cts.IsCancellationRequested == false)
            {
                try
                {
                    context = httpListener.GetContext();
                    if (httpOnly == false)
                    {
                        var url = new UriBuilder(context.Request.Url) { Port = basePort + 1, Scheme="https" };
                        context?.Response?.Redirect(url.ToString());
                    }
                    else
                    {
                        ProcessRequest(context);
                    }
                }
                catch (Exception x)
                {
                    LogException<HttpsServer>("From HTTP", x);
                }
                finally
                {
                    context?.Response?.Close();
                    context = null;
                }
            }
            LogInfo<HttpsServer>($"Listening on port {basePort} for HTTP stopped");
        }
        void HttpsProcess()
        {
            LogInfo<HttpsServer>($"Starting Listening on port {basePort+1} for HTTPS");
            HttpListenerContext context = null;
            httpsListener.Prefixes.Add($"https://*:{basePort+1}/");
            httpsListener.Start();
            while (cts.IsCancellationRequested == false)
            {
                try
                {
                    context = httpsListener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception x)
                {
                    LogException<HttpsServer>("From HTTPS", x);
                }
                finally
                {
                    context?.Response?.Close();
                    context = null;
                }
            }
            LogInfo<HttpsServer>($"Listening on port {basePort+1} for HTTPS stopped");
        }
        void SupervisionProcess()
        {
            DateTime lastListenerTime = DateTime.MinValue;
            DateTime lastRefreshTime = DateTime.MinValue;
            while (cts.IsCancellationRequested == false)
            {
                Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                if (DateTime.Now - lastListenerTime >= TimeSpan.FromSeconds(SyncListenerSpvPeriod))
                {
                    Sessions.Tick4Idle();
                    LogTrace<HttpsServer>($"{Sessions}");
                }
            }
        }

        #endregion

        #region private attr

        int basePort = default;
        string disableCause { get; set; }
        HttpListener httpListener = null;
        Task httpListenerTask = null;
        HttpListener httpsListener = null;
        Task httpsListenerTask = null;
        Task supervisionTask = null;
        CancellationTokenSource cts = null;

        #endregion
    }
}

