using System;
using System.Threading;
using System.Threading.Tasks;

namespace MicroBenchmarks.Tasks
{
    public static class TaskExtensionsForWaitWithCancellation
    {
        public static async Task<TResult> WithWaitCancellationLinkedTokenSource<TResult>(this Task<TResult> task,
            CancellationToken cancellationToken)
        {
            using (var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var delayTask = Task.Delay(Timeout.Infinite, combined.Token);
                Task completedTask = await Task.WhenAny(task, delayTask);
                if (completedTask == task)
                {
                    combined.Cancel();
                    return await task;
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new InvalidOperationException("Infinite delay task completed.");
                }
            }
        }

        public static Task<TResult> WithWaitCancellationTaskCompletionSource<TResult>(this Task<TResult> task,
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var registration = cancellationToken.Register(s =>
            {
                var source = (TaskCompletionSource<TResult>)s;
                source.TrySetCanceled();
            }, tcs);

            task.ContinueWith((t, s) =>
            {
                var tcsAndRegistration = (Tuple<TaskCompletionSource<TResult>, CancellationTokenRegistration>)s;

                if (t.IsFaulted && t.Exception != null)
                {
                    tcsAndRegistration.Item1.TrySetException(t.Exception.InnerException);
                }

                if (t.IsCanceled)
                {
                    tcsAndRegistration.Item1.TrySetCanceled();
                }

                if (t.IsCompleted)
                {
                    tcsAndRegistration.Item1.TrySetResult(t.Result);
                }

                tcsAndRegistration.Item2.Dispose();
            }, Tuple.Create(tcs, registration), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return tcs.Task;
        }
    }
}