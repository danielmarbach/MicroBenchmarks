using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MicroBenchmarks.DictionaryTable;

static class DictionaryTableEntityExtensionsNew
{
    public static TEntity ToSagaDataNew<TEntity>(DictionaryTableEntity entity)
    {
        return (TEntity)ToSagaData(typeof(TEntity), entity);
    }

    private static object ToSagaData(Type sagaDataType, DictionaryTableEntity entity)
    {
        var toCreate = Activator.CreateInstance(sagaDataType);
        foreach (var accessor in GetPropertyAccessors(sagaDataType))
        {
            if (!entity.ContainsKey(accessor.Name))
            {
                continue;
            }

            var value = entity[accessor.Name];
            var type = accessor.PropertyType;

            if (type == typeof(byte[]))
            {
                accessor.Setter(toCreate, value.BinaryValue);
            }
            else if (TrySetNullable(value, toCreate, accessor))
            {
            }
            else if (type == typeof(string))
            {
                accessor.Setter(toCreate, value.StringValue);
            }
            else
            {
                if (value.PropertyType == EdmType.String)
                {
                    // possibly serialized JSON.NET value
                    try
                    {
                        using (var stringReader = new StringReader(value.StringValue))
                        {
                            var deserialized = jsonSerializer.Deserialize(stringReader, type);
                            accessor.Setter(toCreate, deserialized);
                        }
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException($"The property type '{type.Name}' is not supported in Azure Table Storage and it cannot be deserialized with JSON.NET.");
                    }
                }
                else
                {
                    throw new NotSupportedException($"The property type '{type.Name}' is not supported in Azure Table Storage");
                }
            }
        }
        return toCreate;
    }

    public static DictionaryTableEntity ToDictionaryTableEntity(object sagaData, DictionaryTableEntity toPersist)
    {
        foreach (var accessor in GetPropertyAccessors(sagaData.GetType()))
        {
            var name = accessor.Name;
            var type = accessor.PropertyType;
            var value = accessor.Getter(sagaData);

            if (type == typeof(byte[]))
            {
                toPersist[name] = new EntityProperty((byte[])value);
            }
            else if (TryGetNullable(type, value, out bool? @bool))
            {
                toPersist[name] = new EntityProperty(@bool);
            }
            else if (TryGetNullable(type, value, out DateTime? dateTime))
            {
                if (!dateTime.HasValue || dateTime.Value < StorageTableMinDateTime)
                {
                    throw new Exception($"Saga data of type '{sagaData.GetType().FullName}' with DateTime property '{name}' has an invalid value '{dateTime}'. Value cannot be null and must be equal to or greater than '{StorageTableMinDateTime}'.");
                }

                toPersist[name] = new EntityProperty(dateTime);
            }
            else if (TryGetNullable(type, value, out Guid? guid))
            {
                toPersist[name] = new EntityProperty(guid);
            }
            else if (TryGetNullable(type, value, out int? @int))
            {
                toPersist[name] = new EntityProperty(@int);
            }
            else if (TryGetNullable(type, value, out long? @long))
            {
                toPersist[name] = new EntityProperty(@long);
            }
            else if (TryGetNullable(type, value, out double? @double))
            {
                toPersist[name] = new EntityProperty(@double);
            }
            else if (type == typeof(string))
            {
                toPersist[name] = new EntityProperty((string)value);
            }
            else
            {
                using (var sw = new StringWriter())
                {
                    try
                    {
                        jsonSerializerWithNonAbstractDefaultContractResolver.Serialize(sw, value, type);
                    }
                    catch (Exception)
                    {
                        throw new NotSupportedException($"The property type '{type.Name}' is not supported in Azure Table Storage and it cannot be serialized with JSON.NET.");
                    }
                    toPersist[name] = new EntityProperty(sw.ToString());
                }
            }
        }
        return toPersist;
    }

    private static IReadOnlyCollection<PropertyAccessor> GetPropertyAccessors(Type sagaDataType)
    {
        var accessors = propertyAccessorCache.GetOrAdd(sagaDataType, dataType =>
        {
            var setters = new List<PropertyAccessor>();
            var entityProperties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var propertyInfo in entityProperties)
            {
                setters.Add(new PropertyAccessor(propertyInfo));
            }
            return setters;
        });
        return accessors;
    }

    static bool TryGetNullable<TPrimitive>(Type type, object value, out TPrimitive? nullable)
        where TPrimitive : struct
    {
        if (type == typeof(TPrimitive))
        {
            nullable = (TPrimitive)value;
            return true;
        }

        if (type == typeof(TPrimitive?))
        {
            nullable = (TPrimitive?)value;
            return true;
        }

        nullable = null;
        return false;
    }

    static bool TrySetNullable(EntityProperty value, object toCreate, PropertyAccessor setter)
    {
        return
            TrySetNullable<bool>(value, toCreate, setter) ||
            TrySetNullable<DateTime>(value, toCreate, setter) ||
            TrySetNullable<Guid>(value, toCreate, setter) ||
            TrySetNullable<int>(value, toCreate, setter) ||
            TrySetNullable<double>(value, toCreate, setter) ||
            TrySetNullable<long>(value, toCreate, setter);
    }

    static bool TrySetNullable<TPrimitive>(EntityProperty property, object entity, PropertyAccessor setter)
        where TPrimitive : struct
    {
        if (setter.PropertyType == typeof(TPrimitive))
        {
            var value = (TPrimitive?)property.PropertyAsObject;
            var nonNullableValue = value ?? default;
            setter.Setter(entity, nonNullableValue);
            return true;
        }

        if (setter.PropertyType == typeof(TPrimitive?))
        {
            var value = (TPrimitive?)property.PropertyAsObject;
            setter.Setter(entity, value);
            return true;
        }

        return false;
    }

    public static TableQuery<DictionaryTableEntity> BuildWherePropertyQuery(Type type, string property, object value)
    {
        TableQuery<DictionaryTableEntity> query;

        var propertyInfo = type.GetProperty(property);
        if (propertyInfo == null)
        {
            return null;
        }

        if (propertyInfo.PropertyType == typeof(byte[]))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBinary(property, QueryComparisons.Equal, (byte[])value));
        }
        else if (propertyInfo.PropertyType == typeof(bool))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForBool(property, QueryComparisons.Equal, (bool)value));
        }
        else if (propertyInfo.PropertyType == typeof(DateTime))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDate(property, QueryComparisons.Equal, (DateTime)value));
        }
        else if (propertyInfo.PropertyType == typeof(Guid))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForGuid(property, QueryComparisons.Equal, (Guid)value));
        }
        else if (propertyInfo.PropertyType == typeof(int))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForInt(property, QueryComparisons.Equal, (int)value));
        }
        else if (propertyInfo.PropertyType == typeof(long))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForLong(property, QueryComparisons.Equal, (long)value));
        }
        else if (propertyInfo.PropertyType == typeof(double))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterConditionForDouble(property, QueryComparisons.Equal, (double)value));
        }
        else if (propertyInfo.PropertyType == typeof(string))
        {
            query = new TableQuery<DictionaryTableEntity>().Where(TableQuery.GenerateFilterCondition(property, QueryComparisons.Equal, (string)value));
        }
        else
        {
            throw new NotSupportedException($"The property type '{propertyInfo.PropertyType.Name}' is not supported in Azure Table Storage");
        }

        return query;
    }

    static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyAccessor>> propertyAccessorCache = new ConcurrentDictionary<Type, IReadOnlyCollection<PropertyAccessor>>();

    static readonly JsonSerializer jsonSerializer = JsonSerializer.Create();
    static readonly JsonSerializer jsonSerializerWithNonAbstractDefaultContractResolver = new JsonSerializer
    {
        ContractResolver = new NonAbstractDefaultContractResolver(),
    };

    private static readonly DateTime StorageTableMinDateTime = new DateTime(1601, 1, 1);

    sealed class PropertyAccessor
    {
        public PropertyAccessor(PropertyInfo propertyInfo)
        {
            Setter = GenerateSetter(propertyInfo);
            Getter = GenerateGetter(propertyInfo);
            Name = propertyInfo.Name;
            PropertyType = propertyInfo.PropertyType;
        }

        private static Func<object, object> GenerateGetter(PropertyInfo propertyInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var instanceCast = !propertyInfo.DeclaringType.IsValueType
                ? Expression.TypeAs(instance, propertyInfo.DeclaringType)
                : Expression.Convert(instance, propertyInfo.DeclaringType);
            var getter = Expression
                .Lambda<Func<object, object>>(
                    Expression.TypeAs(Expression.Call(instanceCast, propertyInfo.GetGetMethod()), typeof(object)), instance)
                .Compile();
            return getter;
        }

        private static Action<object, object> GenerateSetter(PropertyInfo propertyInfo)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");
            // value as T is slightly faster than (T)value, so if it's not a value type, use that
            var instanceCast = !propertyInfo.DeclaringType.IsValueType
                ? Expression.TypeAs(instance, propertyInfo.DeclaringType)
                : Expression.Convert(instance, propertyInfo.DeclaringType);
            var valueCast = !propertyInfo.PropertyType.IsValueType
                ? Expression.TypeAs(value, propertyInfo.PropertyType)
                : Expression.Convert(value, propertyInfo.PropertyType);
            var setter = Expression
                .Lambda<Action<object, object>>(Expression.Call(instanceCast, propertyInfo.GetSetMethod(), valueCast), instance,
                    value).Compile();
            return setter;
        }

        public Action<object, object> Setter { get; }
        public Func<object, object> Getter { get; }
        public string Name { get; }
        public Type PropertyType { get; }
    }

    class NonAbstractDefaultContractResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            if (objectType.IsAbstract || objectType.IsInterface)
            {
                throw new ArgumentException("Cannot serialize an abstract class/interface", nameof(objectType));
            }
            return base.CreateObjectContract(objectType);
        }
    }
}