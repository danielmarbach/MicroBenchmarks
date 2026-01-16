using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class Trampoline
{
    // if we can generate this for every behavior we win
    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Behavior(IBehaviorContext ctx, int index, PipelinePart[] parts)
    {
        var context = Unsafe.As<BehaviorContext>(ctx);
        var behavior = context.GetBehavior<BehaviorTrampoline>(index);
        return behavior.Invoke(ctx, CachedNext);
    }

    // This is crucial! We need a cached delegate for each stage
    static readonly Func<IBehaviorContext, Task> CachedNext = StageRunners.Next;

    public sealed class BehaviorTrampoline : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            return next(context);
        }
    }

    [DebuggerStepThrough]
    [DebuggerHidden]
    [DebuggerNonUserCode]
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task Throwing(IBehaviorContext ctx, int index, PipelinePart[] parts)
    {
        var context = Unsafe.As<BehaviorContext>(ctx);
        var behavior = context.GetBehavior<ThrowingTrampoline>(index);
        return behavior.Invoke(ctx, CachedNext);
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
        public BehaviorContext(IBehaviorContext? parent = null)
        {
            if (parent is BehaviorContext parentContext)
            {
                Behaviors = parentContext.Behaviors;
                Parts = parentContext.Parts;
                CurrentIndex = parentContext.CurrentIndex;
            }
            else
            {
                Behaviors = [];
                Parts = [];
                CurrentIndex = 0;
            }
        }

        internal IBehavior[] Behaviors { get; init; }
        internal PipelinePart[] Parts;
        internal int CurrentIndex;

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TBehavior GetBehavior<TBehavior>(int index)
            where TBehavior : class, IBehavior
            => Unsafe.As<TBehavior>(
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Behaviors), index));
    }

    public readonly record struct PipelinePart(
        Func<IBehaviorContext, int, PipelinePart[], Task> Invoke,
        int NextStageStartIndex = -1);

    public static class StageRunners
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Start(IBehaviorContext ctx, PipelinePart[] parts)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            context.Parts = parts;
            context.CurrentIndex = 0;

            if (parts.Length == 0)
            {
                return Task.CompletedTask;
            }

            scoped ref var part = ref MemoryMarshal.GetArrayDataReference(parts);
            return part.Invoke(ctx, 0, parts);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Next(IBehaviorContext ctx)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            var parts = context.Parts;
            var nextIndex = ++context.CurrentIndex;

            if ((uint)nextIndex >= (uint)parts.Length)
            {
                return Task.CompletedTask;
            }

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), nextIndex);
            return part.Invoke(ctx, nextIndex, parts);
        }
    }

    // This seems more inlineable
    public static class Invoker
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Invoke<TContext, TBehavior>(IBehaviorContext context, int index, PipelinePart[] parts)
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
        {
            var ctx = Unsafe.As<BehaviorContext>(context);
            var behavior = ctx.GetBehavior<TBehavior>(index);
            return behavior.Invoke(Unsafe.As<TContext>(context), CachedNext);
        }
    }

    public static class BehaviorPart<TContext, TBehavior>
        where TContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TContext, TContext>
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Invoke(IBehaviorContext context, int index, PipelinePart[] parts)
        {
            var ctx = Unsafe.As<BehaviorContext>(context);
            var behavior = ctx.GetBehavior<TBehavior>(index);
            return behavior.Invoke(Unsafe.As<TContext>(context), CachedNext);
        }
    }

    /// <summary>
    /// Stage transition part. The nextStageStartIndex is stored in the PipelinePart struct.
    /// </summary>
    public static class StagePart<TInContext, TOutContext, TBehavior>
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TInContext, TOutContext>
    {
        private static readonly Func<TOutContext, Task> CachedNext = StageRunners.Next;

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Invoke(IBehaviorContext context, int index, PipelinePart[] parts)
        {
            var ctx = Unsafe.As<BehaviorContext>(context);
            var behavior = ctx.GetBehavior<TBehavior>(index);

            // Get the next stage start index from the current part
            scoped ref var currentPart = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), index);
            // Set the index to nextStageStartIndex - 1 because Next will increment
            ctx.CurrentIndex = currentPart.NextStageStartIndex - 1;

            return behavior.Invoke(Unsafe.As<TInContext>(context), CachedNext);
        }
    }

    public interface IBehavior<in TInContext, out TOutContext> : IBehavior
        where TInContext : IBehaviorContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }
}