namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Get collection info and stats
        /// </summary>
        public CollectionInfo GetCollectionInfo()
        {
            return _engine.Stats(_name);
        }
    }
}