using System;
using BenchmarkDotNet.Running;
using MicroBenchmarks.NServiceBus;
using MicroBenchmarks.Tasks;
using System.Linq;

namespace MicroBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        { 
            var switcher = new BenchmarkSwitcher(typeof(Program).Assembly);
            switcher.Run(args);
        }
    }
}
