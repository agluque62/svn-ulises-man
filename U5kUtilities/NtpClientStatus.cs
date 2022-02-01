using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Win32;

namespace Utilities
{
    /*public*/ class NtpClientStatus : IDisposable
    {
        #region Public
        protected enum NtpClient { Windows = 0, Meinberg = 1, Unknow = -1 }
        public NtpClientStatus(int client = 0)
        {
            Cliente = client == 0 ? NtpClient.Windows : client == 1 ? NtpClient.Meinberg : NtpClient.Unknow;
        }
        public void Dispose()
        {
        }
        public List<string> Status
        {
            get
            {
                try
                {
                    switch (Cliente)
                    {
                        case NtpClient.Windows:
                            return W32tmStatus;
                        case NtpClient.Meinberg:
                            return MeinbergNtpClientStatus;
                        default:
                            return ErrorStatus("Not implemented");
                    }
                }
                catch (Exception x)
                {
                    return ErrorStatus(x.Message);
                }
            }
        }
        public string UrlServer(string client_response = "")
        {
            switch (Cliente)
            {
                case NtpClient.Windows:
                    return WindowsNtpServerUrl;
                case NtpClient.Meinberg:
                    return MeinbergNtpServerUrl(client_response);
                default:
                    return String.Format("Ntp Client {0} unknown", (int)Cliente);
            }
        }

        #endregion

        #region Private
        private NtpClient Cliente { get; set; }
        private List<string> W32tmStatus
        {
            get
            {
                List<string> status = new List<string>();
                ProcessStartInfo psi = new ProcessStartInfo("w32tm", " /query /peers")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                string Text = proc.StandardOutput.ReadToEnd();

                using (StringReader reader = new StringReader(Text))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line != String.Empty)
                            status.Add(Normalizar(line));
                    }
                }

                return status;
            }
        }
        private List<string> MeinbergNtpClientStatus
        {
            get
            {
                List<string> status = new List<string>();
                try
                {
                    string Text = ExecuteMeinberCommand();

                    using (StringReader reader = new StringReader(Text))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line != String.Empty)
                                status.Add(Normalizar(line));
                        }
                    }
                }
                catch (Exception x)
                {
                    return ErrorStatus(x.Message);
                }

                return status;
            }
        }

        private String WindowsNtpServerUrl
        {
            get
            {
                RegistryKey pRegKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\W32Time\Parameters");
                Object obj = pRegKey.GetValue("NtpServer");
                String[] ntpSrvs = ((string)obj).Split(',');
                return ntpSrvs[0];
            }
        }
        private String MeinbergNtpServerUrl(string client_response)
        {
            try
            {
                string Text = client_response == "" ? ExecuteMeinberCommand() : client_response;
                List<string> Servidores = new List<string>();

                using (StringReader reader = new StringReader(Text))
                {
                    string line;
                    int nline = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (nline > 1 && line != String.Empty)
                        {
                            bool active = line[0] == '*';
                            string server = line.Substring(1).Split(new char[] { ' ' }).ToList().ElementAt(0);
                            Servidores.Add((active ? "a" : "i") + server);
                        }
                        nline++;
                    }
                }
                Servidores = Servidores.OrderBy(item => item).ToList();
                return Servidores.Count == 0 ? "No NTP Server" : Servidores.ElementAt(0).Substring(1);

            }
            catch (Exception )
            {
                return String.Format("No NTP Meinberg Client");
            }
        }
        private List<string> ErrorStatus(string error)
        {
            List<string> status = new List<string>()
                {
                    String.Format("ntpc {1} Error: {0}", error, Cliente) 
                };
            return status;
        }
        private String Normalizar(String inputString)
        {
            // var inputString = "Mañana será otro día";
            var normalizedString = inputString.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            for (int i = 0; i < normalizedString.Length; i++)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(normalizedString[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalizedString[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
        private String ExecuteMeinberCommand()
        {
#if DEBUG
            string Text = "     remote           refid      st t when poll reach   delay   offset  jitter\r\n"
                          + "==============================================================================\r\n"
                          + " 192.168.0.129  172.24.90.12     6 u  234  256  377    0.977   82.219  26.097\r\n"
                          + "*DF1501   172.24.90.12     6 u  234  256  377    0.977   82.219  26.097\r\n";
            return Text;
#else
            ProcessStartInfo psi = new ProcessStartInfo("ntpq", " -p")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            return proc.StandardOutput.ReadToEnd();
#endif
        }
        #endregion
    }

    public class NtpMeinbergClientInfo : IDisposable
    {
        #region Clases Publicas
        public enum NtpServerClass { Unknow, NoConnected, Candidate, SystemPeer, FalseTicker }
        public class NtpServerInfo
        {
            public NtpServerClass Class { get; set; }
            public string Remote { get; set; }
            public string Refif { get; set; }
            public int Stratum { get; set; }
            public string Type { get; set; }
            public int When { get; set; }
            public int Poll { get; set; }
            public string Reach { get; set; }
            public double Delay { get; set; }
            public double Offset { get; set; }
            public double Jitter { get; set; }
            public bool Valid => (Class != NtpServerClass.Unknow);
            public string Ip
            {
                get
                {
                    var ip = IPHelper.IsIpv4(Remote) ? Remote :
                        IPHelper.IsIpv4(Refif) ? Refif : "127.0.0.1";
                    return ip;
                }
            }
            public NtpServerInfo()
            {
                SetToDefault();
            }
            public NtpServerInfo(string line)
            {
                if (line?.Length > 0)
                {
                    Class = DecodeClass(line.ElementAt(0));
                    if (Valid)
                    {
                        var strData = GeneralHelper.NormalizeWhiteSpace(line.Substring(1)).Split(' ');
                        if (strData.Length == 10)
                        {
                            char[] charsToTrim = { '.' };
                            Remote = strData[0].Trim(charsToTrim);
                            Refif = strData[1].Trim(charsToTrim);
                            Stratum = DecodeStratum(strData[2]);
                            Type = strData[3];
                            When = DecodeInt(strData[4]);
                            Poll = DecodeInt(strData[5]);
                            Reach = strData[6];
                            Delay = DecodeDouble(strData[7]);
                            Offset = DecodeDouble(strData[8]);
                            Jitter = DecodeDouble(strData[9]);
                        }
                        else
                        {
                            SetToDefault();
                        }
                    }
                    else
                    {
                        SetToDefault();
                    }
                }
                else
                {
                    SetToDefault();
                }
            }
            private void SetToDefault()
            {
                Class = NtpServerClass.Unknow;
                Remote = default;
                Refif = default;
                Stratum = 16;
                Type = default;
                When = Poll = 0;
                Reach = default;
                Delay = Offset = Jitter = 0;
            }
            private NtpServerClass DecodeClass(char uClass)
            {
                switch (uClass)
                {
                    case '*':
                        return NtpServerClass.SystemPeer;
                    case '+':
                        return NtpServerClass.Candidate;
                    case '-':
                        return NtpServerClass.FalseTicker;
                    case ' ':
                    case 'x':
                        return NtpServerClass.NoConnected;
                    default:
                        return NtpServerClass.Unknow;
                }
            }
            private int DecodeStratum(string input)
            {
                var value = DecodeInt(input);
                if (value < 0 || value > 16)
                    Class = NtpServerClass.Unknow;
                return value;
            }
            private int DecodeInt(string input)
            {
                if (int.TryParse(input, out int res))
                    return res;
                Class = NtpServerClass.Unknow;
                return default;
            }
            private double DecodeDouble(string input)
            {
                var provider = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
                //NumberFormatInfo provider = new NumberFormatInfo();
                //provider.NumberDecimalSeparator = ".";
                //provider.NumberGroupSeparator = ",";
                
                if (double.TryParse(input, System.Globalization.NumberStyles.Any, provider, out double res))
                    return res;
                Class = NtpServerClass.Unknow;
                return default;
            }
        }
        #endregion
        const string CommandErrorKey = "ntpc Error";
        const string FirstLineKey1 = "refid";
        const string FirstLineKey2 = "offset";
        const string FirstLineKey3 = "jitter";
        const string SecondLineKey = "==========";
        const string LineSeparator = "##";
        #region Public Members
        public NtpMeinbergClientInfo(string formattedClientResponse)
        {
            string[] separatingStrings = { LineSeparator };
            var clientLines = formattedClientResponse?.Split(separatingStrings, StringSplitOptions.None).ToList();
            ProcessClientResponse(clientLines ?? InfoFromCommandLine);
        }
        public NtpMeinbergClientInfo(List<string> clientResponse = null)
        {
            ProcessClientResponse(clientResponse ?? InfoFromCommandLine);
        }
        public void Dispose()
        {
        }
        public string MainUrl
        {
            get
            {
                var systempeer = ServersInfo.Where(s => s.Class == NtpServerClass.SystemPeer).FirstOrDefault();
                var candidate = ServersInfo.Where(s => s.Class == NtpServerClass.Candidate).FirstOrDefault();
                var disconnected = ServersInfo.Where(s => s.Class == NtpServerClass.NoConnected).FirstOrDefault();
                var mainUrl = systempeer != default ? systempeer.Ip :
                    candidate != default ? candidate.Ip :
                    disconnected != default ? disconnected.Ip : "No NTP Server";
                return mainUrl;
            }
        }
        public bool Connected
        {
            get
            {
                var systempeer = ServersInfo.Where(s => s.Class == NtpServerClass.SystemPeer);
                var candidate = ServersInfo.Where(s => s.Class == NtpServerClass.Candidate);
                var connected = systempeer.Count() > 0 || candidate.Count() > 0;
                return connected;
            }
        }
        public double Offset
        {
            get
            {
                var systempeer = ServersInfo.Where(s => s.Class == NtpServerClass.SystemPeer).FirstOrDefault();
                var candidate = ServersInfo.Where(s => s.Class == NtpServerClass.Candidate).FirstOrDefault();
                var offset = systempeer != default ? systempeer.Offset :
                    candidate != default ? candidate.Offset : 0;
                return offset;
            }
        }
        public string LastClientResponse => string.Join(LineSeparator, LastClientResponseProcessed?.ToArray());
        #endregion Public Members
        protected List<string> InfoFromCommandLine
        {
            get
            {
                List<string> status = new List<string>();
                try
                {
                    string Text = ExecuteMeinberCommand();
                    using (StringReader reader = new StringReader(Text))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line != String.Empty)
                                status.Add(Normalize(line));
                        }
                    }
                }
                catch (Exception x)
                {
                    return ErrorStatus(x.Message);
                }

                return status;
            }
        }
        private String Normalize(String inputString)
        {
            var normalizedString = inputString.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            for (int i = 0; i < normalizedString.Length; i++)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(normalizedString[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalizedString[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
        private List<string> ErrorStatus(string error)
        {
            return new List<string>() { $"{CommandErrorKey} {error}" };
        }
        private String ExecuteMeinberCommand()
        {
#if DEBUG1
            string Text = "     remote           refid      st t when poll reach   delay   offset  jitter\r\n"
                          + "==============================================================================\r\n"
                          + " 192.168.0.129 ( 172.24.90.12     6 u  234  256  377    0.977   82.219  26.097\r\n"
                          + "*DF1501  ( 172.24.90.12     6 u  234  256  377    0.977   82.219  26.097\r\n";
            return Text;
#else
            var filePath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/ntp/bin/ntpq.exe";
            ProcessStartInfo psi = new ProcessStartInfo(filePath, " -p")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            return proc.StandardOutput.ReadToEnd();
#endif
        }
        private void ProcessClientResponse(List<string> response)
        {
            LastClientResponseProcessed = response;
            ServersInfo = new List<NtpServerInfo>();
            response.ForEach(line =>
            {
                DecodeLine(line, (info) =>
                {
                    ServersInfo.Add(info);
                });
            });
        }
        private void DecodeLine(string line, Action<NtpServerInfo> addServer)
        {
            // Filtra Errores de Procesado y las dos primeras líneas.
            string[] check = new string[] { CommandErrorKey, FirstLineKey1, FirstLineKey2, FirstLineKey3, SecondLineKey };
            var discard = check.Where(item => line.ToLower().Contains(item.ToLower())).ToList().Count > 0;
            if (discard) return;
            // Decodifica datos del servidor, y filtra los erróneos...
            var info = new NtpMeinbergClientInfo.NtpServerInfo(line);
            if (info.Valid)
                addServer(info);
        }
        #region Private Members
        List<string> LastClientResponseProcessed { get; set; }
        List<NtpServerInfo> ServersInfo { get; set; }
        #endregion
    }
}
