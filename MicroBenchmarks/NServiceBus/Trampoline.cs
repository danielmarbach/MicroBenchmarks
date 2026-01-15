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

    public sealed class BehaviorTrampolinePart(int behaviorIndex) : BehaviorPart<IBehaviorContext, BehaviorTrampoline>(behaviorIndex);

    public sealed class ThrowingTrampoline : IBehavior<IBehaviorContext, IBehaviorContext>
    {
        public async Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            await Task.Yield();
            throw new InvalidOperationException();
        }
    }

    public sealed class ThrowingTrampolinePart(int behaviorIndex) : BehaviorPart<IBehaviorContext, ThrowingTrampoline>(behaviorIndex);

    public interface IBehaviorContext
    {
        // In Core this would move to extensions and not be exposed on the interface to public
        PipelineFrame Frame { get; }
    }

    public class BehaviorContext : IBehaviorContext
    {
        public BehaviorContext(IBehaviorContext? parent = null)
        {
            if (parent is BehaviorContext parentContext)
            {
                Behaviors = parentContext.Behaviors;
                Frame = parentContext.Frame;
            }
            else
            {
                Behaviors = [];
                Frame = new PipelineFrame();
            }
        }

        internal IBehavior[] Behaviors { get; init; }
        public PipelineFrame Frame { get; }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TBehavior GetBehavior<TBehavior>(int index)
            where TBehavior : class, IBehavior
            => Unsafe.As<TBehavior>(
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Behaviors), index));
    }

    public readonly record struct FrameSnapshot(PipelinePart[] Parts, int Index);

    [InlineArray(MaxDepth)]
    public struct FrameStack
    {
        public const int MaxDepth = 8; // this is well known

        private FrameSnapshot _element0;
    }

    public sealed class PipelineFrame
    {
        public PipelinePart[] Parts = [];
        public int Index;

        private FrameStack stack;
        private int stackDepth;

        // Should be verified whether those hints are still necessary
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(PipelinePart[] parts, int index)
        {
            var d = stackDepth;
            if ((uint)d >= FrameStack.MaxDepth)
            {
                ThrowOverflow();
            }

            stack[d] = new FrameSnapshot(parts, index);
            stackDepth = d + 1;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
        // Should be verified whether those hints are still necessary
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out FrameSnapshot snapshot)
        {
            var d = stackDepth;
            if (d == 0)
            {
                snapshot = default;
                return false;
            }

            d--;
            snapshot = stack[d];
            stackDepth = d;
            return true;
        }

        [DoesNotReturn]
        private static void ThrowOverflow() =>
            throw new InvalidOperationException($"Pipeline frame stack overflow. MaxDepth={FrameStack.MaxDepth}.");
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
        public static Task Start(IBehaviorContext ctx, PipelinePart[] parts)
        {
            var frame = ctx.Frame;
            frame.Parts = parts;
            frame.Index = 0;

            return parts.Length == 0
                ? Complete(ctx)
                : Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), 0).Invoke(ctx);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        public static Task Next(IBehaviorContext ctx)
        {
            var f = ctx.Frame;
            var nextIndex = ++f.Index;

            return (uint)nextIndex < (uint)f.Parts.Length
                ? Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(f.Parts), nextIndex).Invoke(ctx)
                : Complete(ctx);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        static Task Complete(IBehaviorContext ctx)
        {
            var frame = ctx.Frame;
            if (!frame.TryPop(out var frameSnapshot))
            {
                return Task.CompletedTask;
            }

            frame.Parts = frameSnapshot.Parts;
            frame.Index = frameSnapshot.Index;

            return Next(ctx);
        }
    }

    public interface IBehavior<in TInContext, out TOutContext> : IBehavior
        where TInContext : IBehaviorContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }

    public abstract class BehaviorPart<TContext, TBehavior>(int behaviorIndex) : PipelinePart
        where TContext : class, IBehaviorContext
        where TBehavior : class, IBehavior<TContext, TContext>
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        public sealed override Task Invoke(IBehaviorContext context)
        {
            var ctx = (TContext)context;
            var behavior = Unsafe.As<BehaviorContext>(context).GetBehavior<TBehavior>(behaviorIndex);
            return behavior.Invoke(ctx, Next);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task Next(TContext ctx) => StageRunners.Next(ctx);
    }

    public abstract class StagePart<TInContext, TOutContext, TBehavior>(int stageIndex, PipelinePart[] childParts)
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
            var frame = context.Frame;

            frame.Push(frame.Parts, frame.Index);

            frame.Parts = childParts;
            frame.Index = 0;

            return childParts.Length == 0
                ? StageRunners.Next(context)
                : (context as BehaviorContext)!.GetBehavior<TBehavior>(stageIndex).Invoke((TInContext)context, Start);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task Start(TOutContext ctx) => StageRunners.Start(ctx, ctx.Frame.Parts);
    }
}