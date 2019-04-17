using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace LiteDB.Benchmarks
{
    class Program
    {
        // sudo mono LiteDB.Benchmarks/bin/Release/net471/LiteDB.Benchmarks.exe
        static void Main(string[] args)
        {
            /*BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance
                .With(MemoryDiagnoser.Default)
                .With(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub)
                .With(Job.Mono
                    .With(Jit.Llvm)
                    .With(new[] {new MonoArgument("--optimize=inline")})
                    .WithGcForce(true))
                .With(Job.Core
                    .With(Jit.RyuJit)
                    .With(CsProjCoreToolchain.NetCoreApp21)
                    .WithGcForce(true)));*/

            /*BenchmarkRunner.Run<Benchmarks.Queries.QueryIgnoreExpressionPropertiesBenchmark>(DefaultConfig.Instance
                .With(MemoryDiagnoser.Default)
                .With(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub)
                .With(Job.Mono
                    .With(Jit.Llvm)
                    .With(new[] {new MonoArgument("--optimize=inline")})
                    .WithGcForce(true))
                .With(Job.Core
                    .With(Jit.RyuJit)
                    .With(CsProjCoreToolchain.NetCoreApp21)
                    .WithGcForce(true)));*/

            BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance
                .With(new AnyCategoriesFilter(new[] {Constants.Categories.QUERIES}))
                .With(Job.Mono
                    .With(Jit.Llvm)
                    .With(new[] {new MonoArgument("--optimize=inline")})
                    .WithGcForce(true))
                .With(Job.Core
                    .With(Jit.RyuJit)
                    .With(CsProjCoreToolchain.NetCoreApp21)
                    .WithGcForce(true))
                .With(MemoryDiagnoser.Default)
                .With(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub)
                .KeepBenchmarkFiles());
        }
    }
}