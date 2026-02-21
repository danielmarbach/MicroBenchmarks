using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[ShortRunJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Alphabetical)]
public class PipelineTrampolineCorrectnessProbe
{
    [Params(10, 20, 40)]
    public int PipelineDepth { get; set; }

    [Benchmark(Baseline = true)]
    public int ContinueWith_CountBeforeThrow() => RunAndCount(TrampolineContinueWith.StageRunners.Start);

    [Benchmark]
    public int AsyncLocal_CountBeforeThrow() => RunAndCount(TrampolineAsyncLocal.StageRunners.Start);

    [Benchmark]
    public int UnsafeOnCompleted_CountBeforeThrow() => RunAndCount(TrampolineUnsafeOnCompleted.StageRunners.Start);

    [Benchmark]
    public int ValueTaskSource_CountBeforeThrow() => RunAndCount(TrampolineValueTaskSource.StageRunners.Start);

    int RunAndCount(Func<Trampoline.IBehaviorContext, Task> start)
    {
        var context = CreateExceptionContext(PipelineDepth);

        try
        {
            start(context).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            return context.Executed;
        }

        return -1;
    }

    static ProbeBehaviorContext CreateExceptionContext(int depth)
    {
        var behaviors = new IBehavior[depth];
        var parts = new Trampoline.PipelinePart[depth];

        for (var i = 0; i < depth - 1; i++)
        {
            behaviors[i] = new CountingBehavior();
            parts[i] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, CountingBehavior>();
        }

        var last = depth - 1;
        behaviors[last] = new ThrowingCountingBehavior();
        parts[last] = Trampoline.BehaviorPartFactory.Create<Trampoline.IBehaviorContext, ThrowingCountingBehavior>();

        return new ProbeBehaviorContext
        {
            Behaviors = behaviors,
            Parts = parts
        };
    }

    sealed class ProbeBehaviorContext : Trampoline.BehaviorContext
    {
        public int Executed;
    }

    sealed class CountingBehavior : Trampoline.IBehavior<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>
    {
        public async Task Invoke(Trampoline.IBehaviorContext context, Func<Trampoline.IBehaviorContext, Task> next)
        {
            ((ProbeBehaviorContext)context).Executed++;
            await next(context).ConfigureAwait(false);
        }
    }

    sealed class ThrowingCountingBehavior : Trampoline.IBehavior<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>
    {
        public async Task Invoke(Trampoline.IBehaviorContext context, Func<Trampoline.IBehaviorContext, Task> next)
        {
            ((ProbeBehaviorContext)context).Executed++;
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }
}
