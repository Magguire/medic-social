using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;

namespace Professional.Infrastructure.Storage
{
    public class S3StorageService : IDocumentStorageService
    {
        private readonly S3Options _opts;
        private readonly IAmazonS3 _client;

        public S3StorageService(IOptions<DocumentStorageOptions> options)
        {
            _opts = options.Value.S3;
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(_opts.Region) };
            _client = new AmazonS3Client(_opts.AccessKey, _opts.SecretKey, config);
        }

        public async Task<string> SaveAsync(byte[] content, string fileName, string tenantId)
        {
            var key = $"{tenantId}/{Guid.NewGuid()}_{fileName}";
            using var ms = new MemoryStream(content);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = ms,
                Key = key,
                BucketName = _opts.BucketName
            };
            var util = new TransferUtility(_client);
            await util.UploadAsync(uploadRequest);
            return key;
        }

        public async Task<Stream> OpenReadAsync(string storagePath)
        {
            var response = await _client.GetObjectAsync(_opts.BucketName, storagePath);
            return response.ResponseStream;
        }

        public async Task DeleteAsync(string storagePath)
        {
            await _client.DeleteObjectAsync(_opts.BucketName, storagePath);
        }
    }
}