/*
MIT License

Copyright (c) 2017 P Collyer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace AutoAdapter
{
    using System;
    using System.Linq;
    using System.Reflection;
    using AutoAdapter.Extensions;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Adapter extension methods.
    /// </summary>
    public static class AdapterExtensionMethods
    {
        /// <summary>
        /// Checks if an object is adapted.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if it is adapted; otherwise false.</returns>
        public static bool IsAdaptedObject(this object obj)
        {
            return obj != null &&
                typeof(IAdaptedObject).IsAssignableFrom(obj.GetType());
        }

        /// <summary>
        /// Creates an adapter.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="instance">The instance being adapted</param>
        /// <returns>An instance of the adapter type or null if the instance to be adapted is null.</returns>
        public static T CreateAdapter<T>(this object instance)
        {
            if (instance == null)
            {
                return default(T);
            }

            return instance.CreateAdapter<T>(Services.ServiceProvider);
        }

        /// <summary>
        /// Creates an adapter.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="instance">The instance being adapted</param>
        /// <param name="serviceProvider">Optional service provider instance.</param>
        /// <returns>An instance of the adapter type or null if the instance to be adapted is null.</returns>
        public static T CreateAdapter<T>(this object instance, IServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                return default(T);
            }

            var adapterTypeGenerator = serviceProvider?.GetService<IAdapterTypeGenerator>();
            if (adapterTypeGenerator == null)
            {
                adapterTypeGenerator = new AdapterTypeGenerator();
            }

            return adapterTypeGenerator.CreateAdapter<T>(instance, serviceProvider);
        }

        /// <summary>
        /// Creates a adapter type.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="type">The type being adapted</param>
        /// <returns>An instance of the adapter type.</returns>
        public static Type CreateAdapterType<T>(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var adapterTypeGenerator = Services.ServiceProvider?.GetService<IAdapterTypeGenerator>();
            if (adapterTypeGenerator == null)
            {
                adapterTypeGenerator = new AdapterTypeGenerator();
            }

            return adapterTypeGenerator.CreateAdapterType<T>(type, Services.ServiceProvider);
        }

        /// <summary>
        /// Gets an adapter extension.
        /// </summary>
        /// <param name="extensionName">The name of the extension.</param>
        /// <returns>An <see cref="IAdapterExtension{T, TResult}"/> if found; otherwise null.</returns>
        internal static IAdapterExtension<T, TResult> GetAdapterExtension<T, TResult>(this IServiceProvider serviceProvider, string extensionName)
        {
            var adapters = serviceProvider.GetServices<IAdapterExtension<T, TResult>>();
            if (adapters != null)
            {
                return adapters.FirstOrDefault(a => a.Name == extensionName);
            }

            return default(IAdapterExtension<T, TResult>);
        }
    }
}
