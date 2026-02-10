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
    public static Task TrampolineInvoke(IBehaviorContext ctx, int start, int rangeEnd)
    {
        var context = Unsafe.As<BehaviorContext>(ctx);
        scoped ref var frame = ref context.Frame;
        var behavior = context.GetBehavior<BehaviorTrampoline>(frame.Index);
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

    public readonly record struct FrameSnapshot(int Index, int RangeEnd);

    [InlineArray(MaxDepth)]
    public struct FrameStack
    {
        public const int MaxDepth = 8; // this is well known

        private FrameSnapshot _element0;
    }

    [SkipLocalsInit]
    public struct PipelineFrame
    {
        public int Index = 0;
        public int RangeEnd = 0;
        public int PendingChildStart = 0;
        public int PendingChildEnd = 0;

        private FrameStack stack = default;
        private int stackDepth = 0;

        public PipelineFrame()
        {
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(int index, int rangeEnd)
        {
            var d = stackDepth;
            if ((uint)d >= FrameStack.MaxDepth) ThrowOverflow();

            stack[d] = new FrameSnapshot(index, rangeEnd);
            stackDepth = d + 1;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [DebuggerHidden]
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
        private static void ThrowOverflow()
        {
            throw new InvalidOperationException($"Pipeline frame stack overflow. MaxDepth={FrameStack.MaxDepth}.");
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
            return Start(ctx, 0, context.Parts.Length);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Start(IBehaviorContext ctx, int startIndex, int rangeEnd)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            scoped ref var frame = ref context.Frame;
            frame.Index = startIndex;
            frame.RangeEnd = rangeEnd;

            if (startIndex >= rangeEnd) return Complete(ctx);

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), startIndex);
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

            if ((uint)nextIndex >= (uint)frame.RangeEnd) return Complete(ctx);

            scoped ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), nextIndex);
            return part.Invoke(ctx, part.ChildStart, part.ChildEnd);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task Complete(IBehaviorContext ctx)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            scoped ref var frame = ref context.Frame;
            if (!frame.TryPop(out var frameSnapshot)) return Task.CompletedTask;

            frame.Index = frameSnapshot.Index;
            frame.RangeEnd = frameSnapshot.RangeEnd;

            return Next(ctx);
        }
    }

    public static class BehaviorPartFactory
    {
        private static class Cache<TContext, TBehavior>
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
        {
            // Cache the typed Next delegate once per TContext type
            private static readonly Func<TContext, Task> NextDelegate = static ctx => StageRunners.Next(ctx);

            public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
                static (ctx, _, _) =>
                {
                    var context = Unsafe.As<BehaviorContext>(ctx);
                    scoped ref var frame = ref context.Frame;
                    var behavior = context.GetBehavior<TBehavior>(frame.Index);
                    return behavior.Invoke(Unsafe.As<TContext>(ctx), NextDelegate);
                };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PipelinePart Create<TContext, TBehavior>()
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
            => new(Cache<TContext, TBehavior>.Invoke);
    }

    public interface IBehavior<in TInContext, out TOutContext> : IBehavior
        where TInContext : IBehaviorContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }
}