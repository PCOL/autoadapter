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
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using AutoAdapter.Reflection;

    /// <summary>
    /// Action adapter generator.
    /// </summary>
    internal class ActionAdapterGenerator
        : DelegateAdapterGenerator
    {
        public ActionAdapterGenerator(AdapterContext adapterContext)
            : base(adapterContext, "ActionAdapter")
        {
        }

        /// <summary>
        /// Generates the action adapter type.
        /// </summary>
        /// <param name="sourceTypes">The source actions types</param>
        /// <param name="adaptedTypes"></param>
        /// <returns></returns>
        public override Type GenerateType(
            Type[] sourceTypes,
            Type[] adaptedTypes)
        {
            Type actionType = Type
                .GetType($"System.Action`{sourceTypes.Length}")
                .MakeGenericType(sourceTypes);

            Type adaptedType = Type
                .GetType($"System.Action`{adaptedTypes.Length}")
                .MakeGenericType(adaptedTypes);

            return this.GenerateType(
                actionType,
                sourceTypes,
                typeof(void),
                adaptedType,
                adaptedTypes,
                typeof(void));
        }
    }
}
