using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class Trampoline
{
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task TrampolineInvoke(IBehaviorContext ctx, int start, int rangeEnd)
    {
        var context = Unsafe.As<BehaviorContext>(ctx);
        scoped ref var frame = ref context.Frame;
        var behavior = context.GetBehavior<BehaviorTrampoline>(frame.Index);
        return behavior.Invoke(ctx, StageRunners.Next);
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task ThrowingInvoke(IBehaviorContext ctx, int start, int rangeEnd)
    {
        var context = Unsafe.As<BehaviorContext>(ctx);
        scoped ref var frame = ref context.Frame;
        var behavior = context.GetBehavior<ThrowingTrampoline>(frame.Index);
        return behavior.Invoke(ctx, StageRunners.Next);
    }

    public sealed class BehaviorTrampoline : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            return next(context);
        }
    }

    public sealed class ThrowingTrampoline : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    public interface IBehaviorContext;

    public class BehaviorContext : IBehaviorContext
    {
        internal PipelineFrame Frame;

        public BehaviorContext(IBehaviorContext? parent = null)
        {
            if (parent is BehaviorContext parentContext)
            {
                Behaviors = parentContext.Behaviors;
                Parts = parentContext.Parts;
                Frame = parentContext.Frame;
            }
            else
            {
                Behaviors = [];
                Parts = [];
                Frame = new PipelineFrame();
            }
        }

        internal IBehavior[] Behaviors { get; init; }
        internal PipelinePart[] Parts { get; init; }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TBehavior GetBehavior<TBehavior>(int index)
            where TBehavior : class, IBehavior
        {
            return Unsafe.As<TBehavior>(
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Behaviors), index));
        }
    }

    public struct PipelineFrame
    {
        public int Index = 0;
        public int RangeEnd = 0;

        public PipelineFrame()
        {
        }
    }

    public readonly record struct PipelinePart(
        Func<IBehaviorContext, int, int, Task> Invoke,
        int ChildStart = 0,
        int ChildEnd = 0);

    public static class StageRunners
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Start(IBehaviorContext ctx)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            scoped ref var frame = ref context.Frame;
            frame.Index = 0;
            frame.RangeEnd = context.Parts.Length;

            if (context.Parts.Length == 0) return Task.CompletedTask;

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), 0);
            return part.Invoke(ctx, part.ChildStart, part.ChildEnd);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Next(IBehaviorContext ctx)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            scoped ref var frame = ref context.Frame;
            var nextIndex = ++frame.Index;

            if ((uint)nextIndex >= (uint)frame.RangeEnd) return Task.CompletedTask;

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), nextIndex);
            return part.Invoke(ctx, part.ChildStart, part.ChildEnd);
        }
    }

    public static class BehaviorPartFactory
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        public static PipelinePart Create<TContext, TBehavior>()
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
        {
            return new PipelinePart(Cache<TContext, TBehavior>.Invoke);
        }

        private static class Cache<TContext, TBehavior>
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
        {
            public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
                static (ctx, _, _) =>
                {
                    var context = Unsafe.As<BehaviorContext>(ctx);
                    scoped ref var frame = ref context.Frame;
                    var behavior = context.GetBehavior<TBehavior>(frame.Index);
                    return behavior.Invoke(Unsafe.As<TContext>(ctx), Next!);
                };

            private static readonly Func<TContext, Task> Next = StageRunners.Next;
        }
    }

    public static class StagePartFactory
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        public static PipelinePart Create<TInContext, TOutContext, TBehavior>(int childStartIndex, int childEndIndex)
            where TInContext : class, IBehaviorContext
            where TOutContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TInContext, TOutContext>
        {
            return new PipelinePart(Cache<TInContext, TOutContext, TBehavior>.Invoke, childStartIndex, childEndIndex);
        }

        private static class Cache<TInContext, TOutContext, TBehavior>
            where TInContext : class, IBehaviorContext
            where TOutContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TInContext, TOutContext>
        {
            public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
                static (ctx, childStart, _) =>
                {
                    var context = Unsafe.As<BehaviorContext>(ctx);
                    scoped ref var frame = ref context.Frame;
                    var behavior = context.GetBehavior<TBehavior>(frame.Index);
                    frame.Index = childStart - 1; // -1 because Next() increments before dispatch
                    return behavior.Invoke(Unsafe.As<TInContext>(ctx), Start!);
                };

            private static readonly Func<TOutContext, Task> Start = StageRunners.Next;
        }
    }

    public interface IBehavior<in TInContext, out TOutContext> : IBehavior
        where TInContext : IBehaviorContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }
}