using BenchmarkDotNet.Attributes;

namespace LiteDB.Benchmarks.Benchmarks.Base
{
    public abstract class DatabaseBenchmarkBase : BenchmarkBase
    {
        protected virtual string DatabasePath { get; }

        [ParamsAllValues]
        private bool IsJournalEnabled { get; }

        [Params(null, "SecurePassword")]
        private string Password { get; }

        private ConnectionString _connectionString;
        protected ConnectionString ConnectionString => _connectionString ?? (_connectionString = new ConnectionString(DatabasePath)
        {
            Journal = IsJournalEnabled,
            Mode = FileMode.Exclusive,
            Password = Password
        });
    }
}