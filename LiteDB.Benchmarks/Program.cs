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
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly, DefaultConfig.Instance
                .With(MemoryDiagnoser.Default)
                .With(BenchmarkReportExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub)
                .With(Job.Mono
                    .With(Jit.Llvm)
                    .With(new[] {new MonoArgument("--optimize=inline")})
                    .WithGcForce(true))
                .With(Job.Core
                    .With(Jit.RyuJit)
                    .With(CsProjCoreToolchain.NetCoreApp21)
                    .WithGcForce(true)));
        }
    }
}