using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace Professional.Infrastructure.Storage
{
    public class SshStorageService : IDocumentStorageService
    {
        private readonly SshOptions _opts;

        public SshStorageService(IOptions<DocumentStorageOptions> options)
        {
            _opts = options.Value.Ssh;
        }

        public Task DeleteAsync(string storagePath)
        {
            // simple scp remove via ssh
            using var client = new SshClient(_opts.Host, _opts.Port, _opts.Username, _opts.Password);
            client.Connect();
            client.RunCommand($"rm {storagePath}");
            client.Disconnect();
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string storagePath)
        {
            var client = new ScpClient(_opts.Host, _opts.Port, _opts.Username, _opts.Password);
            client.Connect();
            var ms = new MemoryStream();
            client.Download(storagePath, ms);
            ms.Position = 0;
            client.Disconnect();
            return Task.FromResult<Stream>(ms);
        }

        public Task<string> SaveAsync(byte[] content, string fileName, string tenantId)
        {
            var targetPath = Path.Combine(_opts.RemotePath, tenantId, Guid.NewGuid().ToString() + "_" + fileName);
            using var client = new ScpClient(_opts.Host, _opts.Port, _opts.Username, _opts.Password);
            client.Connect();
            using var ms = new MemoryStream(content);
            client.Upload(ms, targetPath);
            client.Disconnect();
            return Task.FromResult(targetPath);
        }
    }
}