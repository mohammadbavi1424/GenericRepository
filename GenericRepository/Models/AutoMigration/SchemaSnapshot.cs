namespace GenericRepository.Models.AutoMigration
{
    public sealed class SchemaSnapshot
    {
        public List<TableSnapshot> Tables { get; set; } = new();
    }


}
