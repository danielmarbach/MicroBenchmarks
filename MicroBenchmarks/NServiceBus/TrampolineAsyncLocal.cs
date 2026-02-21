using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class TrampolineAsyncLocal
{
    static readonly AsyncLocal<Trampoline.PipelineFrame> Frame = new();

    public static class StageRunners
    {
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Start(Trampoline.IBehaviorContext ctx)
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            Frame.Value = new Trampoline.PipelineFrame
            {
                Index = 0,
                RangeEnd = context.Parts.Length
            };

            return context.Parts.Length == 0 ? Task.CompletedTask : Dispatch(ctx, 0);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Next(Trampoline.IBehaviorContext ctx)
        {
            var frame = Frame.Value;
            var nextIndex = ++frame.Index;

            if ((uint)nextIndex >= (uint)frame.RangeEnd)
            {
                return Task.CompletedTask;
            }

            Frame.Value = frame;
            return Dispatch(ctx, nextIndex);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task Dispatch(Trampoline.IBehaviorContext ctx, int index)
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), index);
            return KnownPipelineInvokers.Invoke(ctx, part);
        }
    }

    static class KnownPipelineInvokers
    {
        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Invoke(Trampoline.IBehaviorContext ctx, in Trampoline.PipelinePart part)
        {
            return part.InvokerId switch
            {
                1 => InvokeBehavior<Trampoline.IBehaviorContext>(ctx),
                2 => InvokeBehavior<Trampoline.IBehaviorContext>(ctx),
                101 => InvokeStage<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(ctx, part.ChildStart, part.ChildEnd),
                _ => InvokeFallback(part)
            };
        }

        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task InvokeBehavior<TContext>(Trampoline.IBehaviorContext ctx)
            where TContext : class, Trampoline.IBehaviorContext
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            var index = Frame.Value.Index;
            ref var behaviorRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Behaviors), index);
            var behavior = Unsafe.As<Trampoline.IBehavior<TContext, TContext>>(behaviorRef);
            return behavior.Invoke(Unsafe.As<TContext>(ctx), BehaviorNextCache<TContext>.Next);
        }

        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Task InvokeStage<TInContext, TOutContext>(Trampoline.IBehaviorContext ctx, int childStart, int childEnd)
            where TInContext : class, Trampoline.IBehaviorContext
            where TOutContext : class, Trampoline.IBehaviorContext
        {
            Frame.Value = new Trampoline.PipelineFrame
            {
                Index = childStart - 1,
                RangeEnd = childEnd
            };

            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            var index = Frame.Value.Index;
            ref var behaviorRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Behaviors), index);
            var behavior = Unsafe.As<Trampoline.IBehavior<TInContext, TOutContext>>(behaviorRef);
            return behavior.Invoke(Unsafe.As<TInContext>(ctx), StageNextCache<TOutContext>.Next);
        }

        [DoesNotReturn]
        static Task InvokeFallback(in Trampoline.PipelinePart part)
        {
            throw new InvalidOperationException($"Unknown invoker id '{part.InvokerId}' and no fallback delegate was provided.");
        }

        static class BehaviorNextCache<TContext> where TContext : class, Trampoline.IBehaviorContext
        {
            public static readonly Func<TContext, Task> Next = StageRunners.Next;
        }

        static class StageNextCache<TOutContext> where TOutContext : class, Trampoline.IBehaviorContext
        {
            public static readonly Func<TOutContext, Task> Next = StageRunners.Next;
        }
    }
}
