using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroBenchmarks
{
    public static class TaskCastSimulator
    {
        public static Task<IReadOnlyCollection<int>> Simulate()
        {
            return Helper.YieldList().Cast<List<int>, IReadOnlyCollection <int>>();
        }
    }
}