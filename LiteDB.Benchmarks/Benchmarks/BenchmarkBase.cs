using BenchmarkDotNet.Attributes;

namespace LiteDB.Benchmarks.Benchmarks
{
    public abstract class BenchmarkBase
    {
        protected virtual string DatabasePath { get; }

        [Params(10, 50, 100, 500, 1000, 5000, 10000)]
        public int N;
    }
}