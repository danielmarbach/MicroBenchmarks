using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus;

public static class PrewiredDefaultInterfaces
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<IBehaviorContext, Task> Build(IBehavior[] behaviors)
    {
        if (behaviors.Length == 0)
        {
            return static _ => Task.CompletedTask;
        }

        InvokerNode? next = null;
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            next = behaviors[i].CreateInvokerNode(next);
        }

        return next!.Invoke;
    }
}
