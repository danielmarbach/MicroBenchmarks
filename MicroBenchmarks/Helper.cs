using System.Collections.Generic;
using System.Threading.Tasks;

namespace MicroBenchmarks
{
    public static class Helper
    {
        static readonly List<int> SampleList = new List<int>();

        public static async Task<List<int>> YieldList()
        {
            await Task.Yield();
            return SampleList;
        }
    }
}