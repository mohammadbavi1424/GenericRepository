using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration
{
    public sealed class PrimaryKeySnapshot
    {
        public string? Name { get; set; }

        public List<string> Columns { get; set; } = new();
    }

}
