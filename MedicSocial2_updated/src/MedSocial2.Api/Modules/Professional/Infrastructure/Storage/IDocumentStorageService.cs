using System.IO;
using System.Threading.Tasks;

namespace Professional.Infrastructure.Storage
{
    /// <summary>
    /// Abstraction over where professional documents are stored.
    /// Returns a path or URL that can later be used to retrieve the document.
    /// </summary>
    public interface IDocumentStorageService
    {
        Task<string> SaveAsync(byte[] content, string fileName, string tenantId);
        Task<Stream> OpenReadAsync(string storagePath);
        Task DeleteAsync(string storagePath);
    }
}