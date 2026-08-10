namespace Professional.Infrastructure.Storage
{
    public class DocumentStorageOptions
    {
        public string Provider { get; set; } = "Local"; // Local, S3, Ssh
        public LocalOptions Local { get; set; } = new LocalOptions();
        public S3Options S3 { get; set; } = new S3Options();
        public SshOptions Ssh { get; set; } = new SshOptions();
    }

    public class LocalOptions
    {
        // base path relative or absolute
        public string BasePath { get; set; } = "Documents";
    }

    public class S3Options
    {
        public string BucketName { get; set; } = "";
        public string Region { get; set; } = "us-east-1";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
    }

    public class SshOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string RemotePath { get; set; } = "";
    }
}