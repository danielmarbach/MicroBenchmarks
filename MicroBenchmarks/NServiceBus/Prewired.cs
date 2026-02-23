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
            var root = context.PrewiredRoot ??= Build(context.Parts, context.Behaviors);
            return root(ctx);
        }
    }

    static Func<Trampoline.IBehaviorContext, Task> Build(Trampoline.PipelinePart[] parts, IBehavior[] behaviors)
    {
        if (parts.Length == 0)
        {
            return static _ => Task.CompletedTask;
        }

        Node? next = null;
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            ref var part = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(parts), i);
            var behavior = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(behaviors), i);
            next = part.InvokerId switch
            {
                1 => CreateNode<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(behavior, next),
                2 => CreateNode<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(behavior, next),
                101 => CreateNode<Trampoline.IBehaviorContext, Trampoline.IBehaviorContext>(behavior, next),
                _ => throw new InvalidOperationException($"Unknown invoker id '{part.InvokerId}'.")
            };
        }

        return next!.Invoke;
    }

    static Node<TInContext, TOutContext> CreateNode<TInContext, TOutContext>(IBehavior behavior, Node? next)
        where TInContext : class, Trampoline.IBehaviorContext
        where TOutContext : class, Trampoline.IBehaviorContext =>
        new((Trampoline.IBehavior<TInContext, TOutContext>)behavior, CreateNext<TOutContext>(next));

    static Func<TOut, Task> CreateNext<TOut>(Node? next) where TOut : class, Trampoline.IBehaviorContext => next is null ? CompletedNextCache<TOut>.Next : next.Invoke;

    abstract class Node
    {
        public abstract Task Invoke(Trampoline.IBehaviorContext context);
    }

    sealed class Node<TIn, TOut>(Trampoline.IBehavior<TIn, TOut> behavior, Func<TOut, Task> next) : Node
        where TIn : class, Trampoline.IBehaviorContext
        where TOut : class, Trampoline.IBehaviorContext
    {
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        [DebuggerNonUserCode]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override Task Invoke(Trampoline.IBehaviorContext context) => behavior.Invoke(Unsafe.As<TIn>(context), next);
    }

    static class CompletedNextCache<TOut> where TOut : class, Trampoline.IBehaviorContext
    {
        public static readonly Func<TOut, Task> Next = _ => Task.CompletedTask;
    }
}