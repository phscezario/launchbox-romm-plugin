using System;
using Microsoft.Extensions.DependencyInjection;

namespace RommPlugin.Core
{
    public static class ServiceLocator
    {
        private static IServiceProvider _current;
        private static readonly object _lock = new object();

        public static IServiceProvider Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
                return _current;
            }
        }

        public static void Initialize(Action<IServiceCollection> configure)
        {
            lock (_lock)
            {
                var services = new ServiceCollection();
                configure(services);
                _current = services.BuildServiceProvider();
            }
        }

        public static T GetService<T>() where T : class
        {
            return Current.GetRequiredService<T>();
        }

        public static T GetServiceOrDefault<T>() where T : class
        {
            return Current.GetService<T>();
        }
    }
}
