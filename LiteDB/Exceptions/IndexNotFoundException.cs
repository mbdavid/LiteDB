namespace LiteDB
{
    /// <summary>
    /// A specific exception when a query didnt found an index
    /// </summary>
    internal class IndexNotFoundException : LiteException
    {
        public string Collection { get; set; }
        public string Field { get; set; }

        public IndexNotFoundException(string collection, string field)
            : base("Index not found")
        {
            this.Collection = collection;
            this.Field = field;
        }
    }
}