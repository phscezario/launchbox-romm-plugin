using System;
using Microsoft.Extensions.DependencyInjection;

namespace RommPlugin.Core
{
    /// <summary>
    /// Static service locator that wraps Microsoft.Extensions.DependencyInjection.
    /// Provides a global access point for resolving registered services throughout the plugin.
    /// Must be initialized via <see cref="Initialize"/> before any service resolution.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider _current;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the current service provider instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="Initialize"/> has not been called.</exception>
        public static IServiceProvider Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException("ServiceLocator not initialized. Call Initialize() first.");
                return _current;
            }
        }

        /// <summary>
        /// Initializes the service locator with the specified service registrations.
        /// This method is thread-safe and can only be called once per application lifecycle.
        /// </summary>
        /// <param name="configure">An action to configure the service collection with registrations.</param>
        public static void Initialize(Action<IServiceCollection> configure)
        {
            lock (_lock)
            {
                var services = new ServiceCollection();
                configure(services);
                _current = services.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Resolves a required service of type <typeparamref name="T"/> from the container.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
        public static T GetService<T>() where T : class
        {
            return Current.GetRequiredService<T>();
        }

        /// <summary>
        /// Resolves an optional service of type <typeparamref name="T"/> from the container.
        /// Returns null if the service is not registered.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>The resolved service instance, or null if not registered.</returns>
        public static T GetServiceOrDefault<T>() where T : class
        {
            return Current.GetService<T>();
        }
    }
}
