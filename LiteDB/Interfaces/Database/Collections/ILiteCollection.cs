namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Get collection name
        /// </summary>
        string Name { get; }
    }
}
