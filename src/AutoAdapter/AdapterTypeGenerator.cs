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
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A factory class for building adapter classes.
    /// </summary>
    internal class AdapterTypeGenerator
        : IAdapterTypeGenerator
    {
        /// <summary>
        /// A dependency injection container.
        /// </summary>
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterTypeGenerator"/> class.
        /// </summary>
        /// <param name="serviceProvider">Optional dependency injection container.</param>
        public AdapterTypeGenerator(IServiceProvider serviceProvider = null)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Makes the adapters type name.
        /// </summary>
        /// <param name="adaptedType">The type being adpated.</param>
        /// <param name="adapterType">The adapter type.</param>
        /// <returns>The type name.</returns>
        private static string MakeTypeName(Type adaptedType, Type adapterType)
        {
            string adaptedTypeName = adaptedType.Name;
            if (adaptedType.IsGenericType == true &&
                adaptedType.IsGenericTypeDefinition == false)
            {
                adaptedTypeName += $"({string.Join(",", adaptedType.GetGenericArguments().Select(t => t.FullName))})";
            }

            string adapterTypeName = adapterType.Name;
            if (adapterType.IsGenericType == true &&
                adapterType.IsGenericTypeDefinition == false)
            {
                adapterTypeName += $"({string.Join(",", adapterType.GetGenericArguments().Select(t => t.FullName))})";
            }

            return $"Dynamic.Adapters.{adaptedTypeName}_{adapterTypeName}";
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="typeToAdapt">The type to adapt.</param>
        /// <returns>A new adapter type.</returns>
        public Type CreateAdapterType<T>(Type typeToAdapt, IServiceProvider serviceProvider = null)
        {
            return this.CreateAdapterType(typeToAdapt, typeof(T), serviceProvider);
        }

        /// <summary>
        /// Create instance of an adapter.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="instance">The instance of the object to adapt.</param>
        /// <param name="serviceProvider">Optional dependency injection container.</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        public T CreateAdapter<T>(object instance, IServiceProvider serviceProvider = null)
        {
            return (T)this.CreateAdapter(instance, typeof(T), serviceProvider);
        }

        /// <summary>
        /// Creates an adapter object to represent the desired types.
        /// </summary>
        /// <param name="instance">The instance of the object to adapt.</param>
        /// <param name="types">The interface types to implement on the adapter type.</param>
        /// <param name="serviceProvider">Optional dependency injection container.</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        public object CreateAdapter(object instance, Type adapterType, IServiceProvider serviceProvider = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Type newAdapterType = this.CreateAdapterType(instance.GetType(), adapterType, serviceProvider);
            var ctor = newAdapterType.GetConstructor(new[] { instance.GetType(), typeof(IServiceProvider) });
            if (ctor != null)
            {
                return ctor.Invoke(new object[] { instance, serviceProvider} );
            }

            return Activator.CreateInstance(newAdapterType, instance, serviceProvider);
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <param name="adaptedType">The type to adapt.</param>
        /// <param name="adapterType">The interface type to implement on the adapter type.</param>
        /// <param name="serviceProvider">Optional dependency injection container.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        public Type CreateAdapterType(Type adaptedType, Type adapterType, IServiceProvider serviceProvider = null)
        {
            if (adaptedType == null)
            {
                throw new ArgumentNullException(nameof(adaptedType));
            }

            if (adapterType == null)
            {
                throw new ArgumentNullException(nameof(adapterType));
            }

            if (adapterType.IsInterface == false)
            {
                throw new ArgumentException("Adapter type must be an interface", nameof(adapterType));
            }

            serviceProvider = serviceProvider ?? this.serviceProvider;

            string typeName = MakeTypeName(adaptedType, adapterType);

            Type adapter = TypeFactory
                .Default
                .GetType(
                    typeName,
                    true);

            if (adapter == null)
            {
                adapter = this.GenerateAdapterType(typeName, adaptedType, adapterType, serviceProvider);
            }

            if (adapter.ContainsGenericParameters == true)
            {
                adapter = adapter.MakeGenericType(adapterType.GenericTypeArguments);
            }

            return adapter;
        }

        /// <summary>
        /// Generates the adapter type.
        /// </summary>
        /// <param name="typeName">The name of type being generated.</param>
        /// <param name="adaptedType">The type being adapted.</param>
        /// <param name="adapterType">The adapter interface type to implement.</param>
        /// <param name="serviceProvider">The dependency injection scope to use.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        private Type GenerateAdapterType(string typeName, Type adaptedType, Type adapterType, IServiceProvider serviceProvider)
        {
            TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public;

            TypeBuilder typeBuilder =
                TypeFactory
                .Default
                .ModuleBuilder
                .DefineType(
                    typeName,
                    typeAttributes);

            FieldBuilder adaptedTypeField =
                typeBuilder
                .DefineField(
                    "adaptedType",
                    adaptedType,
                    FieldAttributes.Private);

            FieldBuilder dependencyResolverField =
                typeBuilder
                .DefineField(
                    "serviceProvider",
                    typeof(IServiceProvider),
                    FieldAttributes.Private);

            // Implement the IAdaptedObject interface.
            typeBuilder.ImplementAdaptedObjectInterface(
                adaptedType,
                adaptedTypeField);

            // Add the adapter interface
            typeBuilder.AddAdapterInterface(adapterType);

            // Add a constructor to the type.
            var ctorBuilder =  this.AddConstructor(
                typeBuilder,
                adaptedType,
                adaptedTypeField,
                dependencyResolverField);

            // Implement interfaces.
            AdapterContext context = new AdapterContext(typeBuilder, null, adaptedType, serviceProvider, adaptedTypeField, dependencyResolverField, ctorBuilder);
            context = context.CreateTypeFactoryContext(adapterType);
            this.ImplementInterfaces(context);

            // Create the type.
            return typeBuilder.CreateTypeInfo().AsType();
        }

        /// <summary>
        /// Adds a method to the generated type to get an adapter extension method.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="adapterExtensionsField">A <see cref="FieldBuilder"/> which reprensents the field containing the adapter extensions.</param>
        /// <returns>A <see cref="MethodBuilder"/> that represents the new method.</returns>
        private MethodBuilder AddGetAdapterExtensionMethod(
            TypeBuilder typeBuilder,
            FieldBuilder adapterExtensionsField)
        {
            var method =
                typeBuilder
                .DefineMethod(
                    "GetAdapterExtension",
                    MethodAttributes.Private |
                    MethodAttributes.HideBySig,
                    CallingConventions.HasThis,
                    typeof(IAdapterExtension),
                    new[]
                    {
                        typeof(string)
                    });

            var genTypeBuilder = method.DefineGenericParameters("T", "TResult");

            var genericT = genTypeBuilder[0];
            genericT.SetGenericParameterAttributes(GenericParameterAttributes.None);

            var genericTResult = genTypeBuilder[1];
            genericTResult.SetGenericParameterAttributes(GenericParameterAttributes.None);

            method.SetParameters(genericT.AsType());
            method.SetReturnType(genericTResult.AsType());

            Type returnType = typeof(IAdapterExtension<,>)
                .MakeGenericType(genericT.AsType(), genericTResult.AsType());

            var methodIL = method.GetILGenerator();

            LocalBuilder extension = methodIL.DeclareLocal<IAdapterExtension>();
            LocalBuilder tryGetResult = methodIL.DeclareLocal<bool>();
            LocalBuilder returnValue = methodIL.DeclareLocal(returnType);

            Label nullResult = methodIL.DefineLabel();
            Label end = methodIL.DefineLabel();
            Label test = methodIL.DefineLabel();

            MethodInfo dictionaryTryGetValue = typeof(Dictionary<string, IAdapterExtension>).GetMethod("TryGetValue");
            MethodInfo adapterExtensionGetArg = typeof(IAdapterExtension).GetMethod("get_ArgumentType");
            MethodInfo typeOpEquality = typeof(Type).GetMethod("op_Equality");
            MethodInfo adapterExtensionGetReturnType = typeof(IAdapterExtension).GetMethod("get_ReturnType");

            methodIL.Emit(OpCodes.Nop);
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, adapterExtensionsField);
            methodIL.Emit(OpCodes.Brfalse_S, nullResult);

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, adapterExtensionsField);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldloca_S, extension);

            methodIL.Emit(OpCodes.Callvirt, dictionaryTryGetValue);

            methodIL.Emit(OpCodes.Stloc_S, tryGetResult);
            methodIL.Emit(OpCodes.Ldloc_1);
            methodIL.Emit(OpCodes.Brfalse_S, nullResult);

            methodIL.Emit(OpCodes.Nop);
            methodIL.Emit(OpCodes.Ldloc_S, extension);
            methodIL.Emit(OpCodes.Callvirt, adapterExtensionGetArg);

            methodIL.EmitTypeOf(genericT.AsType());
            methodIL.Emit(OpCodes.Call, typeOpEquality);
            methodIL.Emit(OpCodes.Brfalse_S, nullResult);

            methodIL.Emit(OpCodes.Ldloc_S, tryGetResult);
            methodIL.Emit(OpCodes.Callvirt, adapterExtensionGetReturnType);

            methodIL.EmitTypeOf(genericTResult.AsType());
            methodIL.Emit(OpCodes.Call, typeOpEquality);
            methodIL.Emit(OpCodes.Brfalse_S, nullResult);

            methodIL.Emit(OpCodes.Nop);
            methodIL.Emit(OpCodes.Ldloc_S, tryGetResult);
            methodIL.Emit(OpCodes.Castclass, returnType);
            methodIL.Emit(OpCodes.Stloc_S, returnValue);
            methodIL.Emit(OpCodes.Br_S, end);

            methodIL.MarkLabel(nullResult);

            methodIL.Emit(OpCodes.Ldnull);
            methodIL.Emit(OpCodes.Stloc_S, returnValue);

            methodIL.MarkLabel(end);

            methodIL.Emit(OpCodes.Ldloc_S, returnValue);
            methodIL.Emit(OpCodes.Ret);

            return method;
        }

        /// <summary>
        /// Adds a constructor to the adapter type.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> use to construct the type.</param>
        /// <param name="adaptedType">The <see cref="Type"/> being adapted.</param>
        /// <param name="adaptedTypeField">The <see cref="FieldBuilder"/> which will hold the instance of the adapted type.</param>
        /// <param name="dependencyResolverField">The <see cref="FieldBuilder"/> which will hold the instance of the dependency injection resolver.</param>
        /// <returns>The <see cref="ConstructorBuilder"/> used to build the constructor.</returns>
        private ConstructorBuilder AddConstructor(
            TypeBuilder typeBuilder,
            Type adaptedType,
            FieldBuilder adaptedTypeField,
            FieldBuilder dependencyResolverField)
        {
            var constructorBuilder =
                typeBuilder
                .DefineConstructor(
                    MethodAttributes.Public |
                    MethodAttributes.HideBySig |
                    MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName,
                    CallingConventions.HasThis,
                    new[] {
                        adaptedType,
                        typeof(IServiceProvider)
                    });;

            var methodIL = constructorBuilder.GetILGenerator();

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Stfld, adaptedTypeField);

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldarg_2);
            methodIL.Emit(OpCodes.Stfld, dependencyResolverField);

            methodIL.Emit(OpCodes.Ret);

            return constructorBuilder;
        }

        /// <summary>
        /// Implements the adapter types interfaces on the adapted type.
        /// </summary>
        /// <param name="adapterContext">The current adapter context.</param>
        private void ImplementInterfaces(AdapterContext adapterContext)
        {
            Type[] implementedInterfaces = adapterContext.NewType.GetInterfaces();
            if (implementedInterfaces != null)
            {
                foreach (Type iface in implementedInterfaces)
                {
                    AdapterContext ifaceContext = adapterContext.CreateTypeFactoryContext(iface);
                    this.ImplementInterfaces(ifaceContext);
                }
            }

            var propertyMethods = new Dictionary<string, MethodBuilder>();
            foreach (var memberInfo in adapterContext.NewType.GetMembers())
            {
                var methodContext = new MethodBuilderContext(adapterContext, memberInfo);
                if (memberInfo.MemberType == MemberTypes.Method)
                {
                    MethodInfo methodInfo = methodContext.Method;
                    MethodBuilder methodBuilder = this.BuildMethod(adapterContext, methodContext);

                    if (methodInfo.IsProperty() == true)
                    {
                        var propertyId = $"{methodInfo.Name}_{methodInfo.GetParameters().Length}";
                        propertyMethods.Add(propertyId, methodBuilder);
                    }
                }
                else if (memberInfo.MemberType == MemberTypes.Property)
                {
                    this.DefineProperty(
                        methodContext,
                        (PropertyInfo)memberInfo,
                        propertyMethods);
                }
            }
        }

        /// <summary>
        /// Defines a property.
        /// </summary>
        /// <param name="methodContext">An <see cref="MethodBuilderContext"/>.</param>
        /// <param name="propertyInfo">A <see cref="PropertyInfo"/>.</param>
        /// <param name="propertyMethods">A dictionary of property method implementations.</param>
        private void DefineProperty(
            MethodBuilderContext methodContext,
            PropertyInfo propertyInfo,
            IDictionary<string, MethodBuilder> propertyMethods)
        {
            // Build the property.
            var propertyBuilder = methodContext
                .AdapterContext
                .TypeBuilder
                .DefineProperty(
                    propertyInfo.Name,
                    PropertyAttributes.None,
                    propertyInfo.PropertyType,
                    null);

            var parameterLength = propertyInfo.GetIndexParameters().Length;
            var getPropertyId = $"{propertyInfo.PropertyGetName()}_{parameterLength}";

            MethodBuilder getMethod;
            if (propertyMethods.TryGetValue(getPropertyId, out getMethod) == true)
            {
                propertyBuilder.SetGetMethod(getMethod);
            }

            var setPropertyId = $"{propertyInfo.PropertySetName()}_{parameterLength}";

            MethodBuilder setMethod;
            if (propertyMethods.TryGetValue(setPropertyId, out setMethod) == true)
            {
                propertyBuilder.SetSetMethod(setMethod);
            }
        }

        /// <summary>
        /// Builds a method
        /// </summary>
        /// <param name="adapterContext">The current <see cref="AdapterContext"/.></param>
        /// <param name="methodContext">The current <see cref="MethodBuilderContext"/>.</param>
        private MethodBuilder BuildMethod(
            AdapterContext adapterContext,
            MethodBuilderContext methodContext)
        {
            MethodAttributes attrs = methodContext.Method.Attributes & ~MethodAttributes.Abstract;
            var methodBuilder = adapterContext
                .TypeBuilder
                .DefineMethod(
                    methodContext.Method.Name,
                    attrs | MethodAttributes.Virtual,
                    methodContext.MethodReturnType,
                    methodContext.ParameterTypes);

            if (methodContext.Method.IsGenericMethodDefinition == true)
            {
                var genTypeBuilders = methodBuilder
                    .DefineGenericParameters(
                        methodContext.GenericArguments.Select(t => t.Name)
                            .ToArray());

                for (int i = 0; i < methodContext.GenericArguments.Length; i++)
                {
                    genTypeBuilders[i].SetGenericParameterAttributes(
                        methodContext.GenericArguments[i]
                            .GenericParameterAttributes
                    );
                }
            }

            for (int i = 0; i < methodContext.Parameters.Length; i++)
            {
                methodBuilder.DefineParameter(
                    i + 1,
                    methodContext.Parameters[i].Attributes,
                    methodContext.Parameters[i].Name);
            }

            var methodIL = methodBuilder.GetILGenerator();

            bool implemented = false;
            IServiceProvider scope = adapterContext.ServiceProvider;
            if (scope != null)
            {
                foreach (var extension in scope.GetServices<IAdapterFactoryExtension>())
                {
                    if (extension.ImplementMethod(methodContext.Method, methodIL, adapterContext) == true)
                    {
                        implemented = true;
                        break;
                    }
                }
            }

            if (implemented == false)
            {
                this.BuildMethod(
                    adapterContext,
                    methodContext,
                    methodIL);
            }

            return methodBuilder;
        }

        /// <summary>
        /// Implements a method.
        /// </summary>
        /// <param name="adapterContext">The adapter factories current context.</param>
        /// <param name="methodContext">The <see cref="MethodInfo"/> of the method being implemented.</param>
        /// <param name="ilGen">The methods <see cref="ILGenerator"/>.</param>
        private void BuildMethod(
            AdapterContext adapterContext,
            MethodBuilderContext methodContext,
            ILGenerator ilGen)
        {
            // Get the method being proxied.
            if (methodContext.TargetStaticType == null)
            {
                methodContext.SetProxiedMethod(
                    adapterContext.BaseType,
                    methodContext.TargetMemberName,
                    BindingFlags.Public | BindingFlags.Instance,
                    methodContext.Parameters);
            }
            else
            {
                methodContext.SetProxiedMethod(
                    methodContext.TargetStaticType,
                    methodContext.TargetMemberName,
                    BindingFlags.Public | BindingFlags.Static,
                    methodContext.Parameters);
            }

            // Was a proxy method found?
            if (methodContext.ProxiedMethod == null)
            {
                this.EmitMethodNotFoundProcessing(
                    adapterContext,
                    methodContext,
                    ilGen);

                return;
            }

            Type proxiedReturnType = methodContext.ProxiedMethodReturnType;

            LocalBuilder methodReturnLocal = null;
            LocalBuilder proxiedReturnLocal = null;

            // Does the method have a return type?
            if (methodContext.MethodReturnType != null &&
                methodContext.MethodReturnType != typeof(void))
            {
                // Check if the return value has an adapter attribute applied to it.
                AdapterAttribute attr = methodContext.MethodReturnType.GetCustomAttribute<AdapterAttribute>();
                if (attr != null)
                {
                    proxiedReturnType = attr.GetAdaptedType();
                }

                // Declare locals
                methodReturnLocal = ilGen.DeclareLocal(methodContext.MethodReturnType);
                proxiedReturnLocal = ilGen.DeclareLocal(proxiedReturnType);
            }

            // Is the proxied method static?
            if (methodContext.ProxiedMethod.IsStatic == true)
            {
                // Is it an extension method?
                if (methodContext.ProxiedMethod.IsDefined(typeof(ExtensionAttribute), false) == true)
                {
                    ParameterInfo[] extensionParms = methodContext.ProxiedParameters;
                    if (extensionParms.Length > 0 &&
                        adapterContext.BaseType == extensionParms[0].ParameterType)
                    {
                        ParameterInfo[] proxyParms = new ParameterInfo[extensionParms.Length - 1];
                        if (proxyParms.Length > 0)
                        {
                            Array.Copy(extensionParms, 1, proxyParms, 0, proxyParms.Length);
                        }

                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                        ilGen.EmitParameters(methodContext);
                        ilGen.Emit(OpCodes.Call, methodContext.ProxiedMethod);
                    }
                    else
                    {
                        // If we get here then the extension method is not for the class
                        // being adapted.
                        ilGen.ThrowException(typeof(InvalidOperationException));
                    }
                }
                else
                {
                    ilGen.EmitParameters(methodContext);
                    ilGen.Emit(OpCodes.Call, methodContext.ProxiedMethod);
                }
            }
            else
            {
                if (methodContext.Method.IsGenericMethodDefinition == true)
                {
                    LocalBuilder genericArgs = ilGen.DeclareLocal<Type[]>();
                    LocalBuilder adaptedGenericArgs = ilGen.DeclareLocal<Type[]>();
                    LocalBuilder args = ilGen.DeclareLocal<object[]>();
                    LocalBuilder length  = ilGen.DeclareLocal<int>();

                    ilGen.EmitArray(
                        genericArgs,
                        methodContext.GenericArguments.Length,
                        (i) =>
                        {
                            var attr = ilGen.DeclareLocal<AdapterAttribute>();
                            ilGen.EmitGetCustomAttribute<AdapterAttribute>(methodContext.GenericArguments[i]);
                            ilGen.Emit(OpCodes.Stloc, attr);
                            ilGen.Emit(OpCodes.Ldloc, attr);
                            ilGen.EmitIfNotNull(
                                () =>
                                {
                                    ilGen.Emit(OpCodes.Ldloc, attr);
                                    ilGen.Emit(OpCodes.Callvirt, typeof(AdapterAttribute).GetMethod("GetAdaptedType"));
                                },
                                () => ilGen.Emit(OpCodes.Ldnull));
                        });

                    ilGen.EmitArray(
                        adaptedGenericArgs,
                        methodContext.GenericArguments.Length,
                        (i) =>
                        {
                            ilGen.EmitTypeOf(methodContext.GenericArguments[i]);
                        });

                    ilGen.Emit(OpCodes.Ldc_I4, methodContext.ProxiedParameters.Length);
                    ilGen.Emit(OpCodes.Stloc, length);

                    ilGen.Emit(OpCodes.Ldloc, length);
                    ilGen.Emit(OpCodes.Newarr, typeof(object));
                    ilGen.Emit(OpCodes.Stloc, args);

                    // Transfer arguments in to array.
                    for (int i = 0; i < methodContext.Parameters.Length; i++)
                    {
                        if (methodContext.Parameters[i].IsOut == false)
                        {
                            ilGen.Emit(OpCodes.Ldloc, args);
                            ilGen.Emit(OpCodes.Ldc_I4, i);
                            ilGen.Emit(OpCodes.Ldarg, i + 1);
                            ilGen.Emit(OpCodes.Stelem_Ref);
                        }
                    }

                    // Invoke method
                    ilGen.EmitMethod(methodContext.ProxiedMethod, methodContext.AdapterContext.BaseType);
                    ilGen.Emit(OpCodes.Ldloc, adaptedGenericArgs);
                    ilGen.Emit(OpCodes.Ldloc, genericArgs);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                    ilGen.Emit(OpCodes.Ldloc, args);
                    ilGen.Emit(OpCodes.Call, typeof(AdapterExtensionMethods).GetMethod("InvokeAdapted", new[] { typeof(MethodInfo), typeof(Type[]), typeof(Type[]), typeof(object), typeof(object[]) }));

                    // Process out parameters
                    for (int i = 0; i < methodContext.Parameters.Length; i++)
                    {
                        if (methodContext.Parameters[i].IsOut == true)
                        {
                            ilGen.Emit(OpCodes.Ldarg, i + 1);
                            ilGen.Emit(OpCodes.Ldloc, args);
                            ilGen.Emit(OpCodes.Ldc_I4, i);
                            ilGen.Emit(OpCodes.Ldelem_Ref);
                            ilGen.Emit(OpCodes.Stind_Ref);
                        }
                    }
                }

                // Is the adapted type a class?
                else if (adapterContext.BaseType.IsClass == true)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                    ilGen.EmitParameters(methodContext);

                    ilGen.Emit(OpCodes.Callvirt, methodContext.ProxiedMethod);
                }

                // Is the adapted type a value type?
                else if (adapterContext.BaseType.IsValueType == true)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldflda, adapterContext.BaseObjectField);
                    ilGen.EmitParameters(methodContext);
                    ilGen.Emit(OpCodes.Call, methodContext.ProxiedMethod);
                }
            }

            // Does the method expect a return value?
            if (methodReturnLocal != null)
            {
                // Store the value on the evaluation stack
                // into a local variable.
                ilGen.Emit(OpCodes.Stloc, proxiedReturnLocal);
            }

            // Emit any out parameters
            methodContext.EmitOutParameters(ilGen);

            // Does the method expect a return value?
            if (methodReturnLocal != null)
            {
                // Emit return value handling.
                this.EmitReturnValueProcessing(
                    methodContext,
                    ilGen,
                    proxiedReturnLocal,
                    methodReturnLocal);

                // Load the return value back onto the evaluation stack.
                ilGen.Emit(OpCodes.Ldloc_S, methodReturnLocal);
            }

            // Return from the method.
            ilGen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Handles methods the return a value.
        /// </summary>
        /// <param name="methodContext">The current <see cref="MethodBuilderContext"/>.</param>
        /// <param name="ilGen">The current <see cref="ILGenerator"/>.</param>
        /// <param name="proxiedReturnLocal">A <see cref="LocalBuilder"/> for the proxy return value.</param>
        /// <param name="methodReturnLocal">A <see cref="LocalBuilder"/> for the return value.</param>
        private void EmitReturnValueProcessing(
            MethodBuilderContext methodContext,
            ILGenerator ilGen,
            LocalBuilder proxiedReturnLocal,
            LocalBuilder methodReturnLocal)
        {
            Type proxiedReturnType = proxiedReturnLocal.LocalType;
            Type returnType = methodReturnLocal.LocalType;

            // Does the method have an adapter extension method applied?
            if (methodContext.ExtensionMethodName != null)
            {
                // Emit the code to call the extension method.
                ilGen.EmitAdapterExtensionExecution(
                    methodContext.ExtensionMethodName,
                    proxiedReturnType,
                    proxiedReturnType,
                    methodContext.AdapterContext,
                    proxiedReturnLocal,
                    proxiedReturnLocal);
            }

            // Do the return types match?
            if (returnType != proxiedReturnType)
            {
                ilGen.EmitAdaptedResult(
                    methodContext.AdapterContext,
                    proxiedReturnLocal,
                    returnType);
            }
            else
            {
                ilGen.Emit(OpCodes.Ldloc, proxiedReturnLocal);
            }

            // Store the value on the top of the execution stack
            // into the return value variable.
            ilGen.Emit(OpCodes.Stloc_S, methodReturnLocal);
        }

        /// <summary>
        /// Handles various scenarios when a proxiable method is not found.
        /// </summary>
        /// <param name="adapterContext">The current <see cref="AdapterContext"/>.</param>
        /// <param name="methodContext">The current <see cref="MethodBuilderContext"/>.</param>
        /// <param name="ilGen">The current <see cref="ILGenerator"/>.</param>
        private void EmitMethodNotFoundProcessing(
            AdapterContext adapterContext,
            MethodBuilderContext methodContext,
            ILGenerator ilGen)
        {
            // No method found
            MethodInfo methodInfo = methodContext.Method;
            FieldInfo field = null;

            // Is the desired method is a property getter and
            // is there a public field with same name?
            if (methodInfo.IsPropertyGet() == true &&
                methodContext.Parameters.Length == 1 &&
                (field = adapterContext.BaseType.GetField(methodInfo.Name.Substring(4))) != null)
            {
                LocalBuilder sourceValue = ilGen.DeclareLocal(adapterContext.BaseType);
                LocalBuilder fieldValue = ilGen.DeclareLocal(field.FieldType);
                LocalBuilder returnValue = ilGen.DeclareLocal(methodInfo.ReturnType);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                ilGen.Emit(OpCodes.Stloc_S, sourceValue);

                ilGen.Emit(OpCodes.Ldloc_S, sourceValue);
                ilGen.Emit(OpCodes.Ldfld, field);
                ilGen.Emit(OpCodes.Stloc_S, fieldValue);

                if (methodContext.ExtensionMethodName != null)
                {
                    ilGen.EmitAdapterExtensionExecution(
                            methodContext.ExtensionMethodName,
                            adapterContext.BaseType,
                            methodInfo.ReturnType,
                            adapterContext,
                            fieldValue,
                            returnValue);

                    ilGen.Emit(OpCodes.Ldloc_S, returnValue);
                    ilGen.Emit(OpCodes.Ret);
                }
                else if (field.FieldType != methodInfo.ReturnType)
                {
                    ilGen.EmitAdaptedResult(
                        adapterContext,
                        fieldValue,
                        methodInfo.ReturnType);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldloc_S, fieldValue);
                }

                ilGen.Emit(OpCodes.Ret);
            }

            // Is the desired method a property setter and
            // is there a public field with same name?
            else if (methodInfo.IsPropertySet() == true &&
                methodContext.Parameters.Length == 1 &&
                (field = adapterContext.BaseType.GetField(methodInfo.Name.Substring(4))) != null)
            {
                Type sourceType = methodContext.Parameters[0].ParameterType;

                var baseValue = ilGen.DeclareLocal(adapterContext.BaseType);
                var sourceValue = ilGen.DeclareLocal(sourceType);

                // TODO: Look at adpated types
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                ilGen.Emit(OpCodes.Ldloc_S, baseValue);

                ilGen.Emit(OpCodes.Ldarg_1);
                ilGen.Emit(OpCodes.Stloc_S, sourceValue);

                if (field.FieldType != sourceType)
                {
                    var isAssignable = ilGen.DeclareLocal<bool>();
                    var adapted = ilGen.DefineLabel();

                    ilGen.Emit(OpCodes.Ldloc_S, baseValue);
                    ilGen.EmitGetAdaptedObject(sourceValue);
                    ilGen.Emit(OpCodes.Stfld, field);
                    ilGen.Emit(OpCodes.Nop);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldloc_S, baseValue);
                    ilGen.Emit(OpCodes.Ldloc_S, sourceValue);
                    ilGen.Emit(OpCodes.Stfld, field);
                }

                ilGen.Emit(OpCodes.Ret);
            }

            // Does the desired method have a return type and does it
            // have an adapter extension method applied?
            else if (methodContext.ExtensionMethodName != null &&
                methodInfo.ReturnType != typeof(void))
            {
                LocalBuilder sourceValue = ilGen.DeclareLocal(adapterContext.BaseType);
                LocalBuilder returnValue = ilGen.DeclareLocal(methodInfo.ReturnType);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                ilGen.Emit(OpCodes.Stloc_S, sourceValue);

                ilGen.EmitAdapterExtensionExecution(
                        methodContext.ExtensionMethodName,
                        adapterContext.BaseType,
                        methodInfo.ReturnType,
                        adapterContext,
                        sourceValue,
                        returnValue);

                ilGen.Emit(OpCodes.Ldloc_S, returnValue);
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                // Unable to implement the desired method.
                ilGen.ThrowException(typeof(NotImplementedException));
            }
        }
    }
}
