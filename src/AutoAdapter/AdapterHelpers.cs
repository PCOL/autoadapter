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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoAdapter.Extensions;

    /// <summary>
    /// <see cref="AdapterTypeGenerator"/> helpers and extensions.
    /// </summary>
    public static class AdapterHelpers
    {
        /// <summary>
        /// Creates an adapter.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="inst">The instance being adapted</param>
        /// <returns>An instance of the adapter type or null if the instance to be adapted is null.</returns>
        public static T CreateAdapter<T>(object inst)
        {
            if (inst == null)
            {
                return default(T);
            }

            // IServiceProvider resolver = Services.DependencyResolver;
            // using (IDependencyScope scope = resolver.BeginScope())
            // {
            //     return (T)scope.GetService<IAdapterTypeGenerator>().CreateAdapter<T>(inst, scope);
            // }

            return default(T);
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="typeToAdapt">The type to be adapted.</param>
        /// <returns>A <see cref="Type"/> representing the adapter.</returns>
        public static Type CreateAdapterType<T>(Type typeToAdapt)
        {
            // IDependencyResolver resolver = Services.DependencyResolver;
            // using (IDependencyScope scope = resolver.BeginScope())
            // {
            //     return scope.GetService<IAdapterTypeGenerator>().CreateAdapterType<T>(typeToAdapt, scope);
            // }

            return null;
        }
    }
}
