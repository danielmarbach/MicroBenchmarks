using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

namespace MicroBenchmarks.Async
{
    [Config(typeof(Config))]
    public class TcpAyendeStyle
    {
        class Config : ManualConfig
        {
            public Config()
            {
                Add(MarkdownExporter.GitHub);
                Add(StatisticColumn.AllStatistics);
            }
        }

        [Params(16 * 1024, 24 * 1024, 32 * 1024)]
        public int Length { get; set; }

        [Benchmark(Baseline = true)]
        public void Sync()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 9999);
            tcpListener.Start();
            var listenerThread = new Thread(() =>
            {
                var buffer = new byte[1024];
                var client = tcpListener.AcceptTcpClient();
                using (var stream = client.GetStream())
                {
                    while (true)
                    {
                        var read = stream.Read(buffer, 0, 1024);
                        if (read == 0)
                            break;
                    }
                }
            });
            listenerThread.Start();

            var tcpClient = new TcpClient();
            var sendBuffer = new byte[1024];
            new Random().NextBytes(sendBuffer);
            tcpClient.Connect(new IPEndPoint(IPAddress.Loopback, 9999));
            using (var sendStream = tcpClient.GetStream())
            {
                for (int i = 0; i < Length; i++)
                {
                    sendStream.Write(sendBuffer, 0, 1024);
                }
            }

            listenerThread.Join();
            tcpListener.Stop();
            tcpClient.Close();
            tcpClient.Dispose();
        }

        [Benchmark]
        public async Task Async()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 9999);
            tcpListener.Start();
            var listernerTask = Task.Run(async () =>
            {
                var buffer = new byte[1024];
                var client = await tcpListener.AcceptTcpClientAsync();
                using (var stream = client.GetStream())
                {
                    while (true)
                    {
                        var read = await stream.ReadAsync(buffer, 0, 1024);
                        if (read == 0)
                            break;
                    }
                }
            });

            var tcpClient = new TcpClient();
            var sendBuffer = new byte[1024];
            new Random().NextBytes(sendBuffer);
            await tcpClient.ConnectAsync(IPAddress.Loopback, 9999);
            using (var sendStream = tcpClient.GetStream())
            {
                for (int i = 0; i < Length; i++)
                {
                    await sendStream.WriteAsync(sendBuffer, 0, 1024);
                }
            }

            await listernerTask;
            tcpListener.Stop();
            tcpClient.Close();
            tcpClient.Dispose();
        }

        [Benchmark]
        public async Task AsyncConfigureAwait()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 9999);
            tcpListener.Start();
            var listernerTask = Task.Run(async () =>
            {
                var buffer = new byte[1024];
                var client = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                using (var stream = client.GetStream())
                {
                    while (true)
                    {
                        var read = await stream.ReadAsync(buffer, 0, 1024).ConfigureAwait(false);
                        if (read == 0)
                            break;
                    }
                }
            });

            var tcpClient = new TcpClient();
            var sendBuffer = new byte[1024];
            new Random().NextBytes(sendBuffer);
            await tcpClient.ConnectAsync(IPAddress.Loopback, 9999).ConfigureAwait(false);
            using (var sendStream = tcpClient.GetStream())
            {
                for (int i = 0; i < Length; i++)
                {
                    await sendStream.WriteAsync(sendBuffer, 0, 1024).ConfigureAwait(false);
                }
            }

            await listernerTask.ConfigureAwait(false);
            tcpListener.Stop();
            tcpClient.Close();
            tcpClient.Dispose();
        }
    }
}
