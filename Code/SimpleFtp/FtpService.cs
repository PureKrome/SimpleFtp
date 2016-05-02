using System;
using System.Diagnostics;
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
        public delegate void UploadProgressHandler(UploadProgressArgs e);

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
            FireUploadProgressEventEveryXBytes = 1024*1024; // 1MB.
        }

        public bool UsePasive { get; set; }
        public bool EnableSsl { get; set; }
        public bool KeepAlive { get; set; }
        public bool UseBinary { get; set; }
        public IWebProxy Proxy { get; set; }
        public int? Timeout { get; set; }

        /// <summary>
        /// How many bytes need to uploaded for an UploadProgress event is fired?
        /// </summary>
        /// <remarks>The default is: 1Mb.</remarks>
        public long FireUploadProgressEventEveryXBytes { get; set; }


        /// <summary>
        /// Deletes a remote file from the ftp server.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task DeleteAsync(string fileName)
        {
            fileName.ShouldNotBeNullOrWhiteSpace();

            var destinationUri = GenerateDestinationUri(fileName);

            // Does this file exist?
            if (await CheckIfFileExistsAsync(destinationUri))
            {
                await DeleteFileAsync(destinationUri);
            }
        }

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

            var destinationUri = GenerateDestinationUri(fileName);

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

            var stopwatch = Stopwatch.StartNew();

            // Now start writing the request to the ftp server.
            using (var ftpStream = await ftpWebRequest.GetRequestStreamAsync())
            {
                await CopyToWithEvent(inputStream, ftpStream, FireUploadProgressEventEveryXBytes);

                _loggingService.Debug("Closing service....");
                ftpStream.Close();
                _loggingService.Debug("Closed..");
            }

            stopwatch.Stop();

            _loggingService.Information(
                $"Successfully Ftp-uploaded {inputStream.Length.ToString("N0")} characters to {destinationUri.AbsoluteUri} in {stopwatch.Elapsed.ToString("g")}.");
        }

        public event UploadProgressHandler OnUploadProgress;

        private Uri GenerateDestinationUri(string fileName)
        {
            fileName.ShouldNotBeNullOrWhiteSpace();

            // Prepend the ftp scheme if one isn't provided.
            var uri = !_server.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
                          ? $"ftp://{_server}/{fileName}"
                          : $"{_server}/{fileName}";

            return new Uri(uri);
        }

        private async Task<bool> CheckIfFileExistsAsync(Uri destinationUri)
        {
            destinationUri.ShouldNotBeNull();

            _loggingService.Debug($"Ftp checking if the remote file exists -> destination: {destinationUri.AbsoluteUri}.");

            var ftpWebRequest = (FtpWebRequest) WebRequest.Create(destinationUri);
            ftpWebRequest.Credentials = _networkCredential;
            ftpWebRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            try
            {
                using ((FtpWebResponse) await ftpWebRequest.GetResponseAsync())
                {
                    _loggingService.Information($"Finished checking remote file which exists!");
                }
            }
            catch
            {
                _loggingService.Information($"Remote file doesn't exist - nothing to delete.");
                return false;
            }


            return true;
        }

        private async Task DeleteFileAsync(Uri destinationUri)
        {
            destinationUri.ShouldNotBeNull();

            _loggingService.Debug($"Ftp Deleteting a remote file -> destination: {destinationUri.AbsoluteUri}.");

            var ftpWebRequest = (FtpWebRequest) WebRequest.Create(destinationUri);
            ftpWebRequest.Credentials = _networkCredential;
            ftpWebRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            using ((FtpWebResponse) await ftpWebRequest.GetResponseAsync())
            {
                _loggingService.Information($"Finished deleting remote file -> {destinationUri.AbsoluteUri}.");
            }
        }
        
        private async Task CopyToWithEvent(Stream source,
                                           Stream destination,
                                           long fireEventAfterCopingBytes)
        {
            source.ShouldNotBeNull();
            destination.ShouldNotBeNull();

            var eventId = 0;
            long totalBytesCopied = 0;
            long currentBytesCopied = 0;
            var buffer = new byte[4*1024];
            int bytesRead;

            _loggingService.Debug($" #### Source length: { source.Length}");

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead);

                // Do we need to fire an event?
                if (OnUploadProgress == null)
                {
                    continue;
                }

                totalBytesCopied += bytesRead;
                currentBytesCopied += bytesRead;

                // Partial progress report -or- final report.   
                if (currentBytesCopied >= fireEventAfterCopingBytes ||
                    totalBytesCopied == source.Length)
                {
                    // We have copied more-or-equal bytes to fire off an event.
                    // So lets tell anyone the current event!
                    OnUploadProgress(new UploadProgressArgs(++eventId, totalBytesCopied, currentBytesCopied, source.Length));

                    currentBytesCopied = 0; // Reset.
                }

                if (totalBytesCopied >= source.Length)
                {
                    await destination.FlushAsync();
                }
            }
        }
    }
}