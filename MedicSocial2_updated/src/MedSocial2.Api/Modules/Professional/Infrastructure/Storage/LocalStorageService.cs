using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Professional.Infrastructure.Storage
{
    public class LocalStorageService : IDocumentStorageService
    {
        private readonly LocalOptions _opts;
        private readonly string _basePath;

        public LocalStorageService(IOptions<DocumentStorageOptions> options)
        {
            _opts = options.Value.Local;
            _basePath = Path.GetFullPath(_opts.BasePath, AppContext.BaseDirectory);
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> SaveAsync(byte[] content, string fileName, string tenantId)
        {
            var tenantDir = Path.Combine(_basePath, tenantId);
            Directory.CreateDirectory(tenantDir);
            var filePath = Path.Combine(tenantDir, Guid.NewGuid().ToString() + "_" + fileName);
            await File.WriteAllBytesAsync(filePath, content);
            return filePath;
        }

        public Task<Stream> OpenReadAsync(string storagePath)
        {
            return Task.FromResult<Stream>(File.OpenRead(storagePath));
        }

        public Task DeleteAsync(string storagePath)
        {
            if (File.Exists(storagePath))
                File.Delete(storagePath);
            return Task.CompletedTask;
        }
    }
}