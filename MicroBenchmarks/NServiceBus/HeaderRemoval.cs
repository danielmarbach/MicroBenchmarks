using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;

[Config(typeof(Config))]
public class HeaderRemoval
{
    private class Config : ManualConfig
    {
        public Config()
        {
            Add(MarkdownExporter.GitHub);
            Add(StatisticColumn.AllStatistics);
        }
    }

    private Dictionary<string, string> headers;

    [GlobalSetup]
    public void Setup()
    {
        headers = new Dictionary<string, string>
        {
            { "NServiceBus.MessageId", "caf68027-acce-4260-8442-ac6500e46afc" },
            { "NServiceBus.MessageIntent", "Publish" },
            { "NServiceBus.RelatedTo", "2e6c6c9c-c4cb-474e-98b8-ac6500e46a0c" },
            { "NServiceBus.ConversationId", "37c405c8-e4e4-4873-8eb4-ac6500e46621" },
            { "NServiceBus.CorrelationId", "6e449174-6f77-4da4-b9c0-ac6500e46621" },
            { "NServiceBus.OriginatingMachine", "MACHINE"},
            { "NServiceBus.OriginatingEndpoint", "PurchaseOrderService.1.0" },
            { "$.diagnostics.originating.hostid", "4f8138bdb0421ffe1ceaee86e9145721" },
            { "NServiceBus.OriginatingSagaId", "9e0d2f01-e903-481a-b272-ac6500e46715" },
            { "NServiceBus.OriginatingSagaType", "PowerSupplyPurchaseOrderService.PurchaseOrderSaga, PowerSupplyPurchaseOrderService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" },
            { "NServiceBus.ReplyToAddress", "PowerSupplyPurchaseOrderService.1.0@[dbo]@[Market.NServiceBus.Prod]" },
            { "NServiceBus.ContentType", "application/json" },
            { "NServiceBus.EnclosedMessageTypes", "PowerSupplyPurchaseOrderService.ApiModels.Events.v1_0.PowerSupplyDebtorBlacklistCheckCompleted, PowerSupplyOrderService.ApiModels, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" },
            { "NServiceBus.Version", "7.1.0" },
            { "NServiceBus.TimeSent", "2020-10-31 13:59:26:479745 Z" },
            { "NServiceBus.Retries.Timestamp", "2020-10-31 13:56:55:359877 Z" },
            { "NServiceBus.Timeout.RouteExpiredTimeoutTo", "Crm.PowerSupplySalesOrderManager.1.0@[dbo]@[Market.NServiceBus.Prod]" },
            { "NServiceBus.Timeout.Expire", "2020-10-31 13,59,25,359877 Z" },
            { "NServiceBus.RelatedToTimeoutId", "2c6aa21f-7e73-4142-de86-08d87432ffe4" },
            { "NServiceBus.ExceptionInfo.Data.Message type", "PowerSupplyPurchaseOrderService.ApiModels.Events.v1_0.PowerSupplyDebtorBlacklistCheckCompleted" },
            { "NServiceBus.ExceptionInfo.Data.Handler type", "Crm.PowerSupplySalesOrderManager.MessageHandlers.DebtorBlacklistCheckCompletedHandler" },
            { "NServiceBus.ExceptionInfo.Data.Handler start time", "31-10-2020 13:59:28" },
            { "NServiceBus.ExceptionInfo.Data.Handler failure time", "31-10-2020 13:59:28" },
            { "NServiceBus.ExceptionInfo.Data.Message ID", "caf68027-acce-4260-8442-ac6500e46afc" },
            { "NServiceBus.ExceptionInfo.Data.Transport message ID", "320fb8cb-ad20-48e9-a111-454ebe43a7a8" },
            { "NServiceBus.ExceptionInfo.Data.Custom Entry", "Custom" },
            { "NServiceBus.ProcessingMachine", "MACHINE" },
            { "NServiceBus.ProcessingEndpoint", "SeasNve.Market.Crm.PowerSupplySalesOrderManager.1.0" },
            { "$.diagnostics.hostid", "8d8fcac767fbd7199024c5cae57adde5" },
            { "$.diagnostics.hostdisplayname", "MACHINE" },
            { "ServiceControl.Retry.UniqueMessageId", "a5f6da09-5f3f-5394-c09c-dffbe99c357a" },
            { "NServiceBus.ProcessingStarted", "2020-11-02 08:07:44:650218 Z" },
            { "NServiceBus.ProcessingEnded", "2020-11-02 08:07:44:837731 Z" }
        };
    }

    [Benchmark(Baseline = true)]
    public Dictionary<string, string> Before()
    {
        return HeaderFilterBefore.RemoveErrorMessageHeaders(headers);
    }

    [Benchmark()]
    public Dictionary<string, string> After()
    {
        return HeaderFilterAfter.RemoveErrorMessageHeaders(headers);
    }

    static class HeaderFilterBefore
    {
        public static Dictionary<string, string> RemoveErrorMessageHeaders(Dictionary<string, string> headers)
        {
            var headersToRetryWith = headers
                .Where(kv => !KeysToRemoveWhenRetryingAMessage.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            return headersToRetryWith;
        }

        static readonly string[] KeysToRemoveWhenRetryingAMessage =
        {
            "NServiceBus.Retries",
            "NServiceBus.Retries.Timestamp",
            "NServiceBus.FailedQ",
            "NServiceBus.TimeOfFailure",
            "NServiceBus.ExceptionInfo.ExceptionType",
            "NServiceBus.ExceptionInfo.AuditMessage",
            "NServiceBus.ExceptionInfo.Source",
            "NServiceBus.ExceptionInfo.StackTrace",
            "NServiceBus.ExceptionInfo.HelpLink",
            "NServiceBus.ExceptionInfo.Message",
            "NServiceBus.ExceptionInfo.Data.Message type",
            "NServiceBus.ExceptionInfo.Data.Handler type",
            "NServiceBus.ExceptionInfo.Data.Handler start time",
            "NServiceBus.ExceptionInfo.Data.Handler failure time",
            "NServiceBus.ExceptionInfo.Data.Message ID",
            "NServiceBus.ExceptionInfo.Data.Transport message ID",
            "NServiceBus.ExceptionInfo.Data.Custom Entry",
            "ServiceControl.EditOf"
        };
    }

    static class HeaderFilterAfter
    {
        public static Dictionary<string, string> RemoveErrorMessageHeaders(Dictionary<string, string> headers)
        {
            // still take a copy to preserve the old assumptions
            var headersToRetryWith = new Dictionary<string, string>(headers);
            foreach (var headerToRemove in KeysToRemoveWhenRetryingAMessage)
            {
                // iterate over original so that we are not running into modified collection problem
                foreach (var keyValuePair in headers)
                {
                    if (keyValuePair.Key.StartsWith(headerToRemove, StringComparison.Ordinal))
                    {
                        headersToRetryWith.Remove(keyValuePair.Key);
                    }
                }
            }
            return headersToRetryWith;
        }

        static readonly string[] KeysToRemoveWhenRetryingAMessage =
        {
            "NServiceBus.Retries",
            "NServiceBus.FailedQ",
            "NServiceBus.TimeOfFailure",
            "NServiceBus.ExceptionInfo.",
            "ServiceControl.EditOf"
        };
    }
}