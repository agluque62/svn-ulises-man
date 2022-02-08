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

namespace U5kManServer.WebAppServer
{
    public class OldWebAppServer : BaseCode, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public delegate void wasRestCallBack(HttpListenerContext context, StringBuilder sb, U5kManStdData gdt);

        #region Public

        public string DefaultUrl { get; set; }
        public string DefaultDir { get; set; }
        public bool HtmlEncode { get; set; }
        public bool Enable { get; set; }
        public string DisableCause { get; set; }
        public int SyncListenerSpvPeriod { get; set; } = 5;

        /// <summary>
        /// 
        /// </summary>
        public OldWebAppServer()
        {
            SetRequestRootDirectory();
            DefaultUrl = "/index.html";
            DefaultDir = "/appweb";
            HtmlEncode = true;
            Enable = false;
            DisableCause = "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultDir"></param>
        /// <param name="defaultUrl"></param>
        public OldWebAppServer(string defaultDir, string defaultUrl, bool htmlEncode = true)
        {
            SetRequestRootDirectory();
            DefaultUrl = defaultUrl;
            DefaultDir = defaultDir;
            HtmlEncode = htmlEncode;
            Enable = false;
            DisableCause = "";
        }
        public OldWebAppServer(string rootDirectory)
        {
            Directory.SetCurrentDirectory(rootDirectory);
            DefaultUrl = "/index.html";
            DefaultDir = "/appweb";
            HtmlEncode = true;
            Enable = true;
            DisableCause = "";
        }
        public void Dispose() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="cfg"></param>
        public void Start(int port, Dictionary<string, wasRestCallBack> cfg)
        {
            LogDebug<OldWebAppServer>($"{Id} Starting WebAppServer");
            CfgRest = cfg;
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
                        lock (Locker)
                        {
                            if (Listener == null)
                            {
                                try
                                {
                                    LogDebug<OldWebAppServer>($"{Id} Starting HttpListener");
                                    Listener = new HttpListener();
                                    Listener.Prefixes.Add("http://*:" + port.ToString() + "/");

                                    /** Configurar la Autentificacion */
                                    Listener.AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous;
                                    Listener.AuthenticationSchemeSelectorDelegate = request =>
                                    {
                                        /** Todas las operaciones No GET de Usuarios no ulises se consideran inseguras... Habra que autentificarse */
                                        // return request.HttpMethod == "GET" || request.Headers["UlisesClient"] == "MTTO" ? AuthenticationSchemes.Anonymous : AuthenticationSchemes.Basic;
                                        return AuthenticationSchemes.Anonymous;
                                    };

                                    Listener.Start();
                                    Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                                    LogDebug<OldWebAppServer>($"{Id} HttpListener Started");
                                }
                                catch (Exception x)
                                {
                                    LogException<OldWebAppServer>($"{Id} ", x, false);
                                    ResetListener();
                                }
                            }
                        }
                        lastListenerTime = DateTime.Now;
                    }
                }
            });
            LogDebug<OldWebAppServer>($"{Id} WebAppServer Started");
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Stop()
        {
            lock (Locker)
            {
                LogDebug<OldWebAppServer>($"{Id} Ending WebAppServer");

                ExecutiveThreadCancel?.Cancel();
                ExecutiveThread?.Wait(TimeSpan.FromSeconds(5));
                Listener?.Close();
                Listener = null;
                CfgRest = null;

                LogDebug<OldWebAppServer>($"{Id} WebAppServer Ended");
            }
        }

        #endregion

        #region Protected

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        void GetContextCallback(IAsyncResult result)
        {
            U5kGenericos.TraceCurrentThread(this.GetType().Name);
            lock (Locker)
            {
                //U5kGenericos.SetCurrentCulture();

                ConfigCultureSet();
                if (Listener == null || Listener.IsListening == false)
                    return;
                HttpListenerContext context = Listener.EndGetContext(result);
                try
                {
                    Logrequest(context);

                    string url = context.Request.Url.LocalPath;
                    if (Enable )
                    {
                        if (url == "/") context.Response.Redirect(DefaultUrl);
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
                                url = DefaultDir + url;
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
                    else
                    {
                        // Render(Encode(DisableCause), context.Response);
                        // context.Response.StatusCode = 503;
                        // context.Response.Redirect("/noserver.html");
                        context.Response.ContentType = FileContentType(".html");
                        ProcessFile(context.Response, (DefaultDir + "/disabled.html").Substring(1), "{{cause}}", DisableCause);
                    }
                }
                catch (HttpListenerException x)
                {
                    // Si se produce una excepcion de este tipo, hay que reiniciar el LISTENER.
                    LogException<OldWebAppServer>("", x, false);
                    ResetListener();
                }
                catch (Exception x)
                {
                    LogException<OldWebAppServer>( "", x);
                    context.Response.StatusCode = 500;
                    // Todo. Render(Encode(x.Message), context.Response);
                }
                finally
                {
                    if (Listener != null && Listener.IsListening)
                    {
                        context.Response.Close();
                        Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="file"></param>
        protected void ProcessFile(HttpListenerResponse response, string file, string tag="", string valor="")
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="res"></param>
        protected void Render(string msg, HttpListenerResponse res)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
            res.ContentLength64 = buffer.Length;

            using (System.IO.Stream outputStream = res.OutputStream)
            {
                outputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entrada"></param>
        /// <returns></returns>
        protected string Encode(string entrada)
        {
            if (HtmlEncode == true)
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

        /// <summary>
        /// 
        /// </summary>
        protected void SetRequestRootDirectory()
        {
            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            string rootDirectory = Path.GetDirectoryName(exePath);
            Directory.SetCurrentDirectory(rootDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected wasRestCallBack FindRest(string url)
        {
            if (CfgRest == null)
                return null;

            if (CfgRest.ContainsKey(url))
                return CfgRest[url];

            string[] urlComp = url.Split('/');
            foreach (KeyValuePair<string, wasRestCallBack> item in CfgRest)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        Dictionary<string, string> _filetypes = new Dictionary<string, string>()
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        private string FileContentType(string ext)
        {
            if (_filetypes.ContainsKey(ext))
                return _filetypes[ext];
            return "text/text";
        }

        private void ResetListener()
        {
            LogDebug<OldWebAppServer>($"{Id} Reseting Listener");

            Listener?.Close();
            Listener = null;

            LogDebug<OldWebAppServer>($"{Id} Listener Reset");
        }
        #endregion

        #region Testing
        private void Logrequest(HttpListenerContext context)
        {
#if DEBUG
            if (context.Request.QueryString.Count > 0)
            {
                var array = (from key in context.Request.QueryString.AllKeys
                             from value in context.Request.QueryString.GetValues(key)
                             select string.Format("{0}={1}", key, value)).ToArray();

                LogDebug<OldWebAppServer>($"{Id} URL: {context.Request.Url.OriginalString}, " +
                    $"Raw URL: {context.Request.RawUrl}, " +
                    $"Query: {String.Join("##", array)}");
            }
            else
            {
                LogDebug<OldWebAppServer>($"{Id} URL: {context.Request.Url.OriginalString}, " +
                    $"Raw URL: {context.Request.RawUrl}, ");
            }
            ErrorTesting();
#endif
    }
        private void ErrorTesting()
        {
#if DEBUG
            DebuggingHelper.ThrowErrors.GetError((error) =>
            {
                if (error== DebuggingHelper.ThrowErrors.LaunchableErrors.WebListenerError)
                {
                    throw new HttpListenerException(1445, "Testing HttpListenerException");
                }
            });
#endif
        }
        #endregion

        #region Private

        string Id => $"On WebAppServer:";
        Task ExecutiveThread { get; set; } = null;
        CancellationTokenSource ExecutiveThreadCancel { get; set; } = null;
        HttpListener Listener { get; set; } = null;
        Dictionary<string, wasRestCallBack> CfgRest { get; set; } = null;
        object Locker { get; set; } = new Object();
        static DateTime StartingDate { get; set; } = DateTime.Now;

        #endregion
    }

    public delegate void wasRestCallBack(HttpListenerContext context, StringBuilder sb, U5kManStdData gdt);
    class WebServerBase : BaseCode
    {
        #region Public
        public class CfgServer
        {
            public string DefaultUrl { get; set; }
            public string DefaultDir { get; set; }
            public string LoginUrl { get; set; }
            public string LogoutUrl { get; set; }
            public bool HtmlEncode { get; set; }
            public int SessionDuration { get; set; }
            public Dictionary<string, wasRestCallBack> CfgRest { get; set; }
            public string LoginErrorTag { get; set; }
            public List<string> SecureUris { get; set; }
        }
        public int SyncListenerSpvPeriod { get; set; } = 5;
        public WebServerBase()
        {
            SetRequestRootDirectory();
            Enable = true;
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
                                        LogDebug<WebServerBase>($"{Id} Starting HttpListener");
                                        Listener = new HttpListener();
                                        Listener.Prefixes.Add("http://*:" + port.ToString() + "/");
                                        Listener.Start();
                                        Listener.BeginGetContext(new AsyncCallback(GetContextCallback), null);
                                        LogDebug<WebServerBase>($"{Id} HttpListener Started");
                                    }
                                    catch (Exception x)
                                    {
                                        LogException<WebServerBase>($"{Id} ", x, false);
                                        ResetListener();
                                    }
                                }
                                if (InactivityDetector.Tick4Idle(true) == true)
                                {
                                    SessionExpiredAt = DateTime.MinValue;
                                }
                                LogTrace<WebServerBase>($"Time to End Session => {(SessionExpiredAt - DateTime.Now)}");
                            }
                            lastListenerTime = DateTime.Now;
                        }
                    }
                });
            }
        }
        public virtual void Stop()
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
        #endregion
        #region Protected
        void GetContextCallback(IAsyncResult result)
        {
            lock (locker)
            {
                if (Listener == null || Listener.IsListening == false)
                    return;
                HttpListenerContext context = Listener.EndGetContext(result);
                Logrequest(context);
                try
                {
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
                                if (InactivityDetector.OnRestReceived(context) == false)
                                {
                                    SessionExpiredAt = DateTime.Now + TimeSpan.FromMinutes(Config.SessionDuration);
                                }
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
                catch (Exception x)
                {
                    LogException<WebServerBase>("", x);
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
        Dictionary<string, string> _filetypes = new Dictionary<string, string>()
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
        private string FileContentType(string ext)
        {
            if (_filetypes.ContainsKey(ext))
                return _filetypes[ext];
            return "text/text";
        }
        #endregion

        #region Autenticacion
        protected DateTime SessionExpiredAt { get; set; } = DateTime.Now;
        protected Action<string, Action<bool, string>> AuthenticateUser { get; set; } = null;
        protected bool Enable { get; set; }
        protected string DisableCause { get; set; }
        private bool IsAuthenticated(HttpListenerContext context)
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
                            AuthenticateUser?.Invoke(WebUtility.UrlDecode(data), (accepted, cause) =>
                            {
                                if (accepted)
                                    SessionExpiredAt = DateTime.Now + TimeSpan.FromMinutes(Config.SessionDuration);
                                else
                                {
                                    //context.Response.Redirect(Config?.LoginUrl);
                                    ProcessFile(context.Response, (Config?.DefaultDir + Config?.LoginUrl).Substring(1),
                                        Config.LoginErrorTag, Config.LoginErrorTag + cause);
                                }
                            });
                        }
                    }
                    if (SessionExpiredAt > DateTime.Now)
                        context.Response.Redirect(Config?.DefaultUrl);
                    return false;
                }
                return true;
            }
            // Control de lo que tengo que dejar pasar
            if (SessionExpiredAt > DateTime.Now || Config.SecureUris.Contains(context.Request.RawUrl))
            {
                return true;
            }
            // Redireccionar.
            context.Response.Redirect(Config?.LoginUrl);
            return false;
        }
        #endregion Autentificacion
        private void ResetListener()
        {
            LogDebug<OldWebAppServer>($"{Id} Reseting Listener");

            Listener?.Close();
            Listener = null;

            LogDebug<OldWebAppServer>($"{Id} Listener Reset");
        }
        #region Testing
        private void Logrequest(HttpListenerContext context)
        {
            LogDebug<WebServerBase>($"HTTP Request: {context.Request.HttpMethod} {context.Request.Url.OriginalString}");
            if (context.Request.QueryString.Count > 0)
            {
                var array = (from key in context.Request.QueryString.AllKeys
                             from value in context.Request.QueryString.GetValues(key)
                             select string.Format("{0}={1}", key, value)).ToArray();

                LogDebug<WebServerBase>($"Query: {String.Join("##", array)}");
            }
        }
        #endregion

        #region Private

        string Id => $"On WebAppServer:";
        HttpListener Listener { get; set; } = null;
        Object locker { get; set; } = new Object();
        CfgServer Config { get; set; }
        InactivityDetectorClass InactivityDetector { get; set; } = new InactivityDetectorClass();
        Task ExecutiveThread { get; set; } = null;
        CancellationTokenSource ExecutiveThreadCancel { get; set; } = null;

        #endregion
    }
    public class InactivityDetectorClass : IDisposable
    {
        public TimeSpan IdleTime { get; set; } = TimeSpan.FromSeconds(45);
        public bool Tick4Idle(bool modeClick = false)
        {
            if (modeClick)
            {
                var Elapsed = DateTime.Now - Control.When;
                return Elapsed > IdleTime ? true : false;
            }
            else if (LastRestReceived.Count()>0)
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
        dynamic Control { get; set; } = new { Clicks = "0", When=DateTime.Now };
        Dictionary<string, DateTime> LastRestReceived { get; set; } = new Dictionary<string, DateTime>();
    }

}

