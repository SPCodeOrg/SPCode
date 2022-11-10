using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using SourcepawnCondenser;

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
        var condenser =
            new Condenser(File.ReadAllText("sourcepawn/nativevotes.inc"), "test"); // The biggest thing I found
        condenser.Condense();
    }
}