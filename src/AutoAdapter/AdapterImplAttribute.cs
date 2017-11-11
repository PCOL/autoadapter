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
    using System.Reflection;

    /// <summary>
    /// An attribute used to influence the adapter implementation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public class AdapterImplAttribute
        : Attribute
    {
        /// <summary>
        /// Gets or sets the adapter member binding.
        /// </summary>
        public AdapterBinding TargetBinding { get; set; } = AdapterBinding.Instance;

        /// <summary>
        /// Gets or sets the type that the method, property, or event should target a static member on.
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Gets or sets the name of the type to target.
        /// </summary>
        public string TargetTypeName { get; set; }

        /// <summary>
        /// Gets or sets the members name in the type being adapted.
        /// </summary>
        public string TargetMemberName { get; set; }

        /// <summary>
        /// Gets or sets the target members type.
        /// </summary>
        public TargetMemberType TargetMemberType { get; set; }
    }
}
