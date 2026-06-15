using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;

namespace RabbitMQ.Benchmarks
{
    internal enum ProtocolCommandId : uint
    {
        QueueDeclareOk = 0x00000001,
        BasicGetOk = 0x00000002,
        BasicAck = 0x00000003,
        BasicGetEmpty = 0x00000004,
    }

    // ---- Shared implementations ----

    /// <summary>
    /// Old implementation (Queue&lt;ProtocolCommandId[]&gt;) for baseline comparison.
    /// </summary>
    internal class OldRpcCancellationQueue
    {
        private readonly System.Collections.Generic.Queue<ProtocolCommandId[]> _queue = new();

        public void RpcCanceled(ProtocolCommandId[] protocolCommandIds)
        {
            _queue.Enqueue(protocolCommandIds);
        }

        public bool ShouldIgnoreCommand(ProtocolCommandId commandId)
        {
            if (_queue.Count > 0)
            {
                ProtocolCommandId[] lastErroredCommandIds = _queue.Dequeue();
                return lastErroredCommandIds.Contains(commandId);
            }
            return false;
        }
    }

    /// <summary>
    /// New implementation (readonly struct reinterpreted as long via Unsafe.As
    /// with Interlocked.Exchange and 0-as-sentinel) for lock-free atomic
    /// read-modify-write without a separate count field.
    /// </summary>
    internal class NewRpcCancellationQueue
    {
        private readonly struct LastTimedOutCommandIds(ProtocolCommandId first, ProtocolCommandId second = 0)
        {
            public readonly ProtocolCommandId First = first;
            public readonly ProtocolCommandId Second = second;
        }

        private long _lastTimedOutCommandIds;

        public void RpcCanceled(ReadOnlySpan<ProtocolCommandId> protocolCommandIds)
        {
            var ids = new LastTimedOutCommandIds(first: protocolCommandIds[0], second: protocolCommandIds.Length > 1 ? protocolCommandIds[1] : 0);
            Interlocked.Exchange(ref _lastTimedOutCommandIds, Unsafe.As<LastTimedOutCommandIds, long>(ref ids));
        }

        public bool ShouldIgnoreCommand(ProtocolCommandId commandId)
        {
            long raw = Interlocked.Exchange(ref _lastTimedOutCommandIds, 0L);

            if (raw == 0L)
            {
                return false;
            }

            LastTimedOutCommandIds ids = Unsafe.As<long, LastTimedOutCommandIds>(ref raw);
            return commandId == ids.First || (ids.Second != 0 && commandId == ids.Second);
        }
    }

    // ---- Benchmarks ----

    /// <summary>
    /// Benchmarks the RpcCanceled (write) path: Queue.Enqueue(array) vs CopyTo(InlineArray).
    /// </summary>
    [Config(typeof(Config))]
    [BenchmarkCategory("RpcContinuation")]
    public class RpcCanceledBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(DefaultExporters.Markdown);
            }
        }

        private OldRpcCancellationQueue _oldQueue = new();
        private NewRpcCancellationQueue _newQueue = new();

        // Single-element case (most common: simple RPC like QueueDeclareOk)
        private static readonly ProtocolCommandId[] SingleCommand = [ProtocolCommandId.QueueDeclareOk];

        // Two-element case (BasicGetOk / BasicGetEmpty, ConnectionSecure / ConnectionTune)
        private static readonly ProtocolCommandId[] TwoCommands = [ProtocolCommandId.BasicGetOk, ProtocolCommandId.BasicGetEmpty];

        [Params(1, 2)]
        public int CommandCount { get; set; }

        private ProtocolCommandId[] Commands => CommandCount == 1 ? SingleCommand : TwoCommands;

        [Benchmark(Baseline = true)]
        public void RpcCanceled_Old()
        {
            _oldQueue.RpcCanceled(Commands);
        }

        [Benchmark]
        public void RpcCanceled_New()
        {
            _newQueue.RpcCanceled(Commands);
        }
    }

    /// <summary>
    /// Benchmarks the ShouldIgnoreCommand (read) path:
    /// Queue.Dequeue + LINQ Contains vs InlineArray span scan.
    /// Includes the RpcCanceled write since ShouldIgnoreCommand consumes state.
    /// Also benchmarks the hot path where ShouldIgnoreCommand is called with no pending timeout (empty).
    /// </summary>
    [Config(typeof(Config))]
    public class ShouldIgnoreCommandBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(DefaultExporters.Markdown);
            }
        }

        private OldRpcCancellationQueue _oldQueue = new();
        private NewRpcCancellationQueue _newQueue = new();

        private static readonly ProtocolCommandId[] SingleCommand = [ProtocolCommandId.QueueDeclareOk];
        private static readonly ProtocolCommandId[] TwoCommands = [ProtocolCommandId.BasicGetOk, ProtocolCommandId.BasicGetEmpty];

        [Params(1, 2)]
        public int CommandCount { get; set; }

        private ProtocolCommandId[] Commands => CommandCount == 1 ? SingleCommand : TwoCommands;

        private ProtocolCommandId MatchingId => CommandCount == 1 ? ProtocolCommandId.QueueDeclareOk : ProtocolCommandId.BasicGetEmpty;

        private ProtocolCommandId NonMatchingId => ProtocolCommandId.BasicAck;

        // Hot path: no timeout occurred, ShouldIgnoreCommand checks an empty buffer/queue
        [Benchmark(
            )]
        public bool Empty_Old()
        {
            return _oldQueue.ShouldIgnoreCommand(NonMatchingId);
        }

        [Benchmark]
        public bool Empty_New()
        {
            return _newQueue.ShouldIgnoreCommand(NonMatchingId);
        }

        // Cold path: timeout occurred, command ID matches
        [Benchmark()]
        public bool Matching_Old()
        {
            _oldQueue.RpcCanceled(Commands);
            return _oldQueue.ShouldIgnoreCommand(MatchingId);
        }

        [Benchmark]
        public bool Matching_New()
        {
            _newQueue.RpcCanceled(Commands);
            return _newQueue.ShouldIgnoreCommand(MatchingId);
        }

        // Cold path: timeout occurred, command ID does not match
        [Benchmark()]
        public bool NonMatching_Old()
        {
            _oldQueue.RpcCanceled(Commands);
            return _oldQueue.ShouldIgnoreCommand(NonMatchingId);
        }

        [Benchmark]
        public bool NonMatching_New()
        {
            _newQueue.RpcCanceled(Commands);
            return _newQueue.ShouldIgnoreCommand(NonMatchingId);
        }
    }

    /// <summary>
    /// Full round-trip benchmark: RpcCanceled + ShouldIgnoreCommand with fresh instances,
    /// measuring total allocation difference.
    /// </summary>
    [Config(typeof(Config))]
    [BenchmarkCategory("RpcContinuation")]
    public class RoundTripBenchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddExporter(DefaultExporters.Markdown);
            }
        }

        private static readonly ProtocolCommandId[] SingleCommand = [ProtocolCommandId.QueueDeclareOk];
        private static readonly ProtocolCommandId[] TwoCommands = [ProtocolCommandId.BasicGetOk, ProtocolCommandId.BasicGetEmpty];

        [Params(1, 2)]
        public int CommandCount { get; set; }

        private ProtocolCommandId[] Commands => CommandCount == 1 ? SingleCommand : TwoCommands;

        private ProtocolCommandId MatchingId => CommandCount == 1 ? ProtocolCommandId.QueueDeclareOk : ProtocolCommandId.BasicGetEmpty;

        [Benchmark(Baseline = true)]
        public bool RoundTrip_Old()
        {
            var queue = new OldRpcCancellationQueue();
            queue.RpcCanceled(Commands);
            return queue.ShouldIgnoreCommand(MatchingId);
        }

        [Benchmark]
        public bool RoundTrip_New()
        {
            var queue = new NewRpcCancellationQueue();
            queue.RpcCanceled(Commands);
            return queue.ShouldIgnoreCommand(MatchingId);
        }
    }
}