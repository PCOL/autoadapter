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

namespace AutoAdapter.Extensions
{
    using System;

    /// <summary>
    /// An attribute to allow an adapter extension to be applied to a parameter, property, or return value.
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Property)]
    public class AdapterExtensionAttribute
        : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterExtensionAttribute"/> class.
        /// </summary>
        /// <param name="extensionName">The name of the extension to apply.</param>
        public AdapterExtensionAttribute(string extensionName)
        {
            this.ExtensionName = extensionName;
        }

        /// <summary>
        /// Gets the name of the extension.
        /// </summary>
        public string ExtensionName { get; }

        /// <summary>
        /// Gets or sets the extension methods placement.
        /// </summary>
        public AdapterExtensionPlacement? Placement { get; set; }
    }
}
