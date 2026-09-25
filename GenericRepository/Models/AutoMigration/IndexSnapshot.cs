using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration
{
    public sealed class IndexSnapshot
    {
        public string Name { get; set; } = string.Empty;

        public List<string>? Columns { get; set; } = new();

        public bool? IsUnique { get; set; } = false;

        public bool[]? IsDescending { get; set; } = Array.Empty<bool>();

        public string? Filter { get; set; }
    }

}
