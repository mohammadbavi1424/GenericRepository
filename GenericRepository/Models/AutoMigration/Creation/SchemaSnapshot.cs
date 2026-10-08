namespace GenericRepository.Models.AutoMigration.Creation
{
    public sealed class SchemaSnapshot
    {
        public List<TableSnapshot> Tables { get; set; } = new();
    }


}
