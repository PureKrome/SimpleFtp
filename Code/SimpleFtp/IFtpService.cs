using System;
using System.IO;
using System.Threading.Tasks;

namespace SimpleFtp
{
    public interface IFtpService
    {
        /// <summary>
        /// Deletes a remote file from the ftp server.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task DeleteAsync(string fileName);

        /// <summary>
        /// Uploads some simple string data to an Ftp Server.
        /// </summary>
        /// <param name="data">The content to upload.</param>
        /// <param name="fileName">The destination filename.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task UploadAsync(string data,
                         string fileName);

        /// <summary>
        /// Uploads some simple string data to an Ftp Server.
        /// </summary>
        /// <param name="inputStream">A stream of the content to upload.</param>
        /// <param name="fileName">The destination filename.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task UploadAsync(Stream inputStream,
                         string fileName);
    }
}