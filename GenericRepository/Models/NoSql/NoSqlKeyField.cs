using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlKeyField
    {
        public string FieldName { get; set; } = default!;

        public NoSqlKeyRole Role { get; set; }
    }
}
