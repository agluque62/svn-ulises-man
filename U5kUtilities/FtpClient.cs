using System;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace Utilities
{

    public class FtpClient : IDisposable
    {
        private string host = null;
        private string user = null;
        private string pass = null;
        private FtpWebRequest ftpRequest = null;
        private FtpWebResponse ftpResponse = null;
        private Stream ftpStream = null;
        private int bufferSize = 2048;
        private int Timeout { get; set; }

        /* Construct Object */
        public FtpClient(string hostIP, string userName, string password, int timeout=10000)
        {
            host = hostIP; user = userName; pass = password;
            Timeout = timeout;
        }

        /* IDisposable */
        public void Dispose() { }

        /* Download File */
        protected void Download(string remoteFile, Stream localFileStream, Action<bool,Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + remoteFile);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                /* **/
                ftpRequest.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                ftpRequest.Timeout = Timeout;

                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Get the FTP Server's Response Stream */
                ftpStream = ftpResponse.GetResponseStream();
                /* Open a File Stream to Write the Downloaded File */
                // FileStream localFileStream = new FileStream(localFile, FileMode.Create);
                /* Buffer for the Downloaded Data */
                byte[] byteBuffer = new byte[bufferSize];
                int bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
                /* Download the File by Writing the Buffered Data Until the Transfer is Complete */
                try
                {
                    while (bytesRead > 0)
                    {
                        localFileStream.Write(byteBuffer, 0, bytesRead);
                        bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                respond(true, null);
            }
            catch (Exception ex)
            {
                // throw ex;
                respond(false, ex);
            }
            finally
            {
                /* Resource Cleanup */
                //localFileStream.Close();
                ftpStream?.Close();
                ftpResponse?.Close();
                ftpRequest = null;
            }
            return;
        }
        /// <summary>
        /// Lo salva en un fichero local.
        /// </summary>
        /// <param name="remoteFile"></param>
        /// <param name="localFile"></param>
        public void Download(string remoteFile, string localFile, Action<bool, Exception> respond)
        {
            using (var fs = new FileStream(localFile, FileMode.Create))
            {
                Download(remoteFile, fs, respond);
            }
        }
        /// <summary>
        /// Lo salva en un 'string'
        /// </summary>
        /// <param name="remoteFile"></param>
        /// <param name="localstring"></param>
        public void Download(string remoteFile, Action<bool, string, Exception> respond)
        {
            using (var stream = new MemoryStream())
            {
                Download(remoteFile, stream, (res, x) =>
                {
                    respond(res, res ? System.Text.Encoding.UTF8.GetString(stream.ToArray()) : "", res ? null : x);
                });
            }
        }
        /* Upload File */
        public void Upload(string remoteFile, string localFile, Action<bool, Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + remoteFile);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                /* Establish Return Communication with the FTP Server */
                ftpStream = ftpRequest.GetRequestStream();
                /* Open a File Stream to Read the File for Upload */
                FileStream localFileStream = new FileStream(localFile, FileMode.Create);
                /* Buffer for the Downloaded Data */
                byte[] byteBuffer = new byte[bufferSize];
                int bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
                /* Upload the File by Sending the Buffered Data Until the Transfer is Complete */
                try
                {
                    while (bytesSent != 0)
                    {
                        ftpStream.Write(byteBuffer, 0, bytesSent);
                        bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                /* Resource Cleanup */
                localFileStream.Close();
                ftpStream.Close();
                ftpRequest = null;

                respond(true, null);
            }
            catch (Exception ex)
            {
                respond(false, ex);
                //throw ex;
            }
            return;
        }
        /* Delete File */
        public void Delete(string deleteFile, Action<bool, Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + deleteFile);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Resource Cleanup */
                ftpResponse.Close();
                ftpRequest = null;

                respond(true, null);
            }
            catch (Exception ex)
            {
                respond(false, ex);
                //throw ex;
            }
            return;
        }
        /* Rename File */
        public void Rename(string currentFileNameAndPath, string newFileName, Action<bool, Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + currentFileNameAndPath);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.Rename;
                /* Rename the File */
                ftpRequest.RenameTo = newFileName;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Resource Cleanup */
                ftpResponse.Close();
                ftpRequest = null;

                respond(true, null);
            }
            catch (Exception ex)
            {
                respond(false, ex);
                //throw ex;
            }
            return;
        }
        /* Create a New Directory on the FTP Server */
        public void CreateDirectory(string newDirectory, Action<bool, Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + newDirectory);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Resource Cleanup */
                ftpResponse.Close();
                ftpRequest = null;

                respond(true, null);
            }
            catch (Exception ex)
            {
                respond(false, ex);
                //throw ex;
            }
            return;
        }
        /* Get the Date/Time a File was Created */
        public void GetFileCreatedDateTime(string fileName, Action<bool, string, Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + fileName);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Establish Return Communication with the FTP Server */
                ftpStream = ftpResponse.GetResponseStream();
                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);
                /* Store the Raw Response */
                string fileInfo = null;
                /* Read the Full Response Stream */
                try
                {
                    fileInfo = ftpReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
                ftpRequest = null;
                /* Return File Created Date Time */
                respond(true, fileInfo, null);
                //return fileInfo;
            }
            catch (Exception ex)
            {
                respond(false, "", ex);
                throw ex;
            }
        }
        /* Get the Size of a File */
        public void GetFileSize(string fileName, Action<bool, string, Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + fileName);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Establish Return Communication with the FTP Server */
                ftpStream = ftpResponse.GetResponseStream();
                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);
                /* Store the Raw Response */
                string fileInfo = null;
                /* Read the Full Response Stream */
                try
                {
                    while (ftpReader.Peek() != -1)
                    {
                        fileInfo = ftpReader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
                ftpRequest = null;
                /* Return File Size */
                respond(true, fileInfo, null);
                //return fileInfo;
            }
            catch (Exception ex)
            {
                respond(false, "", ex);
                //throw ex;
            }
        }
        /* List Directory Contents File/Folder Name Only */
        public void DirectoryListSimple(string directory, Action<bool, string[], Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + directory);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Establish Return Communication with the FTP Server */
                ftpStream = ftpResponse.GetResponseStream();
                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);
                /* Store the Raw Response */
                string directoryRaw = null;
                /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
                try
                {
                    while (ftpReader.Peek() != -1)
                    {
                        directoryRaw += ftpReader.ReadLine() + "|";
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
                ftpRequest = null;
                /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
                try 
                { 
                    string[] directoryList = directoryRaw.Split("|".ToCharArray());
                    respond(true, directoryList, null);
                    //return directoryList; 
                }
                catch (Exception ex) 
                {
                    throw ex; 
                }
            }
            catch (Exception ex)
            {
                respond(false, new string[] { "" }, ex);
                //throw ex;
            }
        }
        /* List Directory Contents in Detail (Name, Size, Created, etc.) */
        public void DirectoryListDetailed(string directory, Action<bool, string[], Exception> respond)
        {
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + directory);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = false;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                /* Establish Return Communication with the FTP Server */
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                /* Establish Return Communication with the FTP Server */
                ftpStream = ftpResponse.GetResponseStream();
                /* Get the FTP Server's Response Stream */
                StreamReader ftpReader = new StreamReader(ftpStream);
                /* Store the Raw Response */
                string directoryRaw = null;
                /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
                try
                {
                    while (ftpReader.Peek() != -1)
                    {
                        directoryRaw += ftpReader.ReadLine() + "|";
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                /* Resource Cleanup */
                ftpReader.Close();
                ftpStream.Close();
                ftpResponse.Close();
                ftpRequest = null;
                /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
                try
                {
                    string[] directoryList = directoryRaw.Split("|".ToCharArray());
                    respond(true, directoryList, null);
                    //return directoryList;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                respond(false, new string[] { }, ex);
                throw ex;
            }
        }
    }
}