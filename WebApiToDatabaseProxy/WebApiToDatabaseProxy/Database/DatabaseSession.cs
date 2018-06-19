using System;
using System.Data;

namespace WebApiToDatabaseProxy.Database
{
    public class DatabaseSession : IDatabaseSession
    {
        private Func<string, IDbConnection> createConnectionDelegate;
        private string connectionString;

        public DatabaseSession(string connectionString, Func<string, IDbConnection> createConnection)
        {
            this.connectionString = connectionString;
            this.createConnectionDelegate = createConnection;
        }

        public IDbConnection GetConnection()
        {
            return this.createConnectionDelegate(this.connectionString);
        }
    }
}