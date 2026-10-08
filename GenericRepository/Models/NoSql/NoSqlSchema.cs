using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlSchema
    {
        public string DatabaseName { get; set; } = default!;

        public List<NoSqlCollection> Collections { get; set; } = [];
    }
}
