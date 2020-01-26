using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace LiteDB.Benchmarks
{
    class Program
    {
        // sudo dotnet run -c Release -f netcoreapp3.1 -p LiteDB.Benchmarks/ -v d

        static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance
                //.With(new BenchmarkDotNet.Filters.AnyCategoriesFilter(new[] {Benchmarks.Constants.Categories.INSERTION}))
                .With(Job.Default.With(CoreRuntime.Core31)
                    .With(Jit.RyuJit)
                    .With(CsProjCoreToolchain.NetCoreApp31)
                    .WithGcForce(true))
                /*.With(Job.Default.With(MonoRuntime.Default)
                    .With(Jit.Llvm)
                    .With(new[] {new MonoArgument("--optimize=inline")})
                    .WithGcForce(true))*/
                .With(MemoryDiagnoser.Default)
                .With(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub)
                .KeepBenchmarkFiles());
        }
    }
}