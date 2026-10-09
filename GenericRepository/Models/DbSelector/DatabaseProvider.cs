using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.DbSelector
{
    public enum DatabaseProvider
    {
        // Relational
        SqlServer = 1,
        PostgreSql = 2,
        MySql = 3,
        MariaDb = 4,
        Sqlite = 5,
        Oracle = 6,

        // NoSQL
        MongoDb = 7,
        Redis = 8,
        Cassandra = 9,
        Couchbase = 10,
        RavenDb = 11
    }
}
