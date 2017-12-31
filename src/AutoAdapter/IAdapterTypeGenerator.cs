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

    /// <summary>
    /// Defines the adapter type generator.
    /// </summary>
    public interface IAdapterTypeGenerator
    {
        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="typeToAdapt">The type to adapt.</param>
        /// <param name="serviceProvider">Optional service provider</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        Type CreateAdapterType<T>(Type typeToAdapt, IServiceProvider serviceProvider = null);

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <param name="typeToAdapt">The type to adapt.</param>
        /// <param name="adapterType">The adapter type.</param>
        /// <param name="serviceProvider">Optional service provider</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        Type CreateAdapterType(Type typeToAdapt, Type adapterType, IServiceProvider serviceProvider = null);

        /// <summary>
        /// Create instance of an adapter.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="instance">The instance of the object to adapt.</param>
        /// <param name="serviceProvider">Optional service provider</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        T CreateAdapter<T>(object instance, IServiceProvider serviceProvider = null);

        /// <summary>
        /// Create instance of an adapter.
        /// </summary>
        /// <param name="instance">The instance of the object to adapt.</param>
        /// <param name="serviceProvider">Optional service provider</param>
        /// <param name="adapterType">The interface describing the adapter type.</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        object CreateAdapter(object instance, Type adapterType, IServiceProvider serviceProvider = null);
    }
}
