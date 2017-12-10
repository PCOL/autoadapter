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
    using System.Runtime.CompilerServices;
    using AutoAdapter.Extensions;
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
        /// <param name="memberInfo">A <see cref="MemberInfo"/> instance></param>
        public MethodBuilderContext(AdapterContext adapterContext, MemberInfo memberInfo)
        {
            this.AdapterContext = adapterContext;
            this.Member = memberInfo;

            if (memberInfo.MemberType == MemberTypes.Method)
            {
                this.Method = (MethodInfo)this.Member;
                this.Parameters = this.Method.GetParameters();
                if (this.Parameters.Any())
                {
                    this.ParameterTypes = this.Parameters.Select(p => p.ParameterType).ToArray();
                }

                if (this.Method.ContainsGenericParameters == true)
                {
                    this.GenericArguments = this.Method.GetGenericArguments();
                }

                this.AddTargetMethodDetailsToContext(this.Method);
            }
            else if (memberInfo.MemberType == MemberTypes.Property)
            {
                this.Property = (PropertyInfo)memberInfo;
                this.AddTargetPropertyDetailsToContext(this.Property);
            }
        }

        /// <summary>
        /// Gets the adapter context.
        /// </summary>
        public AdapterContext AdapterContext { get; }

        /// <summary>
        /// Gets the <see cref="MemberInfo"/>.
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> is the member is for a property.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the method being built.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the methods return type.
        /// </summary>
        public Type MethodReturnType => this.Method?.ReturnType;

        /// <summary>
        /// Gets the method being proxied.
        /// </summary>
        public MethodInfo ProxiedMethod { get; private set; }

        /// <summary>
        /// Gets the proxied methods return type.
        /// </summary>
        public Type ProxiedMethodReturnType => this.ProxiedMethod?.ReturnType;

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
        /// Gets or sets the name of a the target.
        /// </summary>
        public string TargetMemberName { get; set; }

        /// <summary>
        /// Gets or sets the name of an extension method.
        /// </summary>
        public string ExtensionMethodName { get; set; }

        /// <summary>
        /// Gets or sets the target type.
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Gets or sets the target static type.
        /// </summary>
        public Type TargetStaticType { get; set; }

        /// <summary>
        /// Gets a value indicating whether or not the proxied method is static
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the proxied method is an extension method.
        /// </summary>
        public bool IsExtension { get; private set; }

        /// <summary>
        /// Gets the methods out parameter locals.
        /// </summary>
        public Dictionary<int, LocalBuilder> OutParameters { get; set; }

        /// <summary>
        /// Adds the target property details to the <see cref="MethodBuilderContext"/>.
        /// </summary>
        /// <param name="propertyInfo">A <see cref="PropertyInfo"/> instance.</param>
        private void AddTargetPropertyDetailsToContext(
            PropertyInfo propertyInfo)
        {
            this.TargetMemberName = propertyInfo.Name;

            // Check for a property extension attribute.
            AdapterExtensionAttribute adapterAttr = propertyInfo.GetCustomAttribute<AdapterExtensionAttribute>();
            if (adapterAttr != null)
            {
                this.ExtensionMethodName = adapterAttr.ExtensionName;
            }

            var adapterImpl = propertyInfo.GetCustomAttribute<AdapterImplAttribute>();
            if (adapterImpl != null)
            {
                string attrTargetName = adapterImpl.GetMemberTargetName(
                    out Type targetStaticType,
                    out TargetMemberType targetMemberType,
                    out Type targetType);

                if (targetMemberType == TargetMemberType.Property ||
                    targetMemberType == TargetMemberType.NotSet)
                {
                    this.TargetMemberName =
                        attrTargetName.IsNullOrEmpty() == false ?
                        propertyInfo.Name + attrTargetName :
                        this.TargetMemberName;
                }
                else if (targetMemberType == TargetMemberType.Method)
                {
                    this.TargetMemberName =
                        attrTargetName.IsNullOrEmpty() == false ?
                        attrTargetName :
                        this.TargetMemberName;
                }

                this.TargetType = targetType;
                this.TargetStaticType = targetStaticType;
            }
        }

        /// <summary>
        /// Adds the target methods details to the <see cref="MethodBuilderContext"/>.
        /// </summary>
        /// <param name="methodInfo">A <see cref="MethodInfo"/> instance.</param>
        private void AddTargetMethodDetailsToContext(
            MethodInfo methodInfo)
        {
            MemberInfo memberInfo = methodInfo;
            if (methodInfo.IsProperty() == true)
            {
                memberInfo = methodInfo.GetProperty();
            }

            this.TargetMemberName = methodInfo.Name;

            // Check for a return type extension attribute.
            object[] adapterAttrs = methodInfo
                .ReturnTypeCustomAttributes
                .GetCustomAttributes(typeof(AdapterExtensionAttribute), false);

            if (adapterAttrs != null &&
                adapterAttrs.Any() == true)
            {
                this.ExtensionMethodName = ((AdapterExtensionAttribute)adapterAttrs.First()).ExtensionName;
            }

            var adapterImpl = memberInfo.GetCustomAttribute<AdapterImplAttribute>();
            if (adapterImpl != null)
            {
                string attrTargetName =adapterImpl.GetMemberTargetName(
                    out Type targetStaticType,
                    out TargetMemberType targetMemberType,
                    out Type targetType);

                if (targetMemberType == TargetMemberType.Property)
                {
                    this.TargetMemberName =
                        attrTargetName.IsNullOrEmpty() == false ?
                        methodInfo.Name.Substring(0, 4) + attrTargetName :
                        this.TargetMemberName;
                }
                else if (targetMemberType == TargetMemberType.Method ||
                    targetMemberType == TargetMemberType.NotSet)
                {
                    this.TargetMemberName =
                        attrTargetName.IsNullOrEmpty() == false ?
                        attrTargetName :
                        this.TargetMemberName;
                }

                this.TargetType = targetType;
                this.TargetStaticType = targetStaticType;
            }
        }

        /// <summary>
        /// Sets the proxied method.
        /// </summary>
        /// <param name="baseType">The base type containing the proxied method.</param>
        /// <param name="methodName">The name of the method to proxy.</param>
        /// <param name="bindingFlags">The binding flags used to find the method.</param>
        /// <param name="parameters">The methods required parameters.</param>
        public MethodInfo SetProxiedMethod(
            Type baseType,
            string methodName,
            BindingFlags bindingFlags,
            ParameterInfo[] parameters)
        {
            this.ProxiedMethod = baseType.GetMethodWithParameters(
                    methodName,
                    bindingFlags,
                    parameters);

            this.IsStatic = (bindingFlags & BindingFlags.Static) != 0;

            if (this.ProxiedMethod != null)
            {
                this.ProxiedParameters = this.ProxiedMethod.GetParameters();
                this.IsExtension = this.ProxiedMethod.IsDefined(typeof(ExtensionAttribute));
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
                        ilGen.EmitAdaptedValue(this.AdapterContext, outParm.Value, localToValue);
                        ilGen.EmitStoreByRefArg(outParm.Key, localToValue);
                    }
                    else if (toType.IsArray == true)
                    {
                        Type fromElemType = outParm.Value.LocalType.GetElementType();
                        Type toElemType = toType.GetElementType();

                        LocalBuilder localToArray = ilGen.DeclareLocal(toType);
                        LocalBuilder localToArrayLength = ilGen.DeclareLocal(typeof(int));

                        ilGen.Emit(OpCodes.Ldloc, outParm.Value);
                        ilGen.Emit(OpCodes.Ldlen);
                        ilGen.Emit(OpCodes.Conv_I4);
                        ilGen.Emit(OpCodes.Dup);
                        ilGen.Emit(OpCodes.Stloc_S, localToArrayLength);
                        ilGen.Emit(OpCodes.Newarr, toElemType);
                        ilGen.Emit(OpCodes.Stloc, localToArray);

                        ilGen.EmitFor(
                            localToArrayLength,
                            (index) =>
                            {
                                ilGen.Emit(OpCodes.Ldloc, localToArray);
                                ilGen.Emit(OpCodes.Ldloc, index);

                                ilGen.Emit(OpCodes.Ldloc, outParm.Value);
                                ilGen.Emit(OpCodes.Ldloc, index);
                                ilGen.Emit(OpCodes.Ldelem_Ref);
                                ilGen.EmitAdaptedValue(this.AdapterContext, fromElemType, toElemType);

                                ilGen.Emit(OpCodes.Stelem_Ref);
                            });

                        ilGen.EmitStoreByRefArg(outParm.Key, localToArray);
                    }
                }
                else
                {
                    ilGen.EmitStoreByRefArg(outParm.Key, outParm.Value);
                }
            }
        }
    }
}