using System;
using System.Threading.Tasks;

namespace SimpleFtp
{
    public interface IFtpService : IDisposable
    {
        /// <summary>
        /// Uploads some simple string data to an Ftp Server.
        /// </summary>
        /// <param name="data">The content to upload.</param>
        /// <param name="fileName">The destination filename.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task UploadAsync(string data,
                         string fileName);
    }
}