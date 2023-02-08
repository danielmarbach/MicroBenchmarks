using BenchmarkDotNet.Running;

namespace MicroBenchmarks;

class Program
{
    static void Main(string[] args)
    {
        var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
        switcher.Run(args);
    }
}