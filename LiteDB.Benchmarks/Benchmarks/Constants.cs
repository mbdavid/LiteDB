namespace LiteDB.Benchmarks.Benchmarks
{
    internal static class Constants
    {
        internal static class DatabaseNames
        {
            public const string QUERIES = @"Query.db";
            public const string INSERTION = @"Insertion.db";
            public const string DELETION = @"Deletion.db";
        }

        internal static class Categories
        {
            internal const string DATA_GEN = nameof(DATA_GEN);
            internal const string QUERIES = nameof(QUERIES);
            internal const string INSERTION = nameof(INSERTION);
            internal const string DELETION = nameof(DELETION);
        }
    }
}