using BenchmarkDotNet.Attributes;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global

namespace LiteDB.Benchmarks.Benchmarks.Base
{
    public abstract class DatabaseBenchmarkBase : BenchmarkBase
    {
        protected abstract string DatabasePath { get; }

        [ParamsAllValues]
        public bool IsJournalEnabled;

        [Params(FileMode.Exclusive)]
        public FileMode FileMode;

        [Params(null, "SecurePassword")]
        public string Password;

        private ConnectionString _connectionString;
        protected ConnectionString ConnectionString => _connectionString ?? (_connectionString = new ConnectionString(DatabasePath)
        {
            Journal = IsJournalEnabled,
            Mode = FileMode,
            Password = Password
        });
    }
}