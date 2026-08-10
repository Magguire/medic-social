using System.Data;

namespace Shared.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}