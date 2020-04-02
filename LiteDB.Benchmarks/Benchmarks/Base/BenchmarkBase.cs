using BenchmarkDotNet.Attributes;

namespace LiteDB.Benchmarks.Benchmarks.Base
{
    public abstract class BenchmarkBase
    {
        [Params(10, 50, 100, 500, 1000, 5000, 10000)]
        public int N;
    }
}