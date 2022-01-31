using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.LowLevel;

[Config(typeof(Config))]
public class ArrayAccessOrUnsafe
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddExporter(MarkdownExporter.GitHub);
            AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(printSource: true, exportDiff: true)));
            AddJob(Job.Default.WithInvocationCount(512000));
        }
    }

    private Consumer consumer = new Consumer();

    [GlobalSetup]
    public void Setup()
    {
        someInterfaces = new ISomeInterface[] {new A(), new B(),new A(), new B(), new A(), new B() };
    }

    private ISomeInterface[] someInterfaces;
        
    [Benchmark(Baseline = true)]
    public void ArrayAccess()
    {
        consumer.Consume(((A)someInterfaces[0]).DoA());
        consumer.Consume(((B)someInterfaces[1]).DoB());
        consumer.Consume(((A)someInterfaces[2]).DoA());
        consumer.Consume(((B)someInterfaces[3]).DoB());
        consumer.Consume(((A)someInterfaces[4]).DoA());
        consumer.Consume(((B)someInterfaces[5]).DoB());
    }
        
    [Benchmark()]
    public unsafe void UnsafeArrayAccess()
    {
        consumer.Consume(Unsafe.Read<A>(Unsafe.AsPointer(ref someInterfaces[0])).DoA());
        consumer.Consume(Unsafe.Read<B>(Unsafe.AsPointer(ref someInterfaces[1])).DoB());
        consumer.Consume(Unsafe.Read<A>(Unsafe.AsPointer(ref someInterfaces[2])).DoA());
        consumer.Consume(Unsafe.Read<B>(Unsafe.AsPointer(ref someInterfaces[3])).DoB());
        consumer.Consume(Unsafe.Read<A>(Unsafe.AsPointer(ref someInterfaces[4])).DoA());
        consumer.Consume(Unsafe.Read<B>(Unsafe.AsPointer(ref someInterfaces[5])).DoB());
    }
    [Benchmark()]
    public void UnsafeAs()
    {
        consumer.Consume(Unsafe.As<A>(someInterfaces[0]).DoA());
        consumer.Consume(Unsafe.As<B>(someInterfaces[1]).DoB());
        consumer.Consume(Unsafe.As<A>(someInterfaces[2]).DoA());
        consumer.Consume(Unsafe.As<B>(someInterfaces[3]).DoB());
        consumer.Consume(Unsafe.As<A>(someInterfaces[4]).DoA());
        consumer.Consume(Unsafe.As<B>(someInterfaces[5]).DoB());
    }
    
    [Benchmark()]
    public void UnsafeAsFrom()
    {
        consumer.Consume(Unsafe.As<ISomeInterface, A>(ref someInterfaces[0]).DoA());
        consumer.Consume(Unsafe.As<ISomeInterface,B>(ref someInterfaces[1]).DoB());
        consumer.Consume(Unsafe.As<ISomeInterface,A>(ref someInterfaces[2]).DoA());
        consumer.Consume(Unsafe.As<ISomeInterface,B>(ref someInterfaces[3]).DoB());
        consumer.Consume(Unsafe.As<ISomeInterface,A>(ref someInterfaces[4]).DoA());
        consumer.Consume(Unsafe.As<ISomeInterface,B>(ref someInterfaces[5]).DoB());
    }
    

    public interface ISomeInterface
    {
            
    }

    public class A : ISomeInterface
    {
        public int DoA()
        {
            return 42;
        }
    }

    public class B : ISomeInterface
    {
        public int DoB()
        {
            return 42;
        }
    }
}