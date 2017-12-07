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
            return $"Dynamic.Adapters.{adaptedType.Name}_{adapterType.Name}";
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
            Type newAdapterType = TypeFactory.Default.GetType(typeName, true);
            if (newAdapterType == null)
            {
                newAdapterType= this.GenerateAdapterType(adaptedType, adapterType, serviceProvider);
            }

            return newAdapterType;
        }

        /// <summary>
        /// Generates the adapter type.
        /// </summary>
        /// <param name="adaptedType">The type being adapted.</param>
        /// <param name="adapterType">The adapter interface type to implement.</param>
        /// <param name="serviceProvider">The dependency injection scope to use.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        private Type GenerateAdapterType(Type adaptedType, Type adapterType, IServiceProvider serviceProvider)
        {
            TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public;

            TypeBuilder typeBuilder =
                TypeFactory
                .Default
                .ModuleBuilder
                .DefineType(
                    MakeTypeName(adaptedType, adapterType),
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
                    new[] { typeof(string) });

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

            Dictionary<string, MethodBuilder> propertyMethods = new Dictionary<string, MethodBuilder>();

            foreach (var memberInfo in adapterContext.NewType.GetMembers())
            {
                var builderContext = new BuilderContext(adapterContext, memberInfo);
                if (memberInfo.MemberType == MemberTypes.Method)
                {
                    MethodInfo methodInfo = builderContext.Method;
                    MethodBuilder methodBuilder = null;
                    if (methodInfo.ContainsGenericParameters == true)
                    {
                        methodBuilder = this.BuildGenericMethod(adapterContext, builderContext);
                    }
                    else
                    {
                        methodBuilder = this.BuildMethod(adapterContext, builderContext);
                    }

                    if (methodInfo.IsProperty() == true)
                    {
                        var propertyId = $"{methodInfo.Name}_{methodInfo.GetParameters().Length}";
                        propertyMethods.Add(propertyId, methodBuilder);
                    }
                }
                else if (memberInfo.MemberType == MemberTypes.Property)
                {
                    this.DefineProperty(
                        builderContext,
                        (PropertyInfo)memberInfo,
                        propertyMethods);
                }
            }
        }

        /// <summary>
        /// Defines a property.
        /// </summary>
        /// <param name="context">An <see cref="AdapterContext"/>.</param>
        /// <param name="propertyInfo">A <see cref="PropertyInfo"/>.</param>
        /// <param name="propertyMethods">A dictionary of property method implementations.</param>
        private void DefineProperty(
            BuilderContext context,
            PropertyInfo propertyInfo,
            IDictionary<string, MethodBuilder> propertyMethods)
        {
            // Build the property.
            var propertyBuilder = context
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
        /// Builds a generic method.
        /// </summary>
        /// <param name="adapterContext">The current <see cref="AdapterContext"/.></param>
        /// <param name="builderContext">The current <see cref="BuilderContext"/>.</param>
        private MethodBuilder BuildGenericMethod(AdapterContext adapterContext, BuilderContext builderContext)
        {
            MethodBuilder methodBuilder = adapterContext
                .TypeBuilder
                .DefineMethod(
                    builderContext.Method.Name,
                    MethodAttributes.Public |
                    MethodAttributes.Virtual,
                    builderContext.MethodReturnType,
                    builderContext.ParameterTypes);

            var genTypeBuilders = methodBuilder.DefineGenericParameters(
                builderContext.GenericArguments.Select(t => t.Name).ToArray());

            for (int i = 0; i < builderContext.GenericArguments.Length; i++)
            {
                genTypeBuilders[i].SetGenericParameterAttributes(
                    builderContext.GenericArguments[i]
                        .GenericParameterAttributes
                );
            }

            ILGenerator methodIL = methodBuilder.GetILGenerator();

            this.BuildGenericMethod(
                adapterContext,
                builderContext,
                methodIL);

            return methodBuilder;
        }

        /// <summary>
        /// Builds a generic method.
        /// </summary>
        /// <param name="context">The current <see cref="AdapterContext"/.></param>
        /// <param name="builderContext">The current <see cref="BuilderContext"/>.</param>
        /// <param name="methodIL">The methods <see cref="ILGenerator"/>.</param>
        private void BuildGenericMethod(
            AdapterContext context,
            BuilderContext builderContext,
            ILGenerator methodIL)
        {
            MethodInfo methodInfo = builderContext.Method;
            MethodInfo proxiedMethod = context.BaseType.GetSimilarMethod(methodInfo);
            if (proxiedMethod == null)
            {
                // Throw NotImplementedException
                methodIL.ThrowException<NotImplementedException>("No matching adaptable method.");
                return;
            }

            LocalBuilder methodReturn = null;
            if (methodInfo.ReturnType != typeof(void))
            {
                methodIL.DeclareLocal<object>();
            }

            LocalBuilder methodReturnTypeAttrs = methodIL.DeclareLocal<object>();
            LocalBuilder adapterAttribute = methodIL.DeclareLocal<AdapterAttribute>();
            LocalBuilder methodReturnType = methodIL.DeclareLocal<Type>();
            LocalBuilder adaptedTypeName = methodIL.DeclareLocal<string>();
            LocalBuilder adaptedType = methodIL.DeclareLocal<Type>();
            LocalBuilder localTypesParam = methodIL.DeclareLocal<Type[]>();
            LocalBuilder localProxiedMethod = methodIL.DeclareLocal<MethodInfo>();
            LocalBuilder objectArray = methodIL.DeclareLocal<object[]>();
            LocalBuilder localIsGeneric = methodIL.DeclareLocal<bool>();

            Label labelEnd = methodIL.DefineLabel();
            Label labelAttributeFound = methodIL.DefineLabel();
            Label labelAttributeNotFound = methodIL.DefineLabel();

            MethodInfo getMethodImpl = typeof(ReflectionExtensions).GetMethodWithParameters("GetMethod", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(Type), typeof(int) });
            MethodInfo makeGenericMethodImpl = typeof(MethodInfo).GetMethod("MakeGenericMethod", new Type[] { typeof(Type[]) });
            MethodInfo invokeMethod = typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) });

            methodIL.EmitTypeOf(builderContext.GenericArguments[0]);
            methodIL.Emit(OpCodes.Stloc_S, methodReturnType);

                // Get custom attributes
            methodIL.EmitGetCustomAttributes<AdapterAttribute>(methodReturnType, true);
            methodIL.Emit(OpCodes.Stloc_S, methodReturnTypeAttrs);

            methodIL.Emit(OpCodes.Ldloc_S, methodReturnTypeAttrs);
            methodIL.Emit(OpCodes.Ldlen);
            methodIL.Emit(OpCodes.Conv_I4);
            methodIL.Emit(OpCodes.Ldc_I4_0);
            methodIL.Emit(OpCodes.Bgt_S, labelAttributeFound);

            methodIL.MarkLabel(labelAttributeNotFound);
            methodIL.ThrowException<AdapterGenerationException>("Unable to determine the adapter type");

            methodIL.MarkLabel(labelAttributeFound);

            methodIL.Emit(OpCodes.Ldloc_S, methodReturnTypeAttrs);
            methodIL.Emit(OpCodes.Ldc_I4_0);
            methodIL.Emit(OpCodes.Ldelem, methodReturnTypeAttrs.LocalType);
            methodIL.Emit(OpCodes.Castclass, typeof(AdapterAttribute));
            methodIL.Emit(OpCodes.Stloc_S, adapterAttribute);

            methodIL.EmitGetProperty("AdaptedTypeName", adapterAttribute);

            methodIL.Emit(OpCodes.Stloc_S, adaptedTypeName);

            // Null check
            methodIL.Emit(OpCodes.Ldloc_S, adaptedTypeName);
            methodIL.Emit(OpCodes.Brfalse_S, labelAttributeNotFound);

            // Get type.
            methodIL.EmitGetType(adaptedTypeName);
            methodIL.Emit(OpCodes.Stloc_S, adaptedType);

            // Create an array of types.
            methodIL.EmitTypeArray(localTypesParam, adaptedType);

            // Gets the proxied method info.
            methodIL.EmitTypeOf(context.BaseType);
            methodIL.Emit(OpCodes.Ldc_I4, proxiedMethod.MetadataToken);
            methodIL.Emit(OpCodes.Call, getMethodImpl);
            methodIL.Emit(OpCodes.Ldloc_S, localTypesParam);
            methodIL.Emit(OpCodes.Callvirt, makeGenericMethodImpl);
            methodIL.Emit(OpCodes.Stloc_S, localProxiedMethod);

            /*
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, context.BaseObjectField);
            methodIL.EmitToString();
            methodIL.EmitWriteLine();

            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.EmitWriteLine();

            methodIL.Emit(OpCodes.Ldarg_2);
            methodIL.EmitWriteLine();

            methodIL.Emit(OpCodes.Ldarg, 4);
            methodIL.Emit(OpCodes.Box, methodArgs[3]);
            methodIL.EmitToString();
            methodIL.EmitWriteLine();

            methodIL.EmitGetProperty("MetadataToken", localMethod);
            methodIL.Emit(OpCodes.Box, typeof(int));
            methodIL.EmitToString();
            methodIL.EmitWriteLine();

            methodIL.EmitGetProperty("IsGenericMethod", localMethod);
            methodIL.Emit(OpCodes.Stloc_S, localIsGeneric);
            methodIL.EmitStringFormat("IsGenericMethod: {0}", localIsGeneric);
            methodIL.EmitWriteLine();
            */

            // Build an arguments array.
            if (builderContext.Parameters.Length > 0)
            {
                // Build the arguments array.
                methodIL
                    .EmitArray(
                        typeof(object),
                        objectArray,
                        builderContext.Parameters.Length,
                        (index) =>
                        {
                            methodIL.Emit(OpCodes.Ldarg, index);
                            methodIL.EmitConv(builderContext.Parameters[index].ParameterType, typeof(object), false);
                        });
            }
            else
            {
                methodIL.Emit(OpCodes.Ldnull);
                methodIL.Emit(OpCodes.Stloc_S, objectArray);
            }

            // Invoke the proxied method.
            methodIL.Emit(OpCodes.Ldloc_S, localProxiedMethod);
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, context.BaseObjectField);
            methodIL.Emit(OpCodes.Ldloc_S, objectArray);
            methodIL.Emit(OpCodes.Callvirt, invokeMethod);

            if (methodReturn != null)
            {
                methodIL.Emit(OpCodes.Stloc_S, methodReturn);
            }
            else
            {
                methodIL.Emit(OpCodes.Pop);
            }

            /*
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, context.BaseObjectField);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldarg_2);
            methodIL.Emit(OpCodes.Ldnull);
            methodIL.Emit(OpCodes.Ldarg, 4);
            methodIL.Emit(OpCodes.Conv_I4);
            methodIL.Emit(OpCodes.Callvirt, localMethod);

            this.EmitParameters(methodIL, methodInfo, proxiedMethod, context);
            methodIL.Emit(OpCodes.Callvirt, localMethod);

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, context.BaseObjectField);
            this.EmitParameters(methodIL, methodInfo, proxiedMethod, context);
            //MethodInfo callMethod = context.BaseObjectField.FieldType.GetMethod(memberInfo.Name, genericArguments);
            methodIL.Emit(OpCodes.Callvirt, proxiedMethod);
            */

            methodIL.MarkLabel(labelEnd);

            if (methodReturn != null)
            {
                /*
                methodIL.Emit(OpCodes.Stloc_S, methodReturn);
                methodIL.Emit(OpCodes.Ldloc_S, methodReturn);
                methodIL.Emit(OpCodes.Castclass, methodReturnType);
                */

                methodIL.Emit(OpCodes.Ldloc_S, methodReturn);
                //methodIL.Emit(OpCodes.Castclass, methodReturnType);
            }
            else
            {
            }

            //methodIL.Emit(OpCodes.Ldnull);
            methodIL.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Builds a method
        /// </summary>
        /// <param name="adapterContext">The current <see cref="AdapterContext"/.></param>
        /// <param name="builderContext">The current <see cref="BuilderContext"/>.</param>
        private MethodBuilder BuildMethod(
            AdapterContext adapterContext,
            BuilderContext builderContext)
        {
            MethodAttributes attrs = builderContext.Method.Attributes & ~MethodAttributes.Abstract;
            var methodBuilder = adapterContext
                .TypeBuilder
                .DefineMethod(
                    builderContext.Method.Name,
                    attrs | MethodAttributes.Virtual,
                    builderContext.MethodReturnType,
                    builderContext.ParameterTypes);

            for (int i = 0; i < builderContext.Parameters.Length; i++)
            {
                methodBuilder.DefineParameter(
                    i + 1,
                    builderContext.Parameters[i].Attributes,
                    builderContext.Parameters[i].Name);
            }

            var methodIL = methodBuilder.GetILGenerator();

            bool implemented = false;
            IServiceProvider scope = adapterContext.ServiceProvider;
            if (scope != null)
            {
                foreach (var extension in scope.GetServices<IAdapterFactoryExtension>())
                {
                    if (extension.ImplementMethod(builderContext.Method, methodIL, adapterContext) == true)
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
                    builderContext,
                    methodIL);
            }

            return methodBuilder;
        }

        /// <summary>
        /// Implements a method.
        /// </summary>
        /// <param name="adapterContext">The adapter factories current context.</param>
        /// <param name="builderContext">The <see cref="MethodInfo"/> of the method being implemented.</param>
        /// <param name="ilGen">The methods <see cref="ILGenerator"/>.</param>
        private void BuildMethod(
            AdapterContext adapterContext,
            BuilderContext builderContext,
            ILGenerator ilGen)
        {
            // Get the method being proxied.
            if (builderContext.TargetStaticType == null)
            {
                builderContext.SetProxiedMethod(
                    adapterContext.BaseType,
                    builderContext.TargetMemberName,
                    BindingFlags.Public | BindingFlags.Instance,
                    builderContext.Parameters);
            }
            else
            {
                builderContext.SetProxiedMethod(
                    builderContext.TargetStaticType,
                    builderContext.TargetMemberName,
                    BindingFlags.Public | BindingFlags.Static,
                    builderContext.Parameters);
            }

            // Was a proxy method found?
            if (builderContext.ProxiedMethod == null)
            {
                this.EmitMethodNotFoundProcessing(
                    adapterContext,
                    builderContext,
                    ilGen);

                return;
            }

            Type proxiedReturnType = builderContext.ProxiedMethodReturnType;

            LocalBuilder methodReturnLocal = null;
            LocalBuilder proxiedReturnLocal = null;

            // Does the method have a return type?
            if (builderContext.MethodReturnType != null &&
                builderContext.MethodReturnType != typeof(void))
            {
                // Check if the return value has an adapter attribute applied to it.
                AdapterAttribute attr = builderContext.MethodReturnType.GetCustomAttribute<AdapterAttribute>();
                if (attr != null)
                {
                    proxiedReturnType = attr.GetAdaptedType();
                }

                // Declare locals
                methodReturnLocal = ilGen.DeclareLocal(builderContext.MethodReturnType);
                proxiedReturnLocal = ilGen.DeclareLocal(proxiedReturnType);

                // hmm
                // If this is a generic method changes it to a generic method of return type.
                // This is NOT robust enough needs to only change the signature if the proxied return
                // type is actual required in the generic definition.
                if (builderContext.ProxiedMethod.IsGenericMethodDefinition == true)
                {
                    builderContext.MakeGenericProxiedMethod(proxiedReturnType);
                }
            }

            // Is the proxied method static?
            if (builderContext.ProxiedMethod.IsStatic == true)
            {
                // Is it an extension method?
                if (builderContext.ProxiedMethod.IsDefined(typeof(ExtensionAttribute), false) == true)
                {
                    ParameterInfo[] extensionParms = builderContext.ProxiedParameters;
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
                        ilGen.EmitParameters(builderContext);
                        ilGen.Emit(OpCodes.Call, builderContext.ProxiedMethod);
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
                    ilGen.EmitParameters(builderContext);
                    ilGen.Emit(OpCodes.Call, builderContext.ProxiedMethod);
                }
            }
            else
            {
                // Is the adapted type a class?
                if (adapterContext.BaseType.IsClass == true)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                    ilGen.EmitParameters(builderContext);
                    ilGen.Emit(OpCodes.Callvirt, builderContext.ProxiedMethod);
                }

                // Is the adapted type a value type?
                else if (adapterContext.BaseType.IsValueType == true)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldflda, adapterContext.BaseObjectField);
                    ilGen.EmitParameters(builderContext);
                    ilGen.Emit(OpCodes.Call, builderContext.ProxiedMethod);
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
            builderContext.EmitOutParameters(ilGen);

            // Does the method expect a return value?
            if (methodReturnLocal != null)
            {
                // Emit return value handling.
                this.EmitReturnValueProcessing(
                    builderContext,
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
        /// <param name="builderContext">The current <see cref="BuilderContext"/>.</param>
        /// <param name="ilGen">The current <see cref="ILGenerator"/>.</param>
        /// <param name="proxiedReturnLocal">A <see cref="LocalBuilder"/> for the proxy return value.</param>
        /// <param name="methodReturnLocal">A <see cref="LocalBuilder"/> for the return value.</param>
        private void EmitReturnValueProcessing(
            BuilderContext builderContext,
            ILGenerator ilGen,
            LocalBuilder proxiedReturnLocal,
            LocalBuilder methodReturnLocal)
        {
            Type proxiedReturnType = proxiedReturnLocal.LocalType;
            Type returnType = methodReturnLocal.LocalType;

            // Does the method have an adapter extension method applied?
            if (builderContext.ExtensionMethodName != null)
            {
                // Emit the code to call the extension method.
                ilGen.EmitAdapterExtensionExecution(
                    builderContext.ExtensionMethodName,
                    proxiedReturnType,
                    proxiedReturnType,
                    builderContext.AdapterContext,
                    proxiedReturnLocal,
                    proxiedReturnLocal);
            }

            // Do the return types match?
            if (returnType != proxiedReturnType)
            {
                ilGen.EmitAdaptedResult(
                    builderContext.AdapterContext,
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
        /// <param name="builderContext">The current <see cref="BuilderContext"/>.</param>
        /// <param name="ilGen">The current <see cref="ILGenerator"/>.</param>
        private void EmitMethodNotFoundProcessing(
            AdapterContext adapterContext,
            BuilderContext builderContext,
            ILGenerator ilGen)
        {
            // No method found
            MethodInfo methodInfo = builderContext.Method;
            FieldInfo field = null;

            // Is the desired method is a property getter and
            // is there a public field with same name?
            if (methodInfo.IsPropertyGet() == true &&
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

                if (builderContext.ExtensionMethodName != null)
                {
                    ilGen.EmitAdapterExtensionExecution(
                            builderContext.ExtensionMethodName,
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
                (field = adapterContext.BaseType.GetField(methodInfo.Name.Substring(4))) != null)
            {
                // TODO: Look at adpated types
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);

                ilGen.Emit(OpCodes.Ldarg_1);
                ilGen.Emit(OpCodes.Stfld, field);
                ilGen.Emit(OpCodes.Ret);
            }

            // Does the desired method have a return type and does it
            // have an adapter extension method applied?
            else if (builderContext.ExtensionMethodName != null &&
                methodInfo.ReturnType != typeof(void))
            {
                LocalBuilder sourceValue = ilGen.DeclareLocal(adapterContext.BaseType);
                LocalBuilder returnValue = ilGen.DeclareLocal(methodInfo.ReturnType);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, adapterContext.BaseObjectField);
                ilGen.Emit(OpCodes.Stloc_S, sourceValue);

                ilGen.EmitAdapterExtensionExecution(
                        builderContext.ExtensionMethodName,
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
                Console.WriteLine("{0} - Not implemented", methodInfo.Name);
                // Unable to implement the desired method.
                ilGen.ThrowException(typeof(NotImplementedException));
            }
        }
    }
}
