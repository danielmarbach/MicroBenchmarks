using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MicroBenchmarks.NServiceBus
{
    public class MessageHandlerRegistryBeforeOptimizations
    {
        internal MessageHandlerRegistryBeforeOptimizations(Conventions conventions)
        {
            this.conventions = conventions;
        }

        /// <summary>
        /// Gets the list of handlers <see cref="Type" />s for the given
        /// <paramref name="messageType" />.
        /// </summary>
        public IEnumerable<MessageHandler> GetHandlersFor(Type messageType)
        {
            if (!conventions.IsMessageType(messageType))
            {
                return Enumerable.Empty<MessageHandler>();
            }

            return from handlersAndMessages in handlerAndMessagesHandledByHandlerCache
                   from messagesBeingHandled in handlersAndMessages.Value
                   where Type.GetTypeFromHandle(messagesBeingHandled.MessageType).IsAssignableFrom(messageType)
                   select new MessageHandler(messagesBeingHandled.MethodDelegate, Type.GetTypeFromHandle(handlersAndMessages.Key));
        }

        /// <summary>
        /// Lists all message type for which we have handlers.
        /// </summary>
        public IEnumerable<Type> GetMessageTypes()
        {
            return (from messagesBeingHandled in handlerAndMessagesHandledByHandlerCache.Values
                    from typeHandled in messagesBeingHandled
                    let messageType = Type.GetTypeFromHandle(typeHandled.MessageType)
                    where conventions.IsMessageType(messageType)
                    select messageType).Distinct();
        }

        /// <summary>
        /// Registers the given potential handler type.
        /// </summary>
        public void RegisterHandler(Type handlerType)
        {
            if (handlerType.IsAbstract)
            {
                return;
            }

            ValidateHandlerType(handlerType);

            var messageTypes = GetMessageTypesBeingHandledBy(handlerType);

            foreach (var messageType in messageTypes)
            {
                List<DelegateHolder> typeList;
                var typeHandle = handlerType.TypeHandle;
                if (!handlerAndMessagesHandledByHandlerCache.TryGetValue(typeHandle, out typeList))
                {
                    handlerAndMessagesHandledByHandlerCache[typeHandle] = typeList = new List<DelegateHolder>();
                }

                CacheHandlerMethods(handlerType, messageType, typeList);
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            handlerAndMessagesHandledByHandlerCache.Clear();
        }

        static void CacheHandlerMethods(Type handler, Type messageType, ICollection<DelegateHolder> typeList)
        {
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), typeList);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), typeList);
        }

        static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, ICollection<DelegateHolder> methodList)
        {
            var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
            if (handleMethod == null)
            {
                return;
            }

            var delegateHolder = new DelegateHolder
            {
                MessageType = messageType.TypeHandle,
                MethodDelegate = handleMethod
            };
            methodList.Add(delegateHolder);
        }

        static Func<object, object, IMessageHandlerContext, Task> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
        {
            var interfaceType = interfaceGenericType.MakeGenericType(messageType);

            if (!interfaceType.IsAssignableFrom(targetType))
            {
                return null;
            }

            var methodInfo = targetType.GetInterfaceMap(interfaceType).TargetMethods.FirstOrDefault();
            if (methodInfo == null)
            {
                return null;
            }

            var target = Expression.Parameter(typeof(object));
            var messageParam = Expression.Parameter(typeof(object));
            var contextParam = Expression.Parameter(typeof(IMessageHandlerContext));

            var castTarget = Expression.Convert(target, targetType);

            var methodParameters = methodInfo.GetParameters();
            var messageCastParam = Expression.Convert(messageParam, methodParameters.ElementAt(0).ParameterType);

            Expression body = Expression.Call(castTarget, methodInfo, messageCastParam, contextParam);

            return Expression.Lambda<Func<object, object, IMessageHandlerContext, Task>>(body, target, messageParam, contextParam).Compile();
        }

        static IEnumerable<Type> GetMessageTypesBeingHandledBy(Type type)
        {
            return (from t in type.GetInterfaces()
                    where t.IsGenericType
                    let potentialMessageType = t.GetGenericArguments()[0]
                    where
                        typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                    select potentialMessageType)
                .Distinct()
                .ToList();
        }
        void ValidateHandlerType(Type handlerType)
        {
            var propertyTypes = handlerType.GetProperties().Select(p => p.PropertyType).ToList();
            var ctorArguments = handlerType.GetConstructors()
             .SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType))
             .ToList();

            var dependencies = propertyTypes.Concat(ctorArguments).ToList();

            if (dependencies.Any(t => typeof(IMessageSession).IsAssignableFrom(t)))
            {
                throw new Exception($"Interfaces IMessageSession or IEndpointInstance should not be resolved from the container to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the {handlerType.Name}.Handle method to send or publish messages.");
            }
        }

        Conventions conventions;
        IDictionary<RuntimeTypeHandle, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();

        class DelegateHolder
        {
            public RuntimeTypeHandle MessageType;
            public Func<object, object, IMessageHandlerContext, Task> MethodDelegate;
        }

        public class MessageHandler
        {
            public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
            {
                HandlerType = handlerType;
                this.invocation = invocation;
            }

            public object Instance { get; set; }

            public Type HandlerType { get; private set; }

            public Task Invoke(object message, IMessageHandlerContext handlerContext)
            {
                return invocation(Instance, message, handlerContext);
            }

            Func<object, object, IMessageHandlerContext, Task> invocation;
        }

        public interface IMessageHandlerContext
        {
        }

        public interface IMessageSession
        {
        }

        public interface IHandleMessages<in TMessage>
        {
            Task Handle(TMessage message, IMessageHandlerContext context);
        }


        public interface IHandleTimeouts<TMessage>
        {
            Task Handle(TMessage message, IMessageHandlerContext context);
        }

        public class Conventions
        {
            public bool IsMessageType(Type t)
            {
                try
                {
                    return MessagesConventionCache.ApplyConvention(t,
                        typeHandle =>
                        {
                            var type = Type.GetTypeFromHandle(typeHandle);

                            if (IsInSystemConventionList(type))
                            {
                                return true;
                            }
                            if (type.IsFromParticularAssembly())
                            {
                                return false;
                            }
                            return IsMessageTypeAction(type) ||
                                   IsCommandTypeAction(type) ||
                                   IsEventTypeAction(type);
                        });
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to evaluate Message convention. See inner exception for details.", ex);
                }
            }

            /// <summary>
            /// Returns true is message is a system message type.
            /// </summary>
            public bool IsInSystemConventionList(Type t)
            {
                return IsSystemMessageActions.Any(isSystemMessageAction => isSystemMessageAction(t));
            }

            /// <summary>
            /// Add system message convention.
            /// </summary>
            /// <param name="definesMessageType">Function to define system message convention.</param>
            public void AddSystemMessagesConventions(Func<Type, bool> definesMessageType)
            {
                if (!IsSystemMessageActions.Contains(definesMessageType))
                {
                    IsSystemMessageActions.Add(definesMessageType);
                    MessagesConventionCache.Reset();
                }
            }

            /// <summary>
            /// Returns true if the given type is a command type.
            /// </summary>
            public bool IsCommandType(Type t)
            {
                try
                {
                    return CommandsConventionCache.ApplyConvention(t, typeHandle =>
                    {
                        var type = Type.GetTypeFromHandle(typeHandle);
                        if (type.IsFromParticularAssembly())
                        {
                            return false;
                        }
                        return IsCommandTypeAction(type);
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to evaluate Command convention. See inner exception for details.", ex);
                }
            }

            /// <summary>
            /// Returns true if the given type is a event type.
            /// </summary>
            public bool IsEventType(Type t)
            {
                try
                {
                    return EventsConventionCache.ApplyConvention(t, typeHandle =>
                    {
                        var type = Type.GetTypeFromHandle(typeHandle);
                        if (type.IsFromParticularAssembly())
                        {
                            return false;
                        }
                        return IsEventTypeAction(type);
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to evaluate Event convention. See inner exception for details.", ex);
                }
            }

            ConventionCache CommandsConventionCache = new ConventionCache();
            ConventionCache EventsConventionCache = new ConventionCache();

            internal Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;

            internal Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;


            internal Func<Type, bool> IsMessageTypeAction = t => typeof(IMessage).IsAssignableFrom(t) &&
                                                                 typeof(IMessage) != t &&
                                                                 typeof(IEvent) != t &&
                                                                 typeof(ICommand) != t;

            List<Func<Type, bool>> IsSystemMessageActions = new List<Func<Type, bool>>();
            ConventionCache MessagesConventionCache = new ConventionCache();


            class ConventionCache
            {
                public bool ApplyConvention(Type type, Func<RuntimeTypeHandle, bool> action)
                {
                    return cache.GetOrAdd(type.TypeHandle, action);
                }

                public void Reset()
                {
                    cache.Clear();
                }

                ConcurrentDictionary<RuntimeTypeHandle, bool> cache = new ConcurrentDictionary<RuntimeTypeHandle, bool>();
            }
        }

        public class Handler1 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler2 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler3 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler4 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler5 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class MyMessage
        {
        }
    }

    public class MessageHandlerRegistryAfterOptimizations
    {
        internal MessageHandlerRegistryAfterOptimizations(Conventions conventions)
        {
            this.conventions = conventions;
        }

        /// <summary>
        /// Gets the list of handlers <see cref="Type" />s for the given
        /// <paramref name="messageType" />.
        /// </summary>
        public List<MessageHandler> GetHandlersFor(Type messageType)
        {
            if (!conventions.IsMessageType(messageType))
            {
                return noMessageHandlers;
            }

            var messageHandlers = new List<MessageHandler>();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var handlersAndMessages in handlerAndMessagesHandledByHandlerCache)
            {
                var handlerType = handlersAndMessages.Key;
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var messagesBeingHandled in handlersAndMessages.Value)
                {
                    if (messagesBeingHandled.MessageType.IsAssignableFrom(messageType))
                    {
                        messageHandlers.Add(new MessageHandler(messagesBeingHandled.MethodDelegate, handlerType));
                    }
                }
            }
            return messageHandlers;
        }

        /// <summary>
        /// Lists all message type for which we have handlers.
        /// </summary>
        public IEnumerable<Type> GetMessageTypes()
        {
            return (from messagesBeingHandled in handlerAndMessagesHandledByHandlerCache.Values
                    from typeHandled in messagesBeingHandled
                    let messageType = typeHandled.MessageType
                    where conventions.IsMessageType(messageType)
                    select messageType).Distinct();
        }

        /// <summary>
        /// Registers the given potential handler type.
        /// </summary>
        public void RegisterHandler(Type handlerType)
        {
            if (handlerType.IsAbstract)
            {
                return;
            }

            ValidateHandlerType(handlerType);

            var messageTypes = GetMessageTypesBeingHandledBy(handlerType);

            foreach (var messageType in messageTypes)
            {
                List<DelegateHolder> typeList;
                if (!handlerAndMessagesHandledByHandlerCache.TryGetValue(handlerType, out typeList))
                {
                    handlerAndMessagesHandledByHandlerCache[handlerType] = typeList = new List<DelegateHolder>();
                }

                CacheHandlerMethods(handlerType, messageType, typeList);
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            handlerAndMessagesHandledByHandlerCache.Clear();
        }

        static void CacheHandlerMethods(Type handler, Type messageType, ICollection<DelegateHolder> typeList)
        {
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), typeList);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), typeList);
        }

        static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, ICollection<DelegateHolder> methodList)
        {
            var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
            if (handleMethod == null)
            {
                return;
            }

            var delegateHolder = new DelegateHolder
            {
                MessageType = messageType,
                MethodDelegate = handleMethod
            };
            methodList.Add(delegateHolder);
        }

        static Func<object, object, IMessageHandlerContext, Task> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
        {
            var interfaceType = interfaceGenericType.MakeGenericType(messageType);

            if (!interfaceType.IsAssignableFrom(targetType))
            {
                return null;
            }

            var methodInfo = targetType.GetInterfaceMap(interfaceType).TargetMethods.FirstOrDefault();
            if (methodInfo == null)
            {
                return null;
            }

            var target = Expression.Parameter(typeof(object));
            var messageParam = Expression.Parameter(typeof(object));
            var contextParam = Expression.Parameter(typeof(IMessageHandlerContext));

            var castTarget = Expression.Convert(target, targetType);

            var methodParameters = methodInfo.GetParameters();
            var messageCastParam = Expression.Convert(messageParam, methodParameters.ElementAt(0).ParameterType);

            Expression body = Expression.Call(castTarget, methodInfo, messageCastParam, contextParam);

            return Expression.Lambda<Func<object, object, IMessageHandlerContext, Task>>(body, target, messageParam, contextParam).Compile();
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        static Type[] GetMessageTypesBeingHandledBy(Type type)
        {
            return (from t in type.GetInterfaces()
                    where t.IsGenericType
                    let potentialMessageType = t.GetGenericArguments()[0]
                    where
                        typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                    select potentialMessageType)
                .Distinct()
                .ToArray();
        }
        void ValidateHandlerType(Type handlerType)
        {
            var propertyTypes = handlerType.GetProperties().Select(p => p.PropertyType).ToList();
            var ctorArguments = handlerType.GetConstructors()
             .SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType))
             .ToList();

            var dependencies = propertyTypes.Concat(ctorArguments).ToList();

            if (dependencies.Any(t => typeof(IMessageSession).IsAssignableFrom(t)))
            {
                throw new Exception($"Interfaces IMessageSession or IEndpointInstance should not be resolved from the container to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the {handlerType.Name}.Handle method to send or publish messages.");
            }
        }

        readonly Conventions conventions;
        readonly Dictionary<Type, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = new Dictionary<Type, List<DelegateHolder>>();
        static List<MessageHandler> noMessageHandlers = new List<MessageHandler>();

        struct DelegateHolder
        {
            public Type MessageType;
            public Func<object, object, IMessageHandlerContext, Task> MethodDelegate;
        }

        public class MessageHandler
        {
            public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
            {
                HandlerType = handlerType;
                this.invocation = invocation;
            }

            public object Instance { get; set; }

            public Type HandlerType { get; private set; }

            public Task Invoke(object message, IMessageHandlerContext handlerContext)
            {
                return invocation(Instance, message, handlerContext);
            }

            Func<object, object, IMessageHandlerContext, Task> invocation;
        }

        public interface IMessageHandlerContext
        {
        }

        public interface IMessageSession
        {
        }

        public interface IHandleMessages<in TMessage>
        {
            Task Handle(TMessage message, IMessageHandlerContext context);
        }


        public interface IHandleTimeouts<TMessage>
        {
            Task Handle(TMessage message, IMessageHandlerContext context);
        }

        public class Conventions
        {
            public bool IsMessageType(Type t)
            {
                try
                {
                    return MessagesConventionCache.ApplyConvention(t,
                        typeHandle =>
                        {
                            var type = Type.GetTypeFromHandle(typeHandle);

                            if (IsInSystemConventionList(type))
                            {
                                return true;
                            }
                            if (type.IsFromParticularAssembly())
                            {
                                return false;
                            }
                            return IsMessageTypeAction(type) ||
                                   IsCommandTypeAction(type) ||
                                   IsEventTypeAction(type);
                        });
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to evaluate Message convention. See inner exception for details.", ex);
                }
            }

            /// <summary>
            /// Returns true is message is a system message type.
            /// </summary>
            public bool IsInSystemConventionList(Type t)
            {
                return IsSystemMessageActions.Any(isSystemMessageAction => isSystemMessageAction(t));
            }

            /// <summary>
            /// Add system message convention.
            /// </summary>
            /// <param name="definesMessageType">Function to define system message convention.</param>
            public void AddSystemMessagesConventions(Func<Type, bool> definesMessageType)
            {
                if (!IsSystemMessageActions.Contains(definesMessageType))
                {
                    IsSystemMessageActions.Add(definesMessageType);
                    MessagesConventionCache.Reset();
                }
            }

            /// <summary>
            /// Returns true if the given type is a command type.
            /// </summary>
            public bool IsCommandType(Type t)
            {
                try
                {
                    return CommandsConventionCache.ApplyConvention(t, typeHandle =>
                    {
                        var type = Type.GetTypeFromHandle(typeHandle);
                        if (type.IsFromParticularAssembly())
                        {
                            return false;
                        }
                        return IsCommandTypeAction(type);
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to evaluate Command convention. See inner exception for details.", ex);
                }
            }

            /// <summary>
            /// Returns true if the given type is a event type.
            /// </summary>
            public bool IsEventType(Type t)
            {
                try
                {
                    return EventsConventionCache.ApplyConvention(t, typeHandle =>
                    {
                        var type = Type.GetTypeFromHandle(typeHandle);
                        if (type.IsFromParticularAssembly())
                        {
                            return false;
                        }
                        return IsEventTypeAction(type);
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to evaluate Event convention. See inner exception for details.", ex);
                }
            }

            ConventionCache CommandsConventionCache = new ConventionCache();
            ConventionCache EventsConventionCache = new ConventionCache();

            internal Func<Type, bool> IsCommandTypeAction = t => typeof(ICommand).IsAssignableFrom(t) && typeof(ICommand) != t;

            internal Func<Type, bool> IsEventTypeAction = t => typeof(IEvent).IsAssignableFrom(t) && typeof(IEvent) != t;


            internal Func<Type, bool> IsMessageTypeAction = t => typeof(IMessage).IsAssignableFrom(t) &&
                                                                 typeof(IMessage) != t &&
                                                                 typeof(IEvent) != t &&
                                                                 typeof(ICommand) != t;

            List<Func<Type, bool>> IsSystemMessageActions = new List<Func<Type, bool>>();
            ConventionCache MessagesConventionCache = new ConventionCache();


            class ConventionCache
            {
                public bool ApplyConvention(Type type, Func<RuntimeTypeHandle, bool> action)
                {
                    return cache.GetOrAdd(type.TypeHandle, action);
                }

                public void Reset()
                {
                    cache.Clear();
                }

                ConcurrentDictionary<RuntimeTypeHandle, bool> cache = new ConcurrentDictionary<RuntimeTypeHandle, bool>();
            }
        }

        public class Handler1 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler2 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler3 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler4 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class Handler5 : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return null;
            }
        }

        public class MyMessage
        {
        }
    }

    static class TypeExtensionMethods
    {
        public static bool IsFromParticularAssembly(this Type type)
        {
            return type.Assembly.GetName()
                .GetPublicKeyToken()
                .SequenceEqual(nsbPublicKeyToken);
        }

        static byte[] nsbPublicKeyToken = typeof(TypeExtensionMethods).Assembly.GetName().GetPublicKeyToken();
    }

    interface ICommand { };

    interface IEvent { };

    interface IMessage { };
}