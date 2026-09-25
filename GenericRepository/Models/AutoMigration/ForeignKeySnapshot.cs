using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration
{
    public sealed class ForeignKeySnapshot
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Columns { get; set; } = new();

        public string PrincipalSchema { get; set; } = "dbo";

        public string PrincipalTable { get; set; } = string.Empty;

        public List<string> PrincipalColumns { get; set; } = new();

        public DeleteBehavior DeleteBehavior { get; set; }
    }

}
