using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class Trampoline
{
    public sealed class BehaviorTrampoline : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            return next(context);
        }
    }

    public sealed class BehaviorTrampolinePart : BehaviorPart<IBehaviorContext, BehaviorTrampoline>;

    // public sealed class BehaviorTrampolinePart : PipelinePart
    // {
    //     [DebuggerStepThrough]
    //     [DebuggerHidden]
    //     [DebuggerNonUserCode]
    //     [StackTraceHidden]
    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public sealed override Task Invoke(IBehaviorContext context)
    //     {
    //         var ctx = Unsafe.As<BehaviorContext>(context);
    //         var behavior = ctx.GetBehavior<BehaviorTrampoline>(ctx.CurrentIndex);
    //         // cast remains here because in real code we would need it too
    //         return behavior.Invoke(Unsafe.As<IBehaviorContext>(context), CachedNext);
    //     }
    // }

    public sealed class ThrowingTrampoline : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    public sealed class ThrowingTrampolinePart : BehaviorPart<IBehaviorContext, ThrowingTrampoline>;

    public static readonly Func<IBehaviorContext, Task> CachedNext = StageRunners.Next;

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

    public abstract class PipelinePart
    {
        public abstract Task Invoke(IBehaviorContext context);
    }

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

            return parts.Length == 0
                ? Task.CompletedTask
                : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), 0).Invoke(ctx);
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

            return (uint)nextIndex >= (uint)parts.Length
                ? Task.CompletedTask
                : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), nextIndex).Invoke(ctx);
        }
    }

    public interface IBehavior<in TInContext, out TOutContext> : IBehavior
        where TInContext : IBehaviorContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }

    public abstract class BehaviorPart<TContext, TBehavior> : PipelinePart
        where TContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TContext, TContext>
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override Task Invoke(IBehaviorContext context)
        {
            var ctx = Unsafe.As<BehaviorContext>(context);
            var behavior = ctx.GetBehavior<TBehavior>(ctx.CurrentIndex);
            return behavior.Invoke(Unsafe.As<TContext>(context), CachedNext);
        }
    }

    public abstract class StagePart<TInContext, TOutContext, TBehavior>(int nextStageStartIndex)
        : PipelinePart
        where TInContext : class, IBehaviorContext
        where TOutContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TInContext, TOutContext>
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        public sealed override Task Invoke(IBehaviorContext context)
        {
            var ctx = Unsafe.As<BehaviorContext>(context);
            var behavior = ctx.GetBehavior<TBehavior>(ctx.CurrentIndex);
            // Set the next stage start index so the cached delegate knows where to jump
            ctx.CurrentIndex = nextStageStartIndex - 1; // -1 because Next will increment

            return behavior.Invoke(Unsafe.As<TInContext>(context), CachedNext);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task StartNextStage(TOutContext ctx)
        {
            return StageRunners.Next(ctx);
        }
    }
}