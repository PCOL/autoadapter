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
    using System.Reflection;
    using System.Reflection.Emit;
    using AutoAdapter.Reflection;

    /// <summary>
    /// Represents a method builder context
    /// </summary>
    internal class MethodBuilderContext
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="MethodBuilderContext"/> class.
        /// </summary>
        /// <param name="adapterContext">A <see cref="AdapterContext"/> instance></param>
        /// <param name="methodInfo">A <see cref="MethodInfo"/> instance></param>
        public MethodBuilderContext(AdapterContext adapterContext, MethodInfo methodInfo)
        {
            this.AdapterContext = adapterContext;
            this.Method = methodInfo;
            this.Parameters = this.Method.GetParameters();
            if (this.Parameters.Any())
            {
                this.ParameterTypes = this.Parameters.Select(p => p.ParameterType).ToArray();
            }

            if (this.Method.ContainsGenericParameters == true)
            {
                this.GenericArguments = this.Method.GetGenericArguments();
            }
        }

        /// <summary>
        /// Gets the adapter context.
        /// </summary>
        public AdapterContext AdapterContext { get; }

        /// <summary>
        /// Gets the method being built.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the method being proxied.
        /// </summary>
        public MethodInfo ProxiedMethod { get; private set; }

        /// <summary>
        /// Gets the methods parameters.
        /// </summary>
        public ParameterInfo[] Parameters { get; }

        /// <summary>
        /// Gets the paremeters of the method being proxied.
        /// </summary>
        public ParameterInfo[] ProxiedParameters { get; set; }

        /// <summary>
        /// Gets the methods parameter types.
        /// </summary>
        public Type[] ParameterTypes { get; }

        /// <summary>
        /// Gets the methods generic arguments.
        /// </summary>
        public Type[] GenericArguments { get; }

        /// <summary>
        /// Gets the methods out parameter locals.
        /// </summary>
        public Dictionary<int, LocalBuilder> OutParameters { get; private set; }

        /// <summary>
        /// Sets the proxied method.
        /// </summary>
        public MethodInfo SetProxiedMethod(
            Type baseType,
            string targetName,
            BindingFlags bindingFlags,
            ParameterInfo[] parameters)
        {
            this.ProxiedMethod = baseType.GetMethodWithParameters(
                    targetName,
                    bindingFlags,
                    parameters);

            if (this.ProxiedMethod != null)
            {
                this.ProxiedParameters = this.ProxiedMethod.GetParameters();
            }

            return this.ProxiedMethod;
        }

        /// <summary>
        /// Makes the proxied method into a generic method.
        /// </summary>
        /// <param name="typeArguments">A list of type arguments.</param>
        /// <returns>The new generic <see cref="MethodInfo"/>.</returns>
        public MethodInfo MakeGenericProxiedMethod(params Type[] typeArguments)
        {
            this.ProxiedMethod = this.ProxiedMethod.MakeGenericMethod(typeArguments);
            return this.ProxiedMethod;
        }

        /// <summary>
        /// Adds an out/ref parameter to the out/ref parameter processing list.
        /// </summary>
        /// <param name="argIndex">The argument index.</param>
        /// <param name="localArgumentValue">The argument value.</param>
        public void AddOutParameter(int argIndex, LocalBuilder localArgumentValue)
        {
            this.OutParameters = this.OutParameters ?? new Dictionary<int, LocalBuilder>();
            this.OutParameters[argIndex] = localArgumentValue;
        }

        /// <summary>
        /// Emits IL to return out parameters.
        /// </summary>
        /// <param name="ilGen">The <see cref="ILGenerator"/> to use.</param>
        public void EmitOutParameters(ILGenerator ilGen)
        {
            if (this.OutParameters == null ||
                this.OutParameters.Any() == false)
            {
                return;
            }

            foreach (var outParm in this.OutParameters)
            {
                Type fromType = outParm.Value.LocalType;
                Type toType = this.ParameterTypes[outParm.Key - 1].GetElementType();

                if (fromType != toType)
                {
                    if (toType.IsEnum == true)
                    {
                        ilGen.EmitStoreByRefArg(
                            outParm.Key,
                            outParm.Value,
                            () => ilGen.Emit(OpCodes.Conv_I4));
                    }
                    else if (toType.IsInterface == true)
                    {
                        LocalBuilder localToValue = ilGen.DeclareLocal(toType);
                        this.EmitAdaptedValue(ilGen, outParm.Value, localToValue);
                        ilGen.EmitStoreByRefArg(outParm.Key, localToValue);
                    }
                }
                else
                {
                    ilGen.EmitStoreByRefArg(outParm.Key, outParm.Value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ilGen"></param>
        /// <param name="localSourceValue"></param>
        /// <param name="localDestValue"></param>
        public void EmitAdaptedValue(
            ILGenerator ilGen,
            LocalBuilder localSourceValue,
            LocalBuilder localDestValue)
        {
            Type toType = localDestValue.LocalType;

            var adapterCtor = this.AdapterContext.GetAdapterConstructor(localSourceValue.LocalType, toType);
            if (adapterCtor == null)
            {
                throw new AdapterGenerationException("Unable to create adapter");
            }

            ilGen.Emit(OpCodes.Ldloc, localSourceValue);
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, this.AdapterContext.ServiceProviderField);
            ilGen.Emit(OpCodes.Newobj, adapterCtor);
            ilGen.Emit(OpCodes.Castclass, toType);
            ilGen.Emit(OpCodes.Stloc, localDestValue);
        }
    }
}