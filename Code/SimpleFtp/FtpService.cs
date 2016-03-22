using System;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using Shouldly;

namespace SimpleFtp
{
    public class FtpService : IFtpService
    {
        private readonly ILogger _loggingService;
        private readonly string _server;
        private readonly Lazy<WebClient> _webClient;

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

            _webClient = new Lazy<WebClient>(() => new WebClient
            {
                Credentials = new NetworkCredential(username, password)
            });
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
            _loggingService.Verbose("UploadAsync");

            data.ShouldNotBeNullOrEmpty();
            fileName.ShouldNotBeNullOrEmpty();

            // Prepend the ftp scheme if one isn't provided.
            var uri = !_server.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
                          ? $"ftp://{_server}/{fileName}"
                          : $"{_server}/{fileName}";
            var destination = new Uri(uri);

            _loggingService.Debug($"Ftp Uploading string data -> destination: {destination.AbsoluteUri}. Data size: {data.Length}.");

            await _webClient.Value.UploadStringTaskAsync(destination, "STOR", data);

            _loggingService.Information($"Successfully Ftp-uploaded {data.Length} characters to {destination.AbsoluteUri}.");
        }

        public void Dispose()
        {
            if (_webClient.IsValueCreated)
            {
                _webClient.Value?.Dispose();
            }
        }
    }
}