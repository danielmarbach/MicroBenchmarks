using System.Threading.Tasks;

namespace MicroBenchmarks
{
    public static class TaskExtensions
    {
        public static Task<TResult> Cast<TSource, TResult>(this Task<TSource> task) where TSource : TResult
        {
            var tcs = new TaskCompletionSource<TResult>();

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
                    tcs.TrySetResult(t.Result);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }
    }

}