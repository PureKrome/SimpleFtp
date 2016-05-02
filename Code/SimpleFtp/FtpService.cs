using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Shouldly;

namespace SimpleFtp
{
    public class FtpService : IFtpService
    {
        private readonly ILogger _loggingService;
        private readonly NetworkCredential _networkCredential;
        private readonly string _server;

        public FtpService(string server,
                          string username,
                          string password,
                          ILogger loggingService)
        {
            server.ShouldNotBeNullOrWhiteSpace();
            username.ShouldNotBeNullOrWhiteSpace();
            password.ShouldNotBeNullOrWhiteSpace();
            loggingService.ShouldNotBe(null);

            _loggingService = loggingService;
            _server = server;

            _networkCredential = new NetworkCredential(username, password);

            // Defaults.
            UsePasive = true;
            KeepAlive = true;
        }

        public bool UsePasive { get; set; }
        public bool EnableSsl { get; set; }
        public bool KeepAlive { get; set; }
        public bool UseBinary { get; set; }
        public IWebProxy Proxy { get; set; }
        public int? Timeout { get; set; }

        /// <summary>
        /// Uploads some simple string data to an Ftp Server.
        /// </summary>
        /// <param name="data">The content to upload.</param>
        /// <param name="fileName">The destination filename.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task UploadAsync(string data,
                                      string fileName)
        {
            data.ShouldNotBeNullOrEmpty();
            fileName.ShouldNotBeNullOrEmpty();

            // Copy the string to a memory inputStream because the Ftp low level plumbing
            // requires streams.
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                await UploadAsync(memoryStream, fileName);
            }
        }

        // ** NOTE: Referece to a nice SO answer about FTP uploading: http://stackoverflow.com/a/25016741/30674
        /// <summary>
        /// Uploads some simple string data to an Ftp Server.
        /// </summary>
        /// <param name="inputStream">A stream of the content to upload.</param>
        /// <param name="fileName">The destination filename.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task UploadAsync(Stream inputStream,
                                      string fileName)
        {
            _loggingService.Verbose("UploadAsync");

            inputStream.ShouldNotBeNull();
            fileName.ShouldNotBeNullOrEmpty();

            // Prepend the ftp scheme if one isn't provided.
            var uri = !_server.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
                          ? $"ftp://{_server}/{fileName}"
                          : $"{_server}/{fileName}";
            var destinationUri = new Uri(uri);

            _loggingService.Debug($"Ftp Uploading string data -> destination: {destinationUri.AbsoluteUri}. Data size: {inputStream.Length}.");

            var ftpWebRequest = (FtpWebRequest) WebRequest.Create(destinationUri);
            ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile; // This is the classic 'STOR' command.
            ftpWebRequest.Credentials = _networkCredential;
            ftpWebRequest.UsePassive = UsePasive;
            ftpWebRequest.EnableSsl = EnableSsl;
            ftpWebRequest.KeepAlive = KeepAlive;
            ftpWebRequest.UseBinary = UseBinary;
            if (Proxy != null)
            {
                ftpWebRequest.Proxy = Proxy;
            }
            if (Timeout.HasValue)
            {
                ftpWebRequest.Timeout = Timeout.Value;
            }

            // Now start writing the request to the ftp server.
            using (var ftpStream = await ftpWebRequest.GetRequestStreamAsync())
            {
                await inputStream.CopyToAsync(ftpStream);

                _loggingService.Information($"Successfully Ftp-uploaded {ftpStream.Length} characters to {destinationUri.AbsoluteUri}.");
            }
        }
    }
}