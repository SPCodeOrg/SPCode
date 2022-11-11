using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using SourcepawnCondenser;
using static SPCode.Utils.SPSyntaxTidy.SPSyntaxTidy;

namespace SPCodeBenchmarks;

[Config(typeof(Config))]
[MemoryDiagnoser]
public class CondenserBench
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.MediumRun
                .WithLaunchCount(1)
                .WithToolchain(InProcessNoEmitToolchain.Instance)
                .WithId("InProcess"));
        }
    }

    [Benchmark]
    public void Condense()
    {
        var text = File.ReadAllText("sourcepawn/nativevotes.inc");

        var condenser =
            new Condenser(text, "test"); // The biggest thing I found

        condenser.Condense();
    }
}