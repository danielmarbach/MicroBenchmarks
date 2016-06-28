using System;
using BenchmarkDotNet.Running;
using MicroBenchmarks.NServiceBus;

namespace MicroBenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<MessageHandlerRegistry>();
            Console.ReadLine();
        }
    }
}
