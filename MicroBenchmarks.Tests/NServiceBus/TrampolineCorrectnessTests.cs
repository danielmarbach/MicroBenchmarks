using System;
using System.Threading.Tasks;
using MicroBenchmarks.NServiceBus;
using NUnit.Framework;

namespace MicroBenchmarks.Tests.NServiceBus;

[TestFixture]
public class TrampolineCorrectnessTests
{
    [TestCase(10)]
    [TestCase(20)]
    [TestCase(40)]
    public void ContinueWith_executes_full_depth_before_throwing(int depth)
    {
        AssertFullDepthAndThrow(depth, TrampolineContinueWith.StageRunners.Start);
    }

    [TestCase(10)]
    [TestCase(20)]
    [TestCase(40)]
    public void AsyncLocal_executes_full_depth_before_throwing(int depth)
    {
        AssertFullDepthAndThrow(depth, TrampolineAsyncLocal.StageRunners.Start);
    }

    [TestCase(10)]
    [TestCase(20)]
    [TestCase(40)]
    public void UnsafeOnCompleted_executes_full_depth_before_throwing(int depth)
    {
        AssertFullDepthAndThrow(depth, TrampolineUnsafeOnCompleted.StageRunners.Start);
    }

    [TestCase(10)]
    [TestCase(20)]
    [TestCase(40)]
    public void ValueTaskSource_executes_full_depth_before_throwing(int depth)
    {
        AssertFullDepthAndThrow(depth, TrampolineValueTaskSource.StageRunners.Start);
    }

    static void AssertFullDepthAndThrow(int depth, Func<Trampoline.IBehaviorContext, Task> start)
    {
        var context = CreateExceptionContext(depth);

        var exception = Assert.Throws<InvalidOperationException>(() => start(context).GetAwaiter().GetResult());

        Assert.That(exception, Is.Not.Null);
        Assert.That(context.Executed, Is.EqualTo(depth));
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
