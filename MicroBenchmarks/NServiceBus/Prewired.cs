using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class Prewired
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
            var root = context.PrewiredRoot ??= Build(context.Parts);
            return root(ctx);
        }
    }

    static Func<Trampoline.IBehaviorContext, Task> Build(Trampoline.PipelinePart[] parts)
    {
        if (parts.Length == 0)
        {
            return static _ => Task.CompletedTask;
        }

        Node? next = null;
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), i);
            next = part.InvokerId switch
            {
                1 => new Node<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(i, CreateNext<Trampoline.IBehaviorContext>(next)),
                2 => new Node<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(i, CreateNext<Trampoline.IBehaviorContext>(next)),
                101 => new Node<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(i, CreateNext<Trampoline.IBehaviorContext>(next)),
                _ => throw new InvalidOperationException($"Unknown invoker id '{part.InvokerId}'.")
            };
        }

        return next!.Invoke;
    }

    static Func<TOut, Task> CreateNext<TOut>(Node? next) where TOut : class, Trampoline.IBehaviorContext => next is null ? CompletedNextCache<TOut>.Next : next.Invoke;

    abstract class Node
    {
        public abstract Task Invoke(Trampoline.IBehaviorContext context);
    }

    sealed class Node<TIn, TOut>(int index, Func<TOut, Task> next) : Node
        where TIn : class, Trampoline.IBehaviorContext
        where TOut : class, Trampoline.IBehaviorContext
    {
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Task Invoke(Trampoline.IBehaviorContext context)
        {
            var typedContext = Unsafe.As<Trampoline.BehaviorContext>(context);
            var behavior = Unsafe.As<Trampoline.IBehavior<TIn, TOut>>(Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(typedContext.Behaviors), index));
            return behavior.Invoke(Unsafe.As<TIn>(context), next);
        }
    }

    static class CompletedNextCache<TOut> where TOut : class, Trampoline.IBehaviorContext
    {
        public static readonly Func<TOut, Task> Next = _ => Task.CompletedTask;
    }
}