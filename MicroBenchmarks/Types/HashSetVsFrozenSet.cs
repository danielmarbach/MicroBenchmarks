using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.Types;

[SimpleJob]
[MemoryDiagnoser]
public class HashSetVsFrozenSet
{
    [Benchmark(Baseline = true)]
    [Arguments("Microsoft.WindowsAzure")]
    [Arguments( "microsoft.windowsAzure")]
    public bool Hashset(string contains)
    {
        return AssemblyExclusionsHashset.Contains(contains);
    }
    
    [Benchmark]
    [Arguments("Microsoft.WindowsAzure")]
    [Arguments( "microsoft.windowsAzure")]
    public bool Frozenset(string contains)
    {
        return AssemblyExclusionsSet.Contains(contains);
    }
    
    private static readonly FrozenSet<string> AssemblyExclusionsSet =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // NSB Build-Dependencies
            "nunit",
            "nunit.framework",
            "nunit.applicationdomain",
            "nunit.engine",
            "nunit.engine.api",
            "nunit.engine.core",

            // NSB OSS Dependencies
            "nlog",
            "newtonsoft.json",
            "common.logging",
            "nhibernate",

            // Raven
            "raven.client",
            "raven.abstractions",

            // Azure host process, which is typically referenced for ease of deployment but should not be scanned
            "NServiceBus.Hosting.Azure.HostProcess.exe",

            // And other windows azure stuff
            "Microsoft.WindowsAzure"
        }.ToFrozenSet();
    
    private static readonly HashSet<string> AssemblyExclusionsHashset =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // NSB Build-Dependencies
            "nunit",
            "nunit.framework",
            "nunit.applicationdomain",
            "nunit.engine",
            "nunit.engine.api",
            "nunit.engine.core",

            // NSB OSS Dependencies
            "nlog",
            "newtonsoft.json",
            "common.logging",
            "nhibernate",

            // Raven
            "raven.client",
            "raven.abstractions",

            // Azure host process, which is typically referenced for ease of deployment but should not be scanned
            "NServiceBus.Hosting.Azure.HostProcess.exe",

            // And other windows azure stuff
            "Microsoft.WindowsAzure"
        };
}

[SimpleJob]
[MemoryDiagnoser]
public class HashSetVsFrozenSetCreation
{
    [Benchmark(Baseline = true)]
    public HashSet<string> Hashset()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // NSB Build-Dependencies
            "nunit",
            "nunit.framework",
            "nunit.applicationdomain",
            "nunit.engine",
            "nunit.engine.api",
            "nunit.engine.core",

            // NSB OSS Dependencies
            "nlog",
            "newtonsoft.json",
            "common.logging",
            "nhibernate",

            // Raven
            "raven.client",
            "raven.abstractions",

            // Azure host process, which is typically referenced for ease of deployment but should not be scanned
            "NServiceBus.Hosting.Azure.HostProcess.exe",

            // And other windows azure stuff
            "Microsoft.WindowsAzure"
        };
    }
    
    [Benchmark]
    public FrozenSet<string> Frozenset()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // NSB Build-Dependencies
            "nunit",
            "nunit.framework",
            "nunit.applicationdomain",
            "nunit.engine",
            "nunit.engine.api",
            "nunit.engine.core",

            // NSB OSS Dependencies
            "nlog",
            "newtonsoft.json",
            "common.logging",
            "nhibernate",

            // Raven
            "raven.client",
            "raven.abstractions",

            // Azure host process, which is typically referenced for ease of deployment but should not be scanned
            "NServiceBus.Hosting.Azure.HostProcess.exe",

            // And other windows azure stuff
            "Microsoft.WindowsAzure"
        }.ToFrozenSet();
    }
}