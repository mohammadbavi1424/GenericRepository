using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlIndexField
    {
        public string FieldName { get; set; } = default!;

        public int SortOrder { get; set; }

        public bool Descending { get; set; }
    }
}
