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

    internal class FuncAdapterGenerator
        : DelegateAdapterGenerator
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="FuncAdapterGenerator"/> class.
        /// </summary>
        /// <param name="adapterContext">A <see cref="AdapterContext"/> instance.</param>
        public FuncAdapterGenerator(AdapterContext adapterContext)
            : base(adapterContext, "FuncAdapter")
        {
        }

        /// <summary>
        /// Generates the action adapter type.
        /// </summary>
        /// <param name="sourceTypes">The source actions types</param>
        /// <param name="adaptedTypes">The adapted actions types.</param>
        /// <returns>The generated type.</returns>
        public override Type GenerateType(
            Type[] sourceTypes,
            Type[] adaptedTypes)
        {
            Type actionType = Type
                .GetType($"System.Func`{sourceTypes.Length}")
                .MakeGenericType(sourceTypes);

            Type adaptedType = Type
                .GetType($"System.Func`{adaptedTypes.Length}")
                .MakeGenericType(adaptedTypes);

            Type sourceReturnType = this.CopyToArgumentsAndReturnType(sourceTypes, out Type[] sourceTypeArgs);

            Type adaptedReturnType = this.CopyToArgumentsAndReturnType(adaptedTypes, out Type[] adaptedTypeArgs);

            return this.GenerateType(
                actionType,
                sourceTypeArgs,
                sourceReturnType,
                adaptedType,
                adaptedTypeArgs,
                adaptedReturnType);
        }
    }
}