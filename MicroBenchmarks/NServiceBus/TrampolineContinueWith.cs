using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class TrampolineContinueWith
{
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
        public static Task Next(Trampoline.IBehaviorContext ctx)
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            ref var frame = ref context.Frame;
            var nextIndex = ++frame.Index;

            if ((uint)nextIndex >= (uint)frame.RangeEnd)
            {
                context.Frame = frame;
                return Task.CompletedTask;
            }

            Task task;
            try
            {
                task = Dispatch(ctx, nextIndex);
            }
#pragma warning disable PS0019
            catch (Exception)
#pragma warning restore PS0019
            {
                context.Frame = frame;
                throw;
            }

            if (!task.IsCompleted)
            {
                return RestoreWithContinuation(task, context, frame);
            }

            context.Frame = frame;
            return task;
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task Dispatch(Trampoline.IBehaviorContext ctx, int index)
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(context.Parts), index);
            return KnownPipelineInvokers.Invoke(ctx, part);
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task RestoreWithContinuation(Task task, Trampoline.BehaviorContext ctx, Trampoline.PipelineFrame frame)
        {
            ctx.PushRestoreFrame(frame);

            return task.ContinueWith(
                static (t, state) =>
                {
                    var context = Unsafe.As<Trampoline.BehaviorContext>(state)!;
                    context.Frame = context.PopRestoreFrame();
                    t.GetAwaiter().GetResult();
                },
                ctx,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private static class KnownPipelineInvokers
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

                101 => InvokeStage<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(ctx,
                    part.ChildStart, part.ChildEnd),

                _ => InvokeFallback(part)
            };
        }

        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task InvokeBehavior<TContext>(Trampoline.IBehaviorContext ctx)
            where TContext : class, Trampoline.IBehaviorContext
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            var behavior = Unsafe.As<Trampoline.IBehavior<TContext, TContext>>(context.GetBehavior());
            return behavior.Invoke(Unsafe.As<TContext>(ctx), BehaviorNextCache<TContext>.Next);
        }

        [DebuggerNonUserCode]
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task InvokeStage<TInContext, TOutContext>(Trampoline.IBehaviorContext ctx, int childStart, int childEnd)
            where TInContext : class, Trampoline.IBehaviorContext
            where TOutContext : class, Trampoline.IBehaviorContext
        {
            var context = Unsafe.As<Trampoline.BehaviorContext>(ctx);
            ref var frame = ref context.Frame;
            frame.Index = childStart - 1;
            frame.RangeEnd = childEnd;

            var behavior = Unsafe.As<Trampoline.IBehavior<TInContext, TOutContext>>(context.GetBehavior());
            return behavior.Invoke(Unsafe.As<TInContext>(ctx), StageNextCache<TOutContext>.Next);
        }

        [DoesNotReturn]
        private static Task InvokeFallback(in Trampoline.PipelinePart part)
        {
            throw new InvalidOperationException(
                $"Unknown invoker id '{part.InvokerId}' and no fallback delegate was provided.");
        }

        private static class BehaviorNextCache<TContext> where TContext : class, Trampoline.IBehaviorContext
        {
            public static readonly Func<TContext, Task> Next = StageRunners.Next;
        }

        private static class StageNextCache<TOutContext> where TOutContext : class, Trampoline.IBehaviorContext
        {
            public static readonly Func<TOutContext, Task> Next = StageRunners.Next;
        }
    }
}
