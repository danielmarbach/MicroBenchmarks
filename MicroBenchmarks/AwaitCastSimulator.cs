using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroBenchmarks
{
    public static class AwaitCastSimulator
    {

        public static async Task<IReadOnlyCollection<int>>  Simulate()
        {
            return await Helper.YieldList().ConfigureAwait(false);
        }
    }
}