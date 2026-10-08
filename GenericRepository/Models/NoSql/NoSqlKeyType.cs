using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public enum NoSqlKeyType
    {
        DocumentId = 1,

        PartitionKey = 2,

        CompositeKey = 3
    }

    public enum NoSqlKeyRole
    {
        Key = 1,

        PartitionKey = 2,

        SortKey = 3,

        ClusteringKey = 4
    }

}
