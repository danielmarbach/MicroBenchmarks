using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace MicroBenchmarks.NServiceBus;

[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
public class ContextBagVariants
{
    struct Slot
    {
        public string? Key;
        public object? Value;
    }

    [InlineArray(4)]
    struct SlotArray4 { private Slot _element0; }

    [InlineArray(8)]
    struct SlotArray8 { private Slot _element0; }

    // ============================================================
    //   0: Dictionary-only baseline
    // ============================================================
    public sealed class BagDictOnly
    {
        Dictionary<string, object>? stash;

        public void Set(string key, object value) => (stash ??= [])[key] = value;

        public object Get(string key)
        {
            if (stash?.TryGetValue(key, out var v) == true) return v!;
            throw new KeyNotFoundException(key);
        }
    }

    // ============================================================
    //   1: 4 manually unrolled fields (current ContextBag)
    // ============================================================
    public sealed class Bag4Field
    {
        string? k0; object? v0;
        string? k1; object? v1;
        string? k2; object? v2;
        string? k3; object? v3;
        Dictionary<string, object>? stash;

        public void Set(string key, object value)
        {
            if (!TrySetInline(key, value))
                (stash ??= [])[key] = value;
        }

        public object Get(string key)
        {
            if (TryGetInline(key, out var v)) return v!;
            if (stash?.TryGetValue(key, out v) == true) return v!;
            throw new KeyNotFoundException(key);
        }

        bool TrySetInline(string key, object value)
        {
            if (k0 is null) { k0 = key; v0 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k0)) { v0 = value; return true; }
            if (k1 is null) { k1 = key; v1 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k1)) { v1 = value; return true; }
            if (k2 is null) { k2 = key; v2 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k2)) { v2 = value; return true; }
            if (k3 is null) { k3 = key; v3 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k3)) { v3 = value; return true; }
            return false;
        }

        bool TryGetInline(string key, out object? value)
        {
            if (k0 is not null && StringComparer.Ordinal.Equals(key, k0)) { value = v0; return true; }
            if (k1 is not null && StringComparer.Ordinal.Equals(key, k1)) { value = v1; return true; }
            if (k2 is not null && StringComparer.Ordinal.Equals(key, k2)) { value = v2; return true; }
            if (k3 is not null && StringComparer.Ordinal.Equals(key, k3)) { value = v3; return true; }
            value = null;
            return false;
        }
    }

    // ============================================================
    //   2: 8 manually unrolled fields
    // ============================================================
    public sealed class Bag8Field
    {
        string? k0; object? v0;
        string? k1; object? v1;
        string? k2; object? v2;
        string? k3; object? v3;
        string? k4; object? v4;
        string? k5; object? v5;
        string? k6; object? v6;
        string? k7; object? v7;
        Dictionary<string, object>? stash;

        public void Set(string key, object value)
        {
            if (!TrySetInline(key, value))
                (stash ??= [])[key] = value;
        }

        public object Get(string key)
        {
            if (TryGetInline(key, out var v)) return v!;
            if (stash?.TryGetValue(key, out v) == true) return v!;
            throw new KeyNotFoundException(key);
        }

        bool TrySetInline(string key, object value)
        {
            if (k0 is null) { k0 = key; v0 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k0)) { v0 = value; return true; }
            if (k1 is null) { k1 = key; v1 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k1)) { v1 = value; return true; }
            if (k2 is null) { k2 = key; v2 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k2)) { v2 = value; return true; }
            if (k3 is null) { k3 = key; v3 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k3)) { v3 = value; return true; }
            if (k4 is null) { k4 = key; v4 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k4)) { v4 = value; return true; }
            if (k5 is null) { k5 = key; v5 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k5)) { v5 = value; return true; }
            if (k6 is null) { k6 = key; v6 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k6)) { v6 = value; return true; }
            if (k7 is null) { k7 = key; v7 = value; return true; }
            if (StringComparer.Ordinal.Equals(key, k7)) { v7 = value; return true; }
            return false;
        }

        bool TryGetInline(string key, out object? value)
        {
            if (k0 is not null && StringComparer.Ordinal.Equals(key, k0)) { value = v0; return true; }
            if (k1 is not null && StringComparer.Ordinal.Equals(key, k1)) { value = v1; return true; }
            if (k2 is not null && StringComparer.Ordinal.Equals(key, k2)) { value = v2; return true; }
            if (k3 is not null && StringComparer.Ordinal.Equals(key, k3)) { value = v3; return true; }
            if (k4 is not null && StringComparer.Ordinal.Equals(key, k4)) { value = v4; return true; }
            if (k5 is not null && StringComparer.Ordinal.Equals(key, k5)) { value = v5; return true; }
            if (k6 is not null && StringComparer.Ordinal.Equals(key, k6)) { value = v6; return true; }
            if (k7 is not null && StringComparer.Ordinal.Equals(key, k7)) { value = v7; return true; }
            value = null;
            return false;
        }
    }

    // ============================================================
    //   3: InlineArray(4) — direct indexer, counted for loop
    // ============================================================
    public sealed class BagInlineArray4
    {
        SlotArray4 _slots;
        int _count;
        Dictionary<string, object>? stash;

        public void Set(string key, object value)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (StringComparer.Ordinal.Equals(key, slot.Key))
                {
                    slot.Value = value;
                    return;
                }
            }
            if (_count < 4)
            {
                ref var slot = ref _slots[_count];
                slot.Key = key;
                slot.Value = value;
                _count++;
                return;
            }
            (stash ??= [])[key] = value;
        }

        public object Get(string key)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (slot.Key is not null && StringComparer.Ordinal.Equals(key, slot.Key))
                    return slot.Value!;
            }
            if (stash?.TryGetValue(key, out var v) == true)
                return v!;
            throw new KeyNotFoundException(key);
        }
    }

    // ============================================================
    //   4: InlineArray(8) — direct indexer, counted for loop
    // ============================================================
    public sealed class BagInlineArray8
    {
        SlotArray8 _slots;
        int _count;
        Dictionary<string, object>? stash;

        public void Set(string key, object value)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (StringComparer.Ordinal.Equals(key, slot.Key))
                {
                    slot.Value = value;
                    return;
                }
            }
            if (_count < 8)
            {
                ref var slot = ref _slots[_count];
                slot.Key = key;
                slot.Value = value;
                _count++;
                return;
            }
            (stash ??= [])[key] = value;
        }

        public object Get(string key)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (slot.Key is not null && StringComparer.Ordinal.Equals(key, slot.Key))
                    return slot.Value!;
            }
            if (stash?.TryGetValue(key, out var v) == true)
                return v!;
            throw new KeyNotFoundException(key);
        }
    }

    // ============================================================
    //   5: InlineArray(4) — RefEquals short-circuit
    // ============================================================
    public sealed class BagInlineArray4_RefEq
    {
        SlotArray4 _slots;
        int _count;
        Dictionary<string, object>? stash;

        public void Set(string key, object value)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (ReferenceEquals(key, slot.Key) || StringComparer.Ordinal.Equals(key, slot.Key))
                {
                    slot.Value = value;
                    return;
                }
            }
            if (_count < 4)
            {
                ref var slot = ref _slots[_count];
                slot.Key = key;
                slot.Value = value;
                _count++;
                return;
            }
            (stash ??= [])[key] = value;
        }

        public object Get(string key)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (slot.Key is not null && (ReferenceEquals(key, slot.Key) || StringComparer.Ordinal.Equals(key, slot.Key)))
                    return slot.Value!;
            }
            if (stash?.TryGetValue(key, out var v) == true)
                return v!;
            throw new KeyNotFoundException(key);
        }
    }

    // ============================================================
    //   6: InlineArray(8) — RefEquals short-circuit
    // ============================================================
    public sealed class BagInlineArray8_RefEq
    {
        SlotArray8 _slots;
        int _count;
        Dictionary<string, object>? stash;

        public void Set(string key, object value)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (ReferenceEquals(key, slot.Key) || StringComparer.Ordinal.Equals(key, slot.Key))
                {
                    slot.Value = value;
                    return;
                }
            }
            if (_count < 8)
            {
                ref var slot = ref _slots[_count];
                slot.Key = key;
                slot.Value = value;
                _count++;
                return;
            }
            (stash ??= [])[key] = value;
        }

        public object Get(string key)
        {
            for (int i = 0; i < _count; i++)
            {
                ref var slot = ref _slots[i];
                if (slot.Key is not null && (ReferenceEquals(key, slot.Key) || StringComparer.Ordinal.Equals(key, slot.Key)))
                    return slot.Value!;
            }
            if (stash?.TryGetValue(key, out var v) == true)
                return v!;
            throw new KeyNotFoundException(key);
        }
    }

    // ============================================================
    //   CONSTANTS
    // ============================================================
    const string K1 = "NServiceBus.MessageId";
    const string K2 = "NServiceBus.ConversationId";
    const string K3 = "NServiceBus.CorrelationId";
    const string K4 = "NServiceBus.OriginatingEndpoint";
    const string K5 = "NServiceBus.ReplyToAddress";
    const string K6 = "NServiceBus.ContentType";
    const string K7 = "NServiceBus.Version";
    const string K8 = "NServiceBus.TimeSent";
    const string K9  = "NServiceBus.ProcessingMachine";
    const string K10 = "NServiceBus.ProcessingEndpoint";
    const string K11 = "NServiceBus.OriginatingMachine";
    const string K12 = "NServiceBus.OriginatingSagaId";

    const string V1 = "caf68027-acce-4260-8442-ac6500e46afc";
    const string V2 = "37c405c8-e4e4-4873-8eb4-ac6500e46621";
    const string V3 = "6e449174-6f77-4da4-b9c0-ac6500e46621";
    const string V4 = "PurchaseOrderService.1.0";
    const string V5 = "PowerSupplyPurchaseOrderService.1.0@[dbo]@[Market.NServiceBus.Prod]";
    const string V6 = "application/json";
    const string V7 = "7.1.0";
    const string V8 = "2020-10-31 13:59:26:479745 Z";
    const string V9  = "MACHINE";
    const string V10 = "SeasNve.Market.Crm.PowerSupplySalesOrderManager.1.0";
    const string V11 = "MACHINE";
    const string V12 = "9e0d2f01-e903-481a-b272-ac6500e46715";

    // ============================================================
    //   PRE-POPULATED BAGS FOR GET BENCHMARKS
    // ============================================================
    BagDictOnly _dict_4, _dict_8, _dict_12;
    Bag4Field _4f_4, _4f_8, _4f_12;
    Bag8Field _8f_4, _8f_8, _8f_12;
    BagInlineArray4 _ia4_4, _ia4_8, _ia4_12;
    BagInlineArray8 _ia8_4, _ia8_8, _ia8_12;
    BagInlineArray4_RefEq _ia4re_4, _ia4re_8, _ia4re_12;
    BagInlineArray8_RefEq _ia8re_4, _ia8re_8, _ia8re_12;

    [GlobalSetup]
    public void GlobalSetup()
    {
        (_dict_4 = new BagDictOnly()).Set(K1, V1); _dict_4.Set(K2, V2); _dict_4.Set(K3, V3); _dict_4.Set(K4, V4);
        (_4f_4 = new Bag4Field()).Set(K1, V1); _4f_4.Set(K2, V2); _4f_4.Set(K3, V3); _4f_4.Set(K4, V4);
        (_8f_4 = new Bag8Field()).Set(K1, V1); _8f_4.Set(K2, V2); _8f_4.Set(K3, V3); _8f_4.Set(K4, V4);
        (_ia4_4 = new BagInlineArray4()).Set(K1, V1); _ia4_4.Set(K2, V2); _ia4_4.Set(K3, V3); _ia4_4.Set(K4, V4);
        (_ia8_4 = new BagInlineArray8()).Set(K1, V1); _ia8_4.Set(K2, V2); _ia8_4.Set(K3, V3); _ia8_4.Set(K4, V4);
        (_ia4re_4 = new BagInlineArray4_RefEq()).Set(K1, V1); _ia4re_4.Set(K2, V2); _ia4re_4.Set(K3, V3); _ia4re_4.Set(K4, V4);
        (_ia8re_4 = new BagInlineArray8_RefEq()).Set(K1, V1); _ia8re_4.Set(K2, V2); _ia8re_4.Set(K3, V3); _ia8re_4.Set(K4, V4);

        (_dict_8 = new BagDictOnly()).Set(K1, V1); _dict_8.Set(K2, V2); _dict_8.Set(K3, V3); _dict_8.Set(K4, V4);
        _dict_8.Set(K5, V5); _dict_8.Set(K6, V6); _dict_8.Set(K7, V7); _dict_8.Set(K8, V8);
        (_4f_8 = new Bag4Field()).Set(K1, V1); _4f_8.Set(K2, V2); _4f_8.Set(K3, V3); _4f_8.Set(K4, V4);
        _4f_8.Set(K5, V5); _4f_8.Set(K6, V6); _4f_8.Set(K7, V7); _4f_8.Set(K8, V8);
        (_8f_8 = new Bag8Field()).Set(K1, V1); _8f_8.Set(K2, V2); _8f_8.Set(K3, V3); _8f_8.Set(K4, V4);
        _8f_8.Set(K5, V5); _8f_8.Set(K6, V6); _8f_8.Set(K7, V7); _8f_8.Set(K8, V8);
        (_ia4_8 = new BagInlineArray4()).Set(K1, V1); _ia4_8.Set(K2, V2); _ia4_8.Set(K3, V3); _ia4_8.Set(K4, V4);
        _ia4_8.Set(K5, V5); _ia4_8.Set(K6, V6); _ia4_8.Set(K7, V7); _ia4_8.Set(K8, V8);
        (_ia8_8 = new BagInlineArray8()).Set(K1, V1); _ia8_8.Set(K2, V2); _ia8_8.Set(K3, V3); _ia8_8.Set(K4, V4);
        _ia8_8.Set(K5, V5); _ia8_8.Set(K6, V6); _ia8_8.Set(K7, V7); _ia8_8.Set(K8, V8);
        (_ia4re_8 = new BagInlineArray4_RefEq()).Set(K1, V1); _ia4re_8.Set(K2, V2); _ia4re_8.Set(K3, V3); _ia4re_8.Set(K4, V4);
        _ia4re_8.Set(K5, V5); _ia4re_8.Set(K6, V6); _ia4re_8.Set(K7, V7); _ia4re_8.Set(K8, V8);
        (_ia8re_8 = new BagInlineArray8_RefEq()).Set(K1, V1); _ia8re_8.Set(K2, V2); _ia8re_8.Set(K3, V3); _ia8re_8.Set(K4, V4);
        _ia8re_8.Set(K5, V5); _ia8re_8.Set(K6, V6); _ia8re_8.Set(K7, V7); _ia8re_8.Set(K8, V8);

        (_dict_12 = new BagDictOnly()).Set(K1, V1); _dict_12.Set(K2, V2); _dict_12.Set(K3, V3); _dict_12.Set(K4, V4);
        _dict_12.Set(K5, V5); _dict_12.Set(K6, V6); _dict_12.Set(K7, V7); _dict_12.Set(K8, V8);
        _dict_12.Set(K9, V9); _dict_12.Set(K10, V10); _dict_12.Set(K11, V11); _dict_12.Set(K12, V12);
        (_4f_12 = new Bag4Field()).Set(K1, V1); _4f_12.Set(K2, V2); _4f_12.Set(K3, V3); _4f_12.Set(K4, V4);
        _4f_12.Set(K5, V5); _4f_12.Set(K6, V6); _4f_12.Set(K7, V7); _4f_12.Set(K8, V8);
        _4f_12.Set(K9, V9); _4f_12.Set(K10, V10); _4f_12.Set(K11, V11); _4f_12.Set(K12, V12);
        (_8f_12 = new Bag8Field()).Set(K1, V1); _8f_12.Set(K2, V2); _8f_12.Set(K3, V3); _8f_12.Set(K4, V4);
        _8f_12.Set(K5, V5); _8f_12.Set(K6, V6); _8f_12.Set(K7, V7); _8f_12.Set(K8, V8);
        _8f_12.Set(K9, V9); _8f_12.Set(K10, V10); _8f_12.Set(K11, V11); _8f_12.Set(K12, V12);
        (_ia4_12 = new BagInlineArray4()).Set(K1, V1); _ia4_12.Set(K2, V2); _ia4_12.Set(K3, V3); _ia4_12.Set(K4, V4);
        _ia4_12.Set(K5, V5); _ia4_12.Set(K6, V6); _ia4_12.Set(K7, V7); _ia4_12.Set(K8, V8);
        _ia4_12.Set(K9, V9); _ia4_12.Set(K10, V10); _ia4_12.Set(K11, V11); _ia4_12.Set(K12, V12);
        (_ia8_12 = new BagInlineArray8()).Set(K1, V1); _ia8_12.Set(K2, V2); _ia8_12.Set(K3, V3); _ia8_12.Set(K4, V4);
        _ia8_12.Set(K5, V5); _ia8_12.Set(K6, V6); _ia8_12.Set(K7, V7); _ia8_12.Set(K8, V8);
        _ia8_12.Set(K9, V9); _ia8_12.Set(K10, V10); _ia8_12.Set(K11, V11); _ia8_12.Set(K12, V12);
        (_ia4re_12 = new BagInlineArray4_RefEq()).Set(K1, V1); _ia4re_12.Set(K2, V2); _ia4re_12.Set(K3, V3); _ia4re_12.Set(K4, V4);
        _ia4re_12.Set(K5, V5); _ia4re_12.Set(K6, V6); _ia4re_12.Set(K7, V7); _ia4re_12.Set(K8, V8);
        _ia4re_12.Set(K9, V9); _ia4re_12.Set(K10, V10); _ia4re_12.Set(K11, V11); _ia4re_12.Set(K12, V12);
        (_ia8re_12 = new BagInlineArray8_RefEq()).Set(K1, V1); _ia8re_12.Set(K2, V2); _ia8re_12.Set(K3, V3); _ia8re_12.Set(K4, V4);
        _ia8re_12.Set(K5, V5); _ia8re_12.Set(K6, V6); _ia8re_12.Set(K7, V7); _ia8re_12.Set(K8, V8);
        _ia8re_12.Set(K9, V9); _ia8re_12.Set(K10, V10); _ia8re_12.Set(K11, V11); _ia8re_12.Set(K12, V12);
    }

    // ================================================================
    //  SET 4 — all return bag to prevent JIT elision
    // ================================================================

    [Benchmark(Baseline = true)]
    public BagDictOnly Set4_DictOnly()
    {
        var b = new BagDictOnly();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    [Benchmark]
    public Bag4Field Set4_4Field()
    {
        var b = new Bag4Field();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    [Benchmark]
    public Bag8Field Set4_8Field()
    {
        var b = new Bag8Field();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    [Benchmark]
    public BagInlineArray4 Set4_InlineArray4()
    {
        var b = new BagInlineArray4();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    [Benchmark]
    public BagInlineArray8 Set4_InlineArray8()
    {
        var b = new BagInlineArray8();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    [Benchmark]
    public BagInlineArray4_RefEq Set4_InlineArray4_RefEq()
    {
        var b = new BagInlineArray4_RefEq();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    [Benchmark]
    public BagInlineArray8_RefEq Set4_InlineArray8_RefEq()
    {
        var b = new BagInlineArray8_RefEq();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        return b;
    }

    // ================================================================
    //  SET 8
    // ================================================================

    [Benchmark]
    public BagDictOnly Set8_DictOnly()
    {
        var b = new BagDictOnly();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    [Benchmark]
    public Bag4Field Set8_4Field()
    {
        var b = new Bag4Field();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    [Benchmark]
    public Bag8Field Set8_8Field()
    {
        var b = new Bag8Field();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    [Benchmark]
    public BagInlineArray4 Set8_InlineArray4()
    {
        var b = new BagInlineArray4();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    [Benchmark]
    public BagInlineArray8 Set8_InlineArray8()
    {
        var b = new BagInlineArray8();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    [Benchmark]
    public BagInlineArray4_RefEq Set8_InlineArray4_RefEq()
    {
        var b = new BagInlineArray4_RefEq();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    [Benchmark]
    public BagInlineArray8_RefEq Set8_InlineArray8_RefEq()
    {
        var b = new BagInlineArray8_RefEq();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        return b;
    }

    // ================================================================
    //  SET 12 — all overflow to stash
    // ================================================================

    [Benchmark]
    public BagDictOnly Set12_DictOnly()
    {
        var b = new BagDictOnly();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    [Benchmark]
    public Bag4Field Set12_4Field()
    {
        var b = new Bag4Field();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    [Benchmark]
    public Bag8Field Set12_8Field()
    {
        var b = new Bag8Field();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    [Benchmark]
    public BagInlineArray4 Set12_InlineArray4()
    {
        var b = new BagInlineArray4();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    [Benchmark]
    public BagInlineArray8 Set12_InlineArray8()
    {
        var b = new BagInlineArray8();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    [Benchmark]
    public BagInlineArray4_RefEq Set12_InlineArray4_RefEq()
    {
        var b = new BagInlineArray4_RefEq();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    [Benchmark]
    public BagInlineArray8_RefEq Set12_InlineArray8_RefEq()
    {
        var b = new BagInlineArray8_RefEq();
        b.Set(K1, V1); b.Set(K2, V2); b.Set(K3, V3); b.Set(K4, V4);
        b.Set(K5, V5); b.Set(K6, V6); b.Set(K7, V7); b.Set(K8, V8);
        b.Set(K9, V9); b.Set(K10, V10); b.Set(K11, V11); b.Set(K12, V12);
        return b;
    }

    // ================================================================
    //  GET 4
    // ================================================================

    [Benchmark]
    public object Get4_DictOnly() => _dict_4.Get(K4);

    [Benchmark]
    public object Get4_4Field() => _4f_4.Get(K4);

    [Benchmark]
    public object Get4_8Field() => _8f_4.Get(K4);

    [Benchmark]
    public object Get4_InlineArray4() => _ia4_4.Get(K4);

    [Benchmark]
    public object Get4_InlineArray8() => _ia8_4.Get(K4);

    [Benchmark]
    public object Get4_InlineArray4_RefEq() => _ia4re_4.Get(K4);

    [Benchmark]
    public object Get4_InlineArray8_RefEq() => _ia8re_4.Get(K4);

    // ================================================================
    //  GET 8
    // ================================================================

    [Benchmark]
    public object Get8_DictOnly() => _dict_8.Get(K8);

    [Benchmark]
    public object Get8_4Field() => _4f_8.Get(K8);

    [Benchmark]
    public object Get8_8Field() => _8f_8.Get(K8);

    [Benchmark]
    public object Get8_InlineArray4() => _ia4_8.Get(K8);

    [Benchmark]
    public object Get8_InlineArray8() => _ia8_8.Get(K8);

    [Benchmark]
    public object Get8_InlineArray4_RefEq() => _ia4re_8.Get(K8);

    [Benchmark]
    public object Get8_InlineArray8_RefEq() => _ia8re_8.Get(K8);

    // ================================================================
    //  GET 12
    // ================================================================

    [Benchmark]
    public object Get12_DictOnly() => _dict_12.Get(K12);

    [Benchmark]
    public object Get12_4Field() => _4f_12.Get(K12);

    [Benchmark]
    public object Get12_8Field() => _8f_12.Get(K12);

    [Benchmark]
    public object Get12_InlineArray4() => _ia4_12.Get(K12);

    [Benchmark]
    public object Get12_InlineArray8() => _ia8_12.Get(K12);

    [Benchmark]
    public object Get12_InlineArray4_RefEq() => _ia4re_12.Get(K12);

    [Benchmark]
    public object Get12_InlineArray8_RefEq() => _ia8re_12.Get(K12);
}