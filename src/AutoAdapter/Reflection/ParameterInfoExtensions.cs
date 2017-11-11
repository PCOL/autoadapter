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

namespace AutoAdapter.Reflection
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Extension methods for the <see cref="ParameterInfo"/> type.
    /// </summary>
    public static class ParameterInfoExtensions
    {
        /// <summary>
        /// Checks if a parameter has an attribute applied to it.
        /// </summary>
        /// <param name="parameterInfo">The parameter.</param>
        /// <param name="attributeType">The attribute type.</param>
        /// <returns>True if it has; otherwise false.</returns>
        public static bool HasAttribute(this ParameterInfo parameterInfo, Type attributeType)
        {
            return parameterInfo.GetCustomAttributes(attributeType).FirstOrDefault() != null;
        }
    }
}
