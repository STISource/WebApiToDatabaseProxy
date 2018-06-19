using System.Collections.Generic;
using System.Data;

namespace WebApiToDatabaseProxy.Database
{
    public interface IDatabaseSession
    {
        IDbConnection GetConnection();                 
    }
}
