using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlPrimaryKey
    {
        public List<NoSqlKeyField> Fields { get; set; } = [];

        public NoSqlKeyType Type { get; set; }
    }
}
