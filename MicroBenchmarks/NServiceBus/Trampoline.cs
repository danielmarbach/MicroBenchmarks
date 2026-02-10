using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class Trampoline
{
    // [DebuggerStepThrough]
    // [DebuggerHidden]
    // [DebuggerNonUserCode]
    // [StackTraceHidden]
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static Task TrampolineInvoke(IBehaviorContext ctx, int start, int rangeEnd)
    // {
    //     var context = Unsafe.As<BehaviorContext>(ctx);
    //     scoped ref var frame = ref context.Frame;
    //     var behavior = context.GetBehavior<BehaviorTrampoline>(frame.Index);
    //     return behavior.Invoke(ctx, StageRunners.Next);
    // }
    //
    // [DebuggerStepThrough]
    // [DebuggerHidden]
    // [DebuggerNonUserCode]
    // [StackTraceHidden]
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static Task ThrowingInvoke(IBehaviorContext ctx, int start, int rangeEnd)
    // {
    //     var context = Unsafe.As<BehaviorContext>(ctx);
    //     scoped ref var frame = ref context.Frame;
    //     var behavior = context.GetBehavior<ThrowingTrampoline>(frame.Index);
    //     return behavior.Invoke(ctx, StageRunners.Next);
    // }

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
        internal IBehavior GetBehavior()
        {
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Behaviors), Frame.Index);
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
        byte InvokerId,
        Func<IBehaviorContext, int, int, Task>? FallbackInvoke,
        int ChildStart,
        int ChildEnd);

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
            ref var frame = ref context.Frame;
            frame.Index = 0;
            frame.RangeEnd = context.Parts.Length;

            return context.Parts.Length == 0 ? Task.CompletedTask : Dispatch(ctx, 0);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Next(IBehaviorContext ctx)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            ref var frame = ref context.Frame;
            var nextIndex = ++frame.Index;

            if ((uint)nextIndex >= (uint)frame.RangeEnd) return Task.CompletedTask;

            return Dispatch(ctx, nextIndex);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task Dispatch(IBehaviorContext ctx, int index)
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), index);
            return KnownPipelineInvokers.Invoke(ctx, part);
        }
    }

    public static class BehaviorPartFactory
    {
        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PipelinePart Create<TContext, TBehavior>()
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
        {
            var invokerId = PipelinePartInvokerIds.GetBehaviorId(typeof(TContext));
            var fallback = invokerId == PipelinePartInvokerIds.Fallback ? Cache<TContext, TBehavior>.Invoke : null;
            return new PipelinePart(invokerId, fallback, 0, 0);
        }

        private static class Cache<TContext, TBehavior>
            where TContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TContext, TContext>
        {
            public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
                static (ctx, _, _) =>
                {
                    var context = Unsafe.As<BehaviorContext>(ctx);
                    var behavior = Unsafe.As<TBehavior>(context.GetBehavior());
                    return behavior.Invoke(Unsafe.As<TContext>(ctx), Start!);
                };

            private static readonly Func<TContext, Task> Start = StageRunners.Next;
        }
    }

    private static class StagePartFactory
    {
        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PipelinePart Create<TInContext, TOutContext, TBehavior>(int childStartIndex, int childEndIndex)
            where TInContext : class, IBehaviorContext
            where TOutContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TInContext, TOutContext>
        {
            var invokerId = PipelinePartInvokerIds.GetStageId(typeof(TInContext), typeof(TOutContext));
            var fallback = invokerId == PipelinePartInvokerIds.Fallback
                ? Cache<TInContext, TOutContext, TBehavior>.Invoke
                : null;
            return new PipelinePart(invokerId, fallback, childStartIndex, childEndIndex);
        }

        private static class Cache<TInContext, TOutContext, TBehavior>
            where TInContext : class, IBehaviorContext
            where TOutContext : class, IBehaviorContext
            where TBehavior : class, IBehavior<TInContext, TOutContext>
        {
            public static readonly Func<IBehaviorContext, int, int, Task> Invoke =
                static (ctx, childStart, childEnd) =>
                {
                    var context = Unsafe.As<BehaviorContext>(ctx);
                    ref var frame = ref context.Frame;

                    frame.Index = childStart - 1;
                    frame.RangeEnd = childEnd;

                    var behavior = Unsafe.As<TBehavior>(context.GetBehavior());
                    return behavior.Invoke(Unsafe.As<TInContext>(ctx), Start!);
                };

            private static readonly Func<TOutContext, Task> Start = StageRunners.Next;
        }
    }

    private static class KnownPipelineInvokers
    {
        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Invoke(IBehaviorContext ctx, in PipelinePart part)
        {
            return part.InvokerId switch
            {
                PipelinePartInvokerIds.BehaviorStage1 => InvokeBehavior<IBehaviorContext>(ctx),
                PipelinePartInvokerIds.BehaviorStage2 => InvokeBehavior<IBehaviorContext>(ctx),

                PipelinePartInvokerIds.Stage1ToStage2 => InvokeStage<IBehaviorContext, IBehaviorContext>(ctx,
                    part.ChildStart, part.ChildEnd),

                _ => InvokeFallback(ctx, part)
            };
        }

        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task InvokeBehavior<TContext>(IBehaviorContext ctx)
            where TContext : class, IBehaviorContext
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            var behavior = Unsafe.As<IBehavior<TContext, TContext>>(context.GetBehavior());
            return behavior.Invoke(Unsafe.As<TContext>(ctx), BehaviorNextCache<TContext>.Next);
        }

        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task InvokeStage<TInContext, TOutContext>(IBehaviorContext ctx, int childStart, int childEnd)
            where TInContext : class, IBehaviorContext
            where TOutContext : class, IBehaviorContext
        {
            var context = Unsafe.As<BehaviorContext>(ctx);
            ref var frame = ref context.Frame;
            frame.Index = childStart - 1;
            frame.RangeEnd = childEnd;

            var behavior = Unsafe.As<IBehavior<TInContext, TOutContext>>(context.GetBehavior());
            return behavior.Invoke(Unsafe.As<TInContext>(ctx), StageNextCache<TOutContext>.Next);
        }

        [DoesNotReturn]
        private static Task InvokeFallback(IBehaviorContext ctx, in PipelinePart part)
        {
            // if (part.FallbackInvoke != null)
            // {
            //     return part.FallbackInvoke(ctx, part.ChildStart, part.ChildEnd);
            // }

            throw new InvalidOperationException(
                $"Unknown invoker id '{part.InvokerId}' and no fallback delegate was provided.");
        }

        private static class BehaviorNextCache<TContext> where TContext : class, IBehaviorContext
        {
            public static readonly Func<TContext, Task> Next = StageRunners.Next;
        }

        private static class StageNextCache<TOutContext> where TOutContext : class, IBehaviorContext
        {
            public static readonly Func<TOutContext, Task> Next = StageRunners.Next;
        }
    }

    private static class PipelinePartInvokerIds
    {
        public const byte Fallback = 0;

        public const byte BehaviorStage1 = 1;
        public const byte BehaviorStage2 = 2;

        public const byte Stage1ToStage2 = 101;

        public static byte GetBehaviorId(Type contextType)
        {
            if (contextType == typeof(IBehaviorContext)) return BehaviorStage1;

            if (contextType == typeof(IBehaviorContext)) return BehaviorStage2;

            return Fallback;
        }

        public static byte GetStageId(Type inContextType, Type outContextType)
        {
            if (inContextType == typeof(IBehaviorContext) && outContextType == typeof(IBehaviorContext))
                return Stage1ToStage2;

            return Fallback;
        }
    }

    public interface IBehavior<in TInContext, out TOutContext> : IBehavior
        where TInContext : IBehaviorContext
    {
        Task Invoke(TInContext context, Func<TOutContext, Task> next);
    }
}