using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    public static Task Behavior(IBehaviorContext ctx, int index, PipelinePart[] parts)
    {
        var context = Unsafe.As<BehaviorContext>(ctx);
        var behavior = context.GetBehavior<BehaviorTrampoline>(index);
        return behavior.Invoke(ctx, StageRunners.Next);
    }

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
        return behavior.Invoke(ctx, StageRunners.Next);
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
                Frame = parentContext.Frame;
            }
            else
            {
                Behaviors = [];
                Frame = new PipelineFrame();
            }
        }

        internal IBehavior[] Behaviors { get; init; }
        internal PipelineFrame Frame;

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

    [SkipLocalsInit]
    public struct PipelineFrame
    {
        public PipelinePart[] Parts = [];
        public int Index = 0;

        private FrameStack stack = default;
        private int stackDepth = 0;

        public PipelineFrame()
        {
        }

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
        private static void ThrowOverflow() => throw new InvalidOperationException($"Pipeline frame stack overflow. MaxDepth={FrameStack.MaxDepth}.");
    }

    public readonly record struct PipelinePart(Func<IBehaviorContext, int, PipelinePart[], Task> Invoke, int BehaviorIndex, PipelinePart[]? ChildParts = null);

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
            scoped ref var frame = ref context.Frame;
            frame.Parts = parts;
            frame.Index = 0;

            if (parts.Length == 0)
            {
                return Complete(ctx);
            }

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), 0);
            return part.Invoke(ctx, part.BehaviorIndex, part.ChildParts ?? []);
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
            var parts = frame.Parts;
            var nextIndex = ++frame.Index;

            if ((uint)nextIndex >= (uint)parts.Length)
            {
                return Complete(ctx);
            }

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), nextIndex);
            return part.Invoke(ctx, part.BehaviorIndex, part.ChildParts ?? []);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        static Task Complete(IBehaviorContext ctx)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            scoped ref var frame = ref context.Frame;
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
}