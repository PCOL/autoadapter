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
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A factory for create adapter function factories.
    /// </summary>
    public class AdapterFuncTypeGenerator
        : IAdapterFuncTypeGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterFuncTypeGenerator"/> class.
        /// </summary>
        public AdapterFuncTypeGenerator()
//            : base("Dynamic", "Adapters")
        {
        }

        /// <summary>
        /// Gets the name of an adapter function type.
        /// </summary>
        /// <param name="returnType">The function type.</param>
        /// <param name="adaptedType">THe adapted type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns>The type name.</returns>
        private static string MakeTypeName(Type returnType, Type adaptedType, Type[] parameterTypes)
        {
            return string.Format("Dynamic.AdapterFunction_{0}_{1}_{2}",
                returnType.Name,
                adaptedType != null ? adaptedType.Name : "#",
                string.Join("_", parameterTypes.Select(p => p.Name)));
        }

        /// <summary>
        /// Creates a delegate to the function.
        /// </summary>
        /// <param name="returnType">The return type.</param>
        /// <param name="adaptedType">The adapted return type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance.</param>
        /// <returns>A delegate.</returns>
        public Delegate CreateDelegate(Type returnType, Type adaptedType, Type[] parameterTypes, IServiceProvider serviceProvider)
        {
            Type delegateType = this.CreateDelegateType(returnType, adaptedType, parameterTypes, serviceProvider);
            MethodInfo methodInvoke = delegateType.GetMethod("Invoke");

            object delegateInstance = Activator.CreateInstance(delegateType);

            Type[] genArgs = parameterTypes.CopyToAndAppendExtras(returnType);
            Type funcType = Type.GetType(string.Format("System.Func`{0}", genArgs.Length)).MakeGenericType(genArgs);
            //return Delegate.CreateDelegate(funcType, delegateInstance, methodInvoke);
            return methodInvoke.CreateDelegate(funcType, delegateInstance);
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <param name="returnType">The function type.</param>
        /// <param name="adaptedType">The adapted return type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        public Type CreateDelegateType(Type returnType, Type adaptedType, Type[] parameterTypes, IServiceProvider serviceProvider)
        {
            Type delegateType = AssemblyCache.GetType(MakeTypeName(returnType, adaptedType, parameterTypes), true);
            if (delegateType == null)
            {
                delegateType = this.GenerateDelegateType(returnType, adaptedType, parameterTypes, serviceProvider);
            }

            return delegateType;
        }

        /// <summary>
        /// Generates the adapter function type.
        /// </summary>
        /// <param name="returnType">The function type.</param>
        /// <param name="adaptedType">The adapted return type.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter function.</returns>
        private Type GenerateDelegateType(Type returnType, Type adaptedType, Type[] parameterTypes, IServiceProvider serviceProvider)
        {
            TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

            TypeBuilder typeBuilder =
                TypeFactory
                .Default
                .ModuleBuilder
                .DefineType(
                    MakeTypeName(
                        returnType,
                        adaptedType,
                        parameterTypes),
                    typeAttributes);

            FieldBuilder serviceProviderField =
                typeBuilder
                .DefineField(
                    "serviceProvider",
                    typeof(IServiceProvider),
                    FieldAttributes.Private);

            this.EmitConstructor(typeBuilder, serviceProviderField);

            this.EmitInvokeMethod(typeBuilder, returnType, adaptedType, parameterTypes, serviceProviderField);

            // Create the type.
            return typeBuilder.CreateTypeInfo().AsType();
        }

        private void EmitConstructor(TypeBuilder typeBuilder, FieldBuilder serviceProviderField)
        {
            var ctorBuilder =
                typeBuilder
                .DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.HasThis,
                    new[] { serviceProviderField.FieldType });

            var ilGen = ctorBuilder.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, serviceProviderField);
            ilGen.Emit(OpCodes.Ret);

                // MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                // CallingConventions.HasThis,
                // Type.EmptyTypes);

            //ctorBuilder.DefineParameter(1, ParameterAttributes.None, "serviceProvider");
            // ILGenerator il = ctorBuilder.GetILGenerator();
            // il.Emit(OpCodes.Ldarg_0);
            // il.Emit(OpCodes.Ldarg_1);
            // il.Emit(OpCodes.Stfld, serviceProviderField);
            // il.Emit(OpCodes.Ret);
        }

        private void EmitInvokeMethod(TypeBuilder typeBuilder, Type returnType, Type adaptedType, Type[] parameterTypes, FieldBuilder serviceProviderField)
        {
            var methodBuilder =
                typeBuilder
                .DefineMethod(
                    "Invoke",
                    MethodAttributes.Public,
                    CallingConventions.HasThis,
                    returnType,
                    parameterTypes);

            var ilGen = methodBuilder.GetILGenerator();

            ConstructorInfo adaptedCtor = adaptedType.GetConstructor(parameterTypes);
            ConstructorInfo returnCtor = returnType.GetConstructor(parameterTypes);

            MethodInfo beginScopeMethod = typeof(IServiceScopeFactory).GetMethod("CreateScope", Type.EmptyTypes);
            MethodInfo getServiceMethod = typeof(IServiceProvider).GetMethod("GetService", new[] { typeof(Type) });
            MethodInfo createAdapterMethod = typeof(IAdapterTypeGenerator).GetMethod("CreateAdapter",new[] { typeof(IServiceProvider) }).MakeGenericMethod(returnType);

            LocalBuilder localReturnType = ilGen.DeclareLocal(returnType);
            LocalBuilder localAdaptedType = ilGen.DeclareLocal(adaptedType);
            LocalBuilder localAdapterFactory = ilGen.DeclareLocal<IAdapterTypeGenerator>();
            LocalBuilder localExtensionMethodScope = ilGen.DeclareLocal<IServiceScope>();

            // Load the arguments.
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                ilGen.Emit(OpCodes.Ldarg, i);
            }

            // Does the type need to be adapted?
            if (adaptedType != null)
            {
                // Construct the new object.
                ilGen.Emit(OpCodes.Newobj, adaptedCtor);
                ilGen.Emit(OpCodes.Stloc, localAdaptedType);

                // Call begin scope.
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, serviceProviderField);
                ilGen.Emit(OpCodes.Castclass, typeof(IServiceScopeFactory));
                ilGen.Emit(OpCodes.Callvirt, beginScopeMethod);

                    // .LdArg0()
                    // .LdFld(serviceProviderField)
                    // .CastClass<IServiceScopeFactory>()
                    // .CallVirt(beginScopeMethod)
                ilGen.Emit(OpCodes.Stloc, localExtensionMethodScope);

                ilGen.Using(
                    localExtensionMethodScope,
                    () =>
                    {
                        // Resolve the adapter factory.
                        ilGen.Emit(OpCodes.Ldloc, localExtensionMethodScope);
                        ilGen.EmitTypeOf<IAdapterTypeGenerator>();
                        ilGen.Emit(OpCodes.Callvirt, getServiceMethod);
                        ilGen.Emit(OpCodes.Stloc, localAdapterFactory);

                        // Create the adapter.
                        ilGen.Emit(OpCodes.Ldloc, localAdapterFactory);
                        ilGen.Emit(OpCodes.Ldloc, localAdaptedType);
                        ilGen.Emit(OpCodes.Ldloc, localExtensionMethodScope);
                        ilGen.Emit(OpCodes.Callvirt, createAdapterMethod);
                        ilGen.Emit(OpCodes.Castclass, returnType);
                        ilGen.Emit(OpCodes.Stloc, localReturnType);
                    });
            }
            else
            {
                // Construct new object.
                ilGen.Emit(OpCodes.Newobj, returnCtor);
                ilGen.Emit(OpCodes.Stloc, localReturnType);
            }

            // Load the value and return.
            ilGen.Emit(OpCodes.Ldloc, localReturnType);
            ilGen.Emit(OpCodes.Ret);
        }
    }
}
