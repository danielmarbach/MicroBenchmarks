using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Reflection;

[SimpleJob]
public class TypeCustomAttributeVsAttribute
{
    private Type type;

    [GlobalSetup]
    public void Setup()
    {
        type = typeof(SomeClass);
    }
    
    [Benchmark(Baseline = true)]
    public SomeAttribute? Type()
    {
        return type.GetCustomAttribute<SomeAttribute>(false);
    }
    
    [Benchmark]
    public Attribute? AttributeBased()
    {
        return Attribute.GetCustomAttribute(type, typeof(SomeAttribute), false);
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SomeAttribute : Attribute {}
    
    [Some]
    class SomeClass {}
}