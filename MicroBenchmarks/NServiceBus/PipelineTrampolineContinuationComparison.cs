using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineTrampolineContinuationComparison
{
    Trampoline.BehaviorContext continueWithContext;
    Trampoline.BehaviorContext unsafeOnCompletedContext;
    Trampoline.BehaviorContext valueTaskSourceContext;
    Trampoline.BehaviorContext asyncLocalContext;

    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        continueWithContext = CreateTrampolineExceptionContext(PipelineDepth);
        unsafeOnCompletedContext = CreateTrampolineExceptionContext(PipelineDepth);
        valueTaskSourceContext = CreateTrampolineExceptionContext(PipelineDepth);
        asyncLocalContext = CreateTrampolineExceptionContext(PipelineDepth);

        try
        {
            TrampolineContinueWith.StageRunners.Start(continueWithContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            TrampolineUnsafeOnCompleted.StageRunners.Start(unsafeOnCompletedContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            TrampolineValueTaskSource.StageRunners.Start(valueTaskSourceContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }

        try
        {
            TrampolineAsyncLocal.StageRunners.Start(asyncLocalContext).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ExceptionAsync")]
    public async Task<Exception?> ContinueWith()
    {
        try
        {
            await TrampolineContinueWith.StageRunners.Start(continueWithContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("ExceptionAsync")]
    public async Task<Exception?> UnsafeOnCompleted()
    {
        try
        {
            await TrampolineUnsafeOnCompleted.StageRunners.Start(unsafeOnCompletedContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("ExceptionAsync")]
    public async Task<Exception?> ValueTaskSource()
    {
        try
        {
            await TrampolineValueTaskSource.StageRunners.Start(valueTaskSourceContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    [Benchmark]
    [BenchmarkCategory("ExceptionAsync")]
    public async Task<Exception?> AsyncLocal()
    {
        try
        {
            await TrampolineAsyncLocal.StageRunners.Start(asyncLocalContext).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }

        return null;
    }

    static Trampoline.BehaviorContext CreateTrampolineExceptionContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];

        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new Trampoline.BehaviorTrampoline();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.BehaviorTrampoline>();
        }

        var lastIndex = depth - 1;
        behaviors[lastIndex] = new Trampoline.ThrowingTrampoline();
        parts[lastIndex] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, Trampoline.ThrowingTrampoline>();

        return new Trampoline.BehaviorContext
        {
            Behaviors = behaviors,
            Parts = parts
        };
    }
}
