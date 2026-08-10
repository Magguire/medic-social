using System;
using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;
using MySqlConnector;
using Oracle.ManagedDataAccess.Client;

namespace Shared.Data
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly DatabaseProvider _provider;
        private readonly string _connectionString;

        public DbConnectionFactory(DatabaseProvider provider, IConfiguration config)
        {
            _provider = provider;
            _connectionString = config.GetConnectionString(provider.ToString()) ?? throw new InvalidOperationException("Connection string not found");
        }

        public IDbConnection CreateConnection()
        {
            IDbConnection? conn = _provider switch
            {
                DatabaseProvider.SqlServer => CreateSqlServerConnection(),
                DatabaseProvider.Sqlite => CreateSqliteConnection(),
                DatabaseProvider.Postgres => CreatePostgresConnection(),
                DatabaseProvider.MySql => CreateMySqlConnection(),
                DatabaseProvider.Oracle => CreateOracleConnection(),
                DatabaseProvider.InMemory => throw new NotSupportedException("InMemory provider cannot create IDbConnection instances"),
                _ => throw new NotSupportedException($"Provider {_provider} not supported")
            };
            return conn;
        }

        private IDbConnection CreateSqlServerConnection()
        {
            var factory = Microsoft.Data.SqlClient.SqlClientFactory.Instance;
            var conn = factory.CreateConnection();
            conn.ConnectionString = _connectionString;
            return conn;
        }

        private IDbConnection CreateSqliteConnection()
        {
            var factory = Microsoft.Data.Sqlite.SqliteFactory.Instance;
            var conn = factory.CreateConnection();
            conn.ConnectionString = _connectionString;
            return conn;
        }

        private IDbConnection CreatePostgresConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            return conn;
        }

        private IDbConnection CreateMySqlConnection()
        {
            var conn = new MySqlConnection(_connectionString);
            return conn;
        }

        private IDbConnection CreateOracleConnection()
        {
            var conn = new OracleConnection(_connectionString);
            return conn;
        }
    }
}