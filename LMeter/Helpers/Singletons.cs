using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using LMeter.ACT;

namespace LMeter.Helpers
{
    public interface IPluginDisposable : IDisposable { }

    public static class Singletons
    {
        private static readonly Dictionary<Type, Func<object>> TypeInitializers = new Dictionary<Type, Func<object>>()
        {
        };

        private static readonly ConcurrentDictionary<Type, object> ActiveInstances = new ConcurrentDictionary<Type, object>();

        public static T Get<T>()
        {
            return (T)ActiveInstances.GetOrAdd(typeof(T), (objectType) =>
            {
                object newInstance;
                if (Singletons.TypeInitializers.TryGetValue(objectType, out Func<object>? initializer))
                {
                    newInstance = initializer();
                }
                else
                {
                    throw new Exception($"No initializer found for Type '{objectType.FullName}'.");
                }

                if (newInstance is null || newInstance is not T)
                {
                    throw new Exception($"Received invalid result from initializer for type '{objectType.FullName}'");
                }

                return newInstance;
            });
        }

        public static void Register(object newSingleton)
        {
            if (!ActiveInstances.TryAdd(newSingleton.GetType(), newSingleton))
            {
                throw new Exception($"Failed to register new singleton for type {newSingleton.GetType()}");
            }
        }

        public static void DeleteActClients()
        {
            ActiveInstances.TryRemove(typeof(ACTClient), out var client1);
            if (client1 != null)
            {
                ((IACTClient) client1).Dispose();
            }

            ActiveInstances.TryRemove(typeof(IINACTClient), out var client2);
            if (client2 != null)
            {
                ((IACTClient) client2).Dispose();
            }
        }
        
        public static void Dispose()
        {
            foreach (object singleton in ActiveInstances.Values)
            {
                // Only dispose the disposable objects that we own
                if (singleton is IPluginDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            ActiveInstances.Clear();
        }
    }
}