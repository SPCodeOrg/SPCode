using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace SPCode.Utils
{
    public class FTP
    {
        private readonly string _host;
        private readonly string _user;
        private readonly string _pass;

        public string ErrorMessage;

        public FTP(string host, string user, string password) { _host = host; _user = user; _pass = password; }

        public void Upload(string remoteFile, string localFile)
        {
            var requestUri = new UriBuilder(_host) { Path = remoteFile }.Uri;

            if (requestUri.Scheme == "sftp")
            {
                var connectionInfo = new ConnectionInfo(requestUri.Host, requestUri.Port == -1 ? 22 : requestUri.Port, _user, new PasswordAuthenticationMethod(_user, _pass));
                using var sftp = new SftpClient(connectionInfo);
                sftp.Connect();

                using (var stream = File.OpenRead(localFile))
                {
                    sftp.UploadFile(stream, remoteFile, true);
                }

                sftp.Disconnect();
            }
            else
            {
                using var client = new WebClient
                {
                    Credentials = new NetworkCredential(_user, _pass)
                };
                client.UploadFile(requestUri, WebRequestMethods.Ftp.UploadFile, localFile);
            }
        }

        /// <summary>
        /// Returns whether the specified credentials in the Configs Window can be used for a valid FTP/SFTP connection.
        /// </summary>
        public async Task<bool> TestConnection()
        {
            var requestUri = new UriBuilder(_host).Uri;
            var success = true;

            if (requestUri.Scheme == "sftp")
            {
                var connectionInfo = new ConnectionInfo(requestUri.Host, requestUri.Port == -1 ? 22 : requestUri.Port, _user, new PasswordAuthenticationMethod(_user, _pass));
                using var sftp = new SftpClient(connectionInfo);
                sftp.OperationTimeout = TimeSpan.FromSeconds(5);
                try
                {
                    await Task.Run(() => sftp.Connect());
                }
                catch (SshAuthenticationException)
                {
                    success = false;
                    ErrorMessage = "Invalid credentials!";
                }
                catch (Exception ex)
                {
                    success = false;
                    ErrorMessage = ex.Message;
                }
                finally
                {
                    sftp.Disconnect();
                    sftp.Dispose();
                }
            }
            else
            {
                var requestDir = WebRequest.Create(requestUri);
                requestDir.Credentials = new NetworkCredential(_user, _pass);
                requestDir.Timeout = 5000;
                try
                {
                    var response = await requestDir.GetResponseAsync();
                }
                catch
                {
                    success = false;
                }
            }
            return success;
        }
    }
}