/*
MIT License

Copyright (c) 2017 PCOL

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
    using AutoAdapter.Reflection;

    /// <summary>
    /// An attribute used to specify the adapted type on interfaces and enumerations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Enum)]
    public class AdapterAttribute
        : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterAttribute"/> class.
        /// </summary>
        /// <param name="adaptedType">The adapted type.</param>
        public AdapterAttribute(Type adaptedType)
        {
            this.AdaptedType = adaptedType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterAttribute"/> class.
        /// </summary>
        /// <param name="adaptedTypeName">The name of the type being adapted.</param>
        public AdapterAttribute(string adaptedTypeName)
        {
            this.AdaptedTypeName = adaptedTypeName;
        }

        /// <summary>
        /// Gets the adapted type.
        /// </summary>
        public Type AdaptedType { get; }

        /// <summary>
        /// Gets the adapted type name.
        /// </summary>
        public string AdaptedTypeName { get; }

        /// <summary>
        /// Gets the adapted type from a <see cref="AdapterAttribute"/> instance.
        /// </summary>
        /// <param name="attr">The <see cref="AdpaterAttribute"/> instance.</param>
        /// <returns>The adapted type if found; otherwise null.</returns>
        public Type GetAdaptedType()
        {
            Type adaptedType = this.AdaptedType;
            if (adaptedType == null &&
                this.AdaptedTypeName.IsNullOrEmpty() == false)
            {
                adaptedType = TypeFactory.Default.GetType(this.AdaptedTypeName, false);
            }

            return adaptedType;
        }
    }
}
