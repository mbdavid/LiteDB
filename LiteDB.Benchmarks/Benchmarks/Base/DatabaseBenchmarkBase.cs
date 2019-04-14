using BenchmarkDotNet.Attributes;

namespace LiteDB.Benchmarks.Benchmarks.Base
{
    public abstract class DatabaseBenchmarkBase : BenchmarkBase
    {
        protected virtual string DatabasePath { get; }
    }
}