using System;
using BenchmarkDotNet.Running;

namespace MicroBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BatchedVsImmediateSimulated>();
            Console.WriteLine(summary.ToString());
            Console.ReadLine();
        }
    }
}
