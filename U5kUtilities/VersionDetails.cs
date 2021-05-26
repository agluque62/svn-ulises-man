using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Utilities
{
    public class VersionDetails_old
    {
        public class VersionDataFileItem
        {
            public string Path { get; set; }
            public string Date { get; set; }
            public string Size { get; set; }
            public string MD5 { get; set; }
        }
        public class VersionDataComponent
        {
            public string Name { get; set; }
            public List<VersionDataFileItem> Files = new List<VersionDataFileItem>();
        }
        public class VersionData
        {
            public string Server { get; set; }
            public string Version { get; set; }
            public string Fecha { get; set; }
            public List<VersionDataComponent> Components = new List<VersionDataComponent>();
        }

        VersionData version;
        public VersionDetails_old(string filepath)
        {
            version = JsonConvert.DeserializeObject<VersionData>(File.ReadAllText(@filepath));
            version.Server = System.Environment.MachineName;
            foreach (VersionDataComponent component in version.Components)
            {
                foreach (VersionDataFileItem fileitem in component.Files)
                {
                    if (File.Exists(fileitem.Path))
                    {
                        FileInfo fi = new FileInfo(fileitem.Path);
                        fileitem.Date = fi.LastWriteTime.ToShortDateString();
                        fileitem.Size = fi.Length.ToString();
                        fileitem.MD5 = EncryptionHelper.FileMd5Hash(fileitem.Path);
                    }
                    else
                    {
                        fileitem.Date = fileitem.Size = fileitem.MD5 = "File not found";
                    }
                }
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(version);
        }
    }

    public class VersionDetails
    {
        public class VersionDataFileItem
        {
            public string Path { get; set; }
            public string Date { get; set; }
            public string Size { get; set; }
            public string MD5 { get; set; }
        }
        public class VersionDataComponent
        {
            public string Name { get; set; }
            public List<VersionDataFileItem> Files = new List<VersionDataFileItem>();
        }
        public class VersionData
        {
            public string Server { get; set; }
            public string Version { get; set; }
            public string Fecha { get; set; }
            public List<VersionDataComponent> Components = new List<VersionDataComponent>();
            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
        }
        /** 20180409. Para poder modificar RUNTIME los ficheros de versiones */
        public VersionData version;
        public VersionDetails(string filepath, bool updateFields = true)
        {
            if (File.Exists(@filepath))
            {
                version = JsonConvert.DeserializeObject<VersionData>(File.ReadAllText(@filepath));

                if (updateFields)
                {
                    version.Server = System.Environment.MachineName;
                    foreach (VersionDataComponent component in version.Components)
                    {
                        foreach (VersionDataFileItem fileitem in component.Files)
                        {
                            if (File.Exists(fileitem.Path))
                            {
                                FileInfo fi = new FileInfo(fileitem.Path);
                                fileitem.Date = fi.LastWriteTime.ToShortDateString();
                                fileitem.Size = fi.Length.ToString();
                                fileitem.MD5 = EncryptionHelper.FileMd5Hash(fileitem.Path);
                            }
                            else
                            {
                                fileitem.Date = fileitem.Size = fileitem.MD5 = "File not found";
                            }
                        }
                    }
                }
            }
            else
            {
                // No existe fichero de versiones...
                version = new VersionData();
                version.Version = "";
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(version, Formatting.Indented);
        }
        public static VersionDetails SwVersions
        {
            get
            {
                var vbase = new VersionDetails("versiones.json");
                var prxversion = JsonHelper.JsonRead("SipProxyPBXVersions.json");
                if (prxversion != null)
                {
                    var prxcomponents = prxversion["components"];
                    if (prxcomponents != null)
                    {
                        vbase.version.Components.Add(new VersionDetails.VersionDataComponent()
                        {
                            Name = "UV5K-Sip Proxy",
                            Files = prxcomponents.Select(c => new VersionDetails.VersionDataFileItem()
                            {
                                Path = Path.GetFileName(c["path"].ToString()),
                                Date = c["date"].ToString(),
                                Size = c["size"].ToString(),
                                MD5 = c["md5"].ToString()
                            }).ToList()
                        });
                        return vbase;
                    }
                }
                vbase.version.Components.Add(new VersionDetails.VersionDataComponent()
                {
                    Name = "UV5K-Sip Proxy",
                    Files = new List<VersionDetails.VersionDataFileItem>()
                {
                    new VersionDetails.VersionDataFileItem()
                    {
                        Path = "SipProxyVersions.json",
                        Date = "",
                        Size = "",
                        MD5 = "File not found or corrupted"
                    }
                }
                });
                return vbase;
            }
        }
    }
}
