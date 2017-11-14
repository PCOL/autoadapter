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
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterTypeGenerator"/> class.
        /// </summary>
        public AdapterTypeGenerator()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterTypeGenerator"/> class.
        /// </summary>
        /// <param name="serviceProvider">A servicve provider.</param>
        public AdapterTypeGenerator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the name of an adapter type.
        /// </summary>
        /// <param name="adaptedType">The type being adpated.</param>
        /// <param name="adapterTypes">The adapter types.</param>
        /// <returns>The type name.</returns>
        public static string TypeName(Type adaptedType, Type[] adapterTypes)
        {
            return string.Format("Dynamic.Adapters.{0}_{1}", adaptedType.Name, adapterTypes.Join("_", (t) => { return t.Name; }));
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="typeToAdapt">The type to adapt.</param>
        /// <returns>A new adapter type.</returns>
        public Type CreateAdapterType<T>(Type typeToAdapt)
        {
            return this.CreateAdapterType<T>(typeToAdapt, this.serviceProvider);
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="typeToAdapt">The type to adapt.</param>
        /// <param name="serviceProvider">The dependency injection scope</param>
        /// <returns>A new adapter type.</returns>
        public Type CreateAdapterType<T>(Type typeToAdapt, IServiceProvider serviceProvider)
        {
            return this.CreateAdapterType(typeToAdapt, new Type[] { typeof(T) }, serviceProvider);
        }

        /// <summary>
        /// Create instance of an adapter.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="instance">The instance of the object to adapt.</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        public T CreateAdapter<T>(object instance)
        {
            return this.CreateAdapter<T>(instance, this.serviceProvider);
        }

        /// <summary>
        /// Create instance of an adapter.
        /// </summary>
        /// <typeparam name="T">The interface describing the adapter type.</typeparam>
        /// <param name="instance">The instance of the object to adapt.</param>
        /// <param name="serviceProvider">The dependency injection scope to use.</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        public T CreateAdapter<T>(object instance, IServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                return default(T);
            }

            return (T)this.CreateAdapterInternal(instance, new Type[] { typeof(T) }, serviceProvider);
        }

        /// <summary>
        /// Creates an adapter object to represent the desired types.
        /// </summary>
        /// <param name="inst">The instance of the object to adapt.</param>
        /// <param name="types">The interface types to implement on the adapter type.</param>
        /// <param name="serviceProvider">The dependency injection scope to use.</param>
        /// <returns>An instance of the adapter if valid; otherwise null.</returns>
        private object CreateAdapterInternal(object inst, Type[] types, IServiceProvider serviceProvider)
        {
            Type adapterType = this.CreateAdapterType(inst.GetType(), types, serviceProvider);
            return Activator.CreateInstance(adapterType, inst, serviceProvider);
        }

        /// <summary>
        /// Creates an adapter type.
        /// </summary>
        /// <param name="adaptedType">The type to adapt.</param>
        /// <param name="adapterTypes">The interface types to implement on the adapter type.</param>
        /// <param name="serviceProvider">The dependency injection scope to use.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        private Type CreateAdapterType(Type adaptedType, Type[] adapterTypes, IServiceProvider serviceProvider)
        {
            string typeName = TypeName(adaptedType, adapterTypes);
            Type adapterType = TypeFactory.Default.GetType(typeName, true);
            if (adapterType == null)
            {
                adapterType = this.GenerateAdapterType(adapterTypes, adaptedType, serviceProvider);
            }

            return adapterType;
        }

        /// <summary>
        /// Generates the adapter type.
        /// </summary>
        /// <param name="adapterTypes">The adapters interface types to implement.</param>
        /// <param name="adaptedType">The type being adapted.</param>
        /// <param name="serviceProvider">The dependency injection scope to use.</param>
        /// <returns>A <see cref="Type"/> representing the new adapter.</returns>
        private Type GenerateAdapterType(Type[] adapterTypes, Type adaptedType, IServiceProvider serviceProvider)
        {
            TypeAttributes typeAttributes = TypeAttributes.Class | TypeAttributes.Public;

            TypeBuilder typeBuilder =
                TypeFactory
                .Default
                .ModuleBuilder
                .DefineType(
                    TypeName(adaptedType, adapterTypes),
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

            typeBuilder.AddInterfaceImplementation(typeof(IAdaptedObject));

            // Implement the IAdaptedObject interface.
            this.ImplementAdaptedObjectInterface(
                typeBuilder,
                adaptedType,
                adaptedTypeField);

            foreach (Type type in adapterTypes)
            {
                typeBuilder.AddInterfaceImplementation(type);

                Type[] implementedInterfaces = type.GetInterfaces();
                if (implementedInterfaces != null)
                {
                    foreach (Type iface in implementedInterfaces)
                    {
                        typeBuilder.AddInterfaceImplementation(iface);
                    }
                }
            }

            // Add a constructor to the type.
            var ctorBuilder =  this.AddConstructor(
                typeBuilder,
                adaptedType,
                adaptedTypeField,
                dependencyResolverField);

            // Implement interfaces.
            AdapterContext context = new AdapterContext(typeBuilder, null, adaptedType, serviceProvider, adaptedTypeField, dependencyResolverField, ctorBuilder);
            foreach (Type adapterType in adapterTypes)
            {
                context = context.CreateTypeFactoryContext(adapterType);
                this.ImplementInterfaces(context);
            }

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
        /// Implements the <see cref="IAdaptedObject"/> interface on the adapter type.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> use to construct the type.</param>
        /// <param name="adaptedType">The <see cref="Type"/> being adapted.</param>
        /// <param name="adaptedTypeField">The <see cref="FieldBuilder"/> which will hold the instance of the adapted type.</param>
        private void ImplementAdaptedObjectInterface(
            TypeBuilder typeBuilder,
            Type adaptedType,
            FieldBuilder adaptedTypeField)
        {
            var propertyAdaptedObject =
                typeBuilder
                .DefineProperty(
                    "AdaptedObject",
                    PropertyAttributes.None,
                    CallingConventions.HasThis,
                    typeof(object),
                    null);

            MethodBuilder getAdaptedObject =
                typeBuilder
                .DefineMethod(
                    "get_AdaptedObject",
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot,
                    CallingConventions.HasThis,
                    typeof(object),
                    null);

            var methodIL = getAdaptedObject.GetILGenerator();

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, adaptedTypeField);
            methodIL.Emit(OpCodes.Ret);

            propertyAdaptedObject.SetGetMethod(getAdaptedObject);
        }

        /// <summary>
        /// Implements the adapter types interfaces on the adapted type.
        /// </summary>
        /// <param name="context">The current adapter context.</param>
        private void ImplementInterfaces(AdapterContext context)
        {
            Type[] implementedInterfaces = context.NewType.GetInterfaces();
            if (implementedInterfaces != null)
            {
                foreach (Type iface in implementedInterfaces)
                {
                    AdapterContext ifaceContext = context.CreateTypeFactoryContext(iface);
                    this.ImplementInterfaces(ifaceContext);
                }
            }

            Dictionary<string, MethodBuilder> propertyMethods = new Dictionary<string, MethodBuilder>();

            foreach (var memberInfo in context.NewType.GetMembers())
            {
                if (memberInfo.MemberType == MemberTypes.Method)
                {
                    MethodInfo methodInfo = (MethodInfo)memberInfo;
                    MethodBuilder methodBuilder = null;

                    if (methodInfo.ContainsGenericParameters == true)
                    {
                        methodBuilder = this.BuildGenericMethod(context, methodInfo);
                    }
                    else
                    {
                        methodBuilder = this.BuildMethod(context, methodInfo);
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
                        context,
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
            AdapterContext context,
            PropertyInfo propertyInfo,
            IDictionary<string, MethodBuilder> propertyMethods)
        {
            this.GetPropertyTargetDetails(propertyInfo, context);

            // Builde the property.
            var propertyBuilder = context
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
        /// Gets the target name for a property.
        /// </summary>
        /// <param name="propertyInfo">A <see cref="PropertyInfo"/> instace.</param>
        /// <param name="targetType">A variable to receive the target type.</param>
        /// <param name="targetStaticType">A variable to receive the target static type.</param>
        /// <param name="adapterExtensionMethodName">A variable to receive an adapter extension method name.</param>
        /// <returns>The target name.</returns>
        private void GetPropertyTargetDetails(PropertyInfo propertyInfo, AdapterContext context)
        {
            context.TargetMemberName = propertyInfo.Name;

            // Check for a property extension attribute.
            AdapterExtensionAttribute adapterAttr = propertyInfo.GetCustomAttribute<AdapterExtensionAttribute>();
            if (adapterAttr != null)
            {
                context.ExtensionMethodName = adapterAttr.ExtensionName;
            }

            TargetMemberType targetMemberType;
            Type targetStaticType;
            Type targetType;
            string attrTargetName = this.GetMemberTargetName(
                propertyInfo.GetCustomAttribute<AdapterImplAttribute>(),
                out targetStaticType,
                out targetMemberType,
                out targetType);

            if (targetMemberType == TargetMemberType.Property ||
                targetMemberType == TargetMemberType.NotSet)
            {
                context.TargetMemberName =
                    attrTargetName.IsNullOrEmpty() == false ?
                    propertyInfo.Name + attrTargetName :
                    context.TargetMemberName;
            }
            else if (targetMemberType == TargetMemberType.Method)
            {
                context.TargetMemberName =
                    attrTargetName.IsNullOrEmpty() == false ?
                    attrTargetName :
                    context.TargetMemberName;
            }

            context.TargetType = targetType;
            context.TargetStaticType = targetStaticType;
        }

        /// <summary>
        /// Gets the target name for a method.
        /// </summary>
        /// <param name="methodInfo">A <see cref="MethodInfo"/> instace.</param>
        /// <param name="context">The adapter context.</param>
        private void GetMethodTargetDetails(MethodInfo methodInfo, AdapterContext context)
        {
            MemberInfo memberInfo = methodInfo;
            if (methodInfo.IsProperty() == true)
            {
                memberInfo = methodInfo.GetProperty();
            }

            context.TargetMemberName = methodInfo.Name;

            // Check for a return type extension attribute.
            object[] adapterAttrs = methodInfo
                .ReturnTypeCustomAttributes
                .GetCustomAttributes(typeof(AdapterExtensionAttribute), false);

            if (adapterAttrs != null &&
                adapterAttrs.Any() == true)
            {
                context.ExtensionMethodName = ((AdapterExtensionAttribute)adapterAttrs.First()).ExtensionName;
            }

            TargetMemberType targetMemberType;
            Type targetStaticType;
            Type targetType;
            string attrTargetName = this.GetMemberTargetName(
                memberInfo.GetCustomAttribute<AdapterImplAttribute>(),
                out targetStaticType,
                out targetMemberType,
                out targetType);

            if (targetMemberType == TargetMemberType.Property)
            {
                context.TargetMemberName =
                    attrTargetName.IsNullOrEmpty() == false ?
                    methodInfo.Name.Substring(0, 4) + attrTargetName :
                    context.TargetMemberName;
            }
            else if (targetMemberType == TargetMemberType.Method ||
                targetMemberType == TargetMemberType.NotSet)
            {
                context.TargetMemberName =
                    attrTargetName.IsNullOrEmpty() == false ?
                    attrTargetName :
                    context.TargetMemberName;
            }

            context.TargetType = targetType;
            context.TargetStaticType = targetStaticType;
        }

        /// <summary>
        /// Builds a generic method.
        /// </summary>
        /// <param name="context">The current <see cref="AdapterContext"/.></param>
        /// <param name="methodInfo">The current <see cref="MethodInfo"/>.</param>
        private MethodBuilder BuildGenericMethod(AdapterContext context, MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            Type[] methodArgs = parameters.Select(p => p.ParameterType).ToArray();
            Type[] genericArguments = methodInfo.GetGenericArguments();

            MethodBuilder methodBuilder =
                context.TypeBuilder
                .DefineMethod(
                    methodInfo.Name,
                    MethodAttributes.Public |
                    MethodAttributes.Virtual,
                    methodInfo.ReturnType,
                    methodArgs);

            var genTypeBuilders = methodBuilder.DefineGenericParameters(
                genericArguments.Select(t => t.Name).ToArray());

            for (int i = 0; i < genericArguments.Length; i++)
            {
                genTypeBuilders[i].SetGenericParameterAttributes(
                    genericArguments[i]
                        .GenericParameterAttributes
                );
            }

            ILGenerator methodIL = methodBuilder.GetILGenerator();

            this.BuildGenericMethod(
                context,
                methodInfo,
                methodIL,
                genericArguments,
                parameters);

            return methodBuilder;
        }

        /// <summary>
        /// Builds a generic method.
        /// </summary>
        /// <param name="context">The current <see cref="AdapterContext"/.></param>
        /// <param name="methodInfo">The current <see cref="MethodInfo"/>.</param>
        /// <param name="methodIL">The methods <see cref="ILGenerator"/>.</param>
        /// <param name="genericArguments">The methods generic arguments.</param>
        /// <param name="parameters">The methods parameters.</param>
        private void BuildGenericMethod(
            AdapterContext context,
            MethodInfo methodInfo,
            ILGenerator methodIL,
            Type[] genericArguments,
            ParameterInfo[] parameters)
        {
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

            methodIL.EmitTypeOf(genericArguments[0]);
            methodIL.Emit(OpCodes.Stloc_S, methodReturnType);
            methodIL.EmitWriteLine("Return Type:");
            methodIL.EmitWriteLine(methodReturnType);

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

            //methodIL.EmitLoadArrayElem(methodReturnTypeAttrs, 0);
            methodIL.Emit(OpCodes.Ldloc_S, methodReturnTypeAttrs);
            methodIL.Emit(OpCodes.Ldc_I4_0);
            methodIL.Emit(OpCodes.Ldelem, methodReturnTypeAttrs.LocalType);

                //.EmitLoadArrayElement(methodReturnTypeAttrs, 0)

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

            methodIL.EmitWriteLine(adaptedType);

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
            if (parameters.Length > 0)
            {
                // Build the arguments array.
                methodIL
                    .EmitArray(
                        typeof(object),
                        objectArray,
                        parameters.Length,
                        (index) =>
                        {
                            methodIL.Emit(OpCodes.Ldarg, index);
                            methodIL.EmitConv(parameters[index].ParameterType, typeof(object), false);
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
        /// <param name="context">The current <see cref="AdapterContext"/.></param>
        /// <param name="methodInfo">The current <see cref="MethodInfo"/>.</param>
        private MethodBuilder BuildMethod(AdapterContext context, MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            Type[] methodArgs = parameters.Select(p => p.ParameterType).ToArray();

            MethodAttributes attrs = methodInfo.Attributes & ~MethodAttributes.Abstract;
            var methodBuilder = context
                .TypeBuilder
                .DefineMethod(
                    methodInfo.Name,
                    attrs | MethodAttributes.Virtual,
                    methodInfo.ReturnType,
                    methodArgs);

            var methodIL = methodBuilder.GetILGenerator();

            bool implemented = false;
            IServiceProvider scope = context.ServiceProvider;
            if (scope != null)
            {
                foreach (var extension in scope.GetServices<IAdapterFactoryExtension>())
                {
                    if (extension.ImplementMethod(methodInfo, methodIL, context) == true)
                    {
                        implemented = true;
                        break;
                    }
                }
            }

            if (implemented == false)
            {
                this.GetMethodTargetDetails(methodInfo, context);

                this.BuildMethod(
                    context,
                    methodInfo,
                    methodIL,
                    methodArgs);
            }

            return methodBuilder;
        }

        /// <summary>
        /// Implements a method.
        /// </summary>
        /// <param name="context">The adapter factories current context.</param>
        /// <param name="methodInfo">The <see cref="MethodInfo"/> of the method being implemented.</param>
        /// <param name="ilGen">The methods <see cref="ILGenerator"/>.</param>
        /// <param name="methodArgs">An array containing the methods argument types.</param>
        private void BuildMethod(AdapterContext context, MethodInfo methodInfo, ILGenerator ilGen, Type[] methodArgs)
        {
            // Get the method being proxied.
            MethodInfo proxiedMethod = null;
            if (context.TargetStaticType == null)
            {
                proxiedMethod = context.BaseType.GetMethodWithParameters(
                    context.TargetMemberName,
                    BindingFlags.Public | BindingFlags.Instance,
                    methodInfo.GetParameters());
            }
            else
            {
                proxiedMethod = context.TargetStaticType.GetMethodWithParameters(
                    context.TargetMemberName,
                    BindingFlags.Public | BindingFlags.Static,
                    methodInfo.GetParameters());
            }

            // Was a proxy method found?
            if (proxiedMethod == null)
            {
                this.EmitMethodNotFoundProcessing(context, methodInfo, ilGen);
                return;
            }

            Type proxiedReturnType = proxiedMethod.ReturnType;

            LocalBuilder methodReturn = null;
            LocalBuilder proxiedReturn = null;

            // Does the method have a return type?
            if (methodInfo.ReturnType != null &&
                methodInfo.ReturnType != typeof(void))
            {
                // Check if the return value has an adapter attribute applied to it.
                AdapterAttribute attr = methodInfo.ReturnType.GetCustomAttribute<AdapterAttribute>();
                if (attr != null)
                {
                    proxiedReturnType = this.GetAdaptedType(attr);
                }

                // Declare locals
                methodReturn = ilGen.DeclareLocal(methodInfo.ReturnType);
                proxiedReturn = ilGen.DeclareLocal(proxiedReturnType);

                // hmm
                // If this is a generic method changes it to a generic method of return type.
                // This is NOT robust enough needs to only change the signature if the proxied return
                // type is actual required in the generic definition.
                if (proxiedMethod.IsGenericMethodDefinition == true)
                {
                    proxiedMethod = proxiedMethod.MakeGenericMethod(proxiedReturnType);
                }
            }

            // Is the proxied method static?
            if (proxiedMethod.IsStatic == true)
            {
                // Is it an extension method?
                if (proxiedMethod.IsDefined(typeof(ExtensionAttribute), false) == true)
                {
                    ParameterInfo[] extensionParms = proxiedMethod.GetParameters();
                    if (extensionParms.Length > 0 &&
                        context.BaseType == extensionParms[0].ParameterType)
                    {
                        ParameterInfo[] proxyParms = new ParameterInfo[extensionParms.Length - 1];
                        if (proxyParms.Length > 0)
                        {
                            Array.Copy(extensionParms, 1, proxyParms, 0, proxyParms.Length);
                        }

                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);
                        ilGen.EmitParameters(methodInfo.GetParameters(), proxyParms, context);
                        ilGen.Emit(OpCodes.Call, proxiedMethod);
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
                    ilGen.EmitParameters(methodInfo, proxiedMethod, context);
                    ilGen.Emit(OpCodes.Call, proxiedMethod);
                }
            }
            else
            {
                // Is the adapted type a class?
                if (context.BaseType.IsClass == true)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);
                    ilGen.EmitParameters(methodInfo, proxiedMethod, context);
                    ilGen.Emit(OpCodes.Callvirt, proxiedMethod);
                }

                // Is the adapted type a value type?
                else if (context.BaseType.IsValueType == true)
                {
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldflda, context.BaseObjectField);
                    ilGen.EmitParameters(methodInfo, proxiedMethod, context);
                    ilGen.Emit(OpCodes.Call, proxiedMethod);
                }
            }

            // Does the method expect a return value?
            if (methodReturn != null)
            {
                // Store the value on the evaluation stack
                // into a local variable.
                ilGen.Emit(OpCodes.Stloc, proxiedReturn);

                // Emit return value handling.
                this.EmitReturnValueProcessing(
                    context,
                    methodInfo,
                    ilGen,
                    proxiedReturnType,
                    proxiedReturn,
                    methodReturn);

                // Load the return value back onto the evaluation stack.
                ilGen.Emit(OpCodes.Ldloc_S, methodReturn);
            }

            // Return from the method.
            ilGen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Handles methods the return a value.
        /// </summary>
        /// <param name="context">The current <see cref="AdapterContext"/>.</param>
        /// <param name="methodInfo">The current <see cref="MethodInfo"/>.</param>
        /// <param name="ilGen">The current <see cref="ILGenerator"/>.</param>
        /// <param name="proxiedReturnType">The proxy return type</param>
        /// <param name="proxiedReturn">A <see cref="LocalBuilder"/> for the proxy return value.</param>
        private void EmitReturnValueProcessing(
            AdapterContext context,
            MethodInfo methodInfo,
            ILGenerator ilGen,
            Type proxiedReturnType,
            LocalBuilder proxiedReturn,
            LocalBuilder methodReturn)
        {
            // Does the method have an adapter extension method applied?
            if (context.ExtensionMethodName != null)
            {
                // Emit the code to call the extension method.
                ilGen.EmitAdapterExtensionExecution(
                    context.ExtensionMethodName,
                    proxiedReturnType,
                    proxiedReturnType,
                    context,
                    proxiedReturn,
                    proxiedReturn);
            }

            ilGen.Emit(OpCodes.Ldloc, proxiedReturn);

            // Do the return types match?
            if (methodInfo.ReturnType != proxiedReturnType)
            {
                // Is the return type an enum?
                if (methodInfo.ReturnType.IsEnum == true)
                {
                    ilGen.Emit(OpCodes.Conv_I4);
                }

                // Is the return type an array.
                else if (methodInfo.ReturnType.IsArray == true)
                {
                    if (methodInfo.ReturnType.GetElementType().IsInterface == false)
                    {
                        throw new AdapterGenerationException("Returns types do not match and cannot be adapted");
                    }

                    Type returnType = methodInfo.ReturnType.GetElementType();
                    Type proxiedType = proxiedReturnType.GetElementType();

                    ConstructorInfo adapterCtor = null;
                    if (context.DoesTypeBuilderImplementInterface(returnType) == true)
                    {
                        adapterCtor = context.ConstructorBuilder;
                    }
                    else
                    {
                        Type adapterType = this.CreateAdapterType(
                            proxiedType,
                            new Type[] { returnType },
                            context.ServiceProvider);

                        adapterCtor = adapterType.GetConstructor(new Type[] { proxiedType, typeof(IServiceProvider) });
                    }

                    Label labelStart = ilGen.DefineLabel();
                    Label loopStart = ilGen.DefineLabel();
                    Label loopCheck = ilGen.DefineLabel();
                    Label labelEnd = ilGen.DefineLabel();

                    LocalBuilder returnArray = ilGen.DeclareLocal(methodInfo.ReturnType);
                    LocalBuilder index = ilGen.DeclareLocal<int>();
                    LocalBuilder done = ilGen.DeclareLocal<bool>();

                    // If null then exit...
                    ilGen.Emit(OpCodes.Brtrue_S, labelStart);
                    ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Br_S, labelEnd);

                    ilGen.MarkLabel(labelStart);
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldloc_S, proxiedReturn);

                    ilGen.Emit(OpCodes.Ldlen);
                    ilGen.Emit(OpCodes.Conv_I4);
                    ilGen.Emit(OpCodes.Newarr, returnType);
                    ilGen.Emit(OpCodes.Stloc_S, returnArray);

                    ilGen.Emit(OpCodes.Ldc_I4_0);
                    ilGen.Emit(OpCodes.Stloc_S, index);
                    ilGen.Emit(OpCodes.Br_S, loopCheck);

                    ilGen.MarkLabel(loopStart);
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldloc_S, returnArray);
                    ilGen.Emit(OpCodes.Ldloc_S, index);

                    ilGen.Emit(OpCodes.Ldloc_S, proxiedReturn);
                    ilGen.Emit(OpCodes.Ldloc_S, index);
                    ilGen.Emit(OpCodes.Ldelem_Ref);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, context.ServiceProviderField);
                    ilGen.Emit(OpCodes.Newobj, adapterCtor);
                    ilGen.Emit(OpCodes.Stelem_Ref);
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldloc_S, index);
                    ilGen.Emit(OpCodes.Ldc_I4_1);
                    ilGen.Emit(OpCodes.Add);
                    ilGen.Emit(OpCodes.Stloc_S, index);

                    ilGen.MarkLabel(loopCheck);
                    ilGen.Emit(OpCodes.Ldloc_S, index);
                    ilGen.Emit(OpCodes.Ldloc_S, returnArray);
                    ilGen.Emit(OpCodes.Ldlen);
                    ilGen.Emit(OpCodes.Conv_I4);
                    ilGen.Emit(OpCodes.Clt);
                    ilGen.Emit(OpCodes.Stloc_S, done);

                    ilGen.Emit(OpCodes.Ldloc_S, done);
                    ilGen.Emit(OpCodes.Brtrue_S, loopStart);
                    ilGen.Emit(OpCodes.Ldloc_S, returnArray);
                    ilGen.MarkLabel(labelEnd);
                }

                // Is the return type an interface?
                else if (methodInfo.ReturnType.IsInterface == true)
                {
                    if (proxiedReturnType.IsGenericParameter == true)
                    {
                        throw new AdapterGenerationException("Unable to determine the type to adapt.");
                    }

                    // If there is a target type set then use that instead of the proxied type.
                    context.TargetType = context.TargetType ?? proxiedReturnType;

                    ConstructorInfo adapterCtor = null;
                    if (context.DoesTypeBuilderImplementInterface(methodInfo.ReturnType) == true)
                    {
                        adapterCtor = context.ConstructorBuilder;
                    }
                    else
                    {
                        // We need to create a new adapted object.
                        Type adapterType = this.CreateAdapterType(
                            context.TargetType,
                            new Type[] { methodInfo.ReturnType },
                            context.ServiceProvider);

                        adapterCtor = adapterType.GetConstructor(new Type[] { context.TargetType, typeof(IServiceProvider) });
                    }

                    Label notNull = ilGen.DefineLabel();
                    Label done = ilGen.DefineLabel();

                    // Check if the adapter type has been created.
                    ilGen.Emit(OpCodes.Brtrue_S, notNull);
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Br_S, done);

                    // Push the arguments onto the evaluation stack.
                    ilGen.MarkLabel(notNull);
                    ilGen.Emit(OpCodes.Ldloc_S, proxiedReturn);
                    ilGen.Emit(OpCodes.Ldarg_0);
                    ilGen.Emit(OpCodes.Ldfld, context.ServiceProviderField);

                    // Construct an instance of the adapter.
                    ilGen.Emit(OpCodes.Newobj, adapterCtor);
                    ilGen.MarkLabel(done);
                }
                else
                {
                    throw new AdapterGenerationException("The return types do not match or cannot be adapted");
                }
            }

            // Store the value on the top of the execution stack into the return value variable.
            ilGen.Emit(OpCodes.Stloc_S, methodReturn);
        }

        /// <summary>
        /// Handles various scenarios when a proxiabe method is not found.
        /// </summary>
        /// <param name="context">The current <see cref="AdapterContext"/>.</param>
        /// <param name="methodInfo">The current <see cref="MethodInfo"/>.</param>
        /// <param name="ilGen">The current <see cref="ILGenerator"/>.</param>
        private void EmitMethodNotFoundProcessing(AdapterContext context, MethodInfo methodInfo, ILGenerator ilGen)
        {
            // No method found

            FieldInfo field = null;

            // Is the desired method is a property getter and
            // is there a public field with same name?
            if (methodInfo.IsPropertyGet() == true &&
                (field = context.BaseType.GetField(methodInfo.Name.Substring(4))) != null)
            {
                LocalBuilder sourceValue = ilGen.DeclareLocal(context.BaseType);
                LocalBuilder fieldValue = ilGen.DeclareLocal(field.FieldType);
                LocalBuilder returnValue = ilGen.DeclareLocal(methodInfo.ReturnType);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);
                ilGen.Emit(OpCodes.Stloc_S, sourceValue);

                ilGen.Emit(OpCodes.Ldloc_S, sourceValue);
                ilGen.Emit(OpCodes.Ldfld, field);
                ilGen.Emit(OpCodes.Stloc_S, fieldValue);

                if (context.ExtensionMethodName != null)
                {
                    ilGen.EmitAdapterExtensionExecution(
                            context.ExtensionMethodName,
                            context.BaseType,
                            methodInfo.ReturnType,
                            context,
                            fieldValue,
                            returnValue);

                    ilGen.Emit(OpCodes.Ldloc_S, returnValue);
                    ilGen.Emit(OpCodes.Ret);
                }
                else if (field.FieldType != methodInfo.ReturnType)
                {
                    ilGen.Emit(OpCodes.Ldloc_S, fieldValue);
                    ilGen.Emit(OpCodes.Castclass, methodInfo.ReturnType);
                    ilGen.Emit(OpCodes.Ldloc_S, returnValue);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldloc_S, fieldValue);
                }

                ilGen.Emit(OpCodes.Ret);
            }

            // Is the desired method is a property setter and
            // is there a public field with same name?
            else if (methodInfo.IsPropertySet() == true &&
                (field = context.BaseType.GetField(methodInfo.Name.Substring(4))) != null)
            {
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);

                ilGen.Emit(OpCodes.Ldarg_1);
                ilGen.Emit(OpCodes.Stfld, field);
                ilGen.Emit(OpCodes.Ret);
            }

            // Does the desired method have a return type and does it
            // have an adapter extension method applied?
            else if (context.ExtensionMethodName != null &&
                methodInfo.ReturnType != typeof(void))
            {
                LocalBuilder sourceValue = ilGen.DeclareLocal(context.BaseType);
                LocalBuilder returnValue = ilGen.DeclareLocal(methodInfo.ReturnType);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);
                ilGen.Emit(OpCodes.Stloc_S, sourceValue);

                ilGen.EmitAdapterExtensionExecution(
                        context.ExtensionMethodName,
                        context.BaseType,
                        methodInfo.ReturnType,
                        context,
                        sourceValue,
                        returnValue);

                ilGen.Emit(OpCodes.Ldloc_S, returnValue);
                ilGen.Emit(OpCodes.Ret);
            }
            else
            {
                Console.WriteLine("Method: {0} - Not Implemented", methodInfo.Name);

                // Unable to implement the desired method.
                ilGen.ThrowException(typeof(NotImplementedException));
            }
        }

        /// <summary>
        /// Gets the target member name from an <see cref="AdapterImplAttribute"/> instance
        /// </summary>
        /// <param name="implAttr">An <see cref="AdapterImplAttribute"/> instance.</param>
        /// <param name="targetStaticType">A variable to receive the target static type.</param>
        /// <param name="targetMemberType">A variable to receive the target member type.</param>
        /// <param name="targetType">A variable to receive the target type.</param>
        /// <returns>The target members name.</returns>
        private string GetMemberTargetName(
            AdapterImplAttribute implAttr,
            out Type targetStaticType,
            out TargetMemberType targetMemberType,
            out Type targetType)
        {
            targetStaticType = null;
            targetMemberType = TargetMemberType.NotSet;
            targetType = null;

            if (implAttr != null)
            {
                var type = implAttr.TargetType;
                if (type == null &&
                    implAttr.TargetTypeName.IsNullOrEmpty() == false)
                {
                    type = TypeFactory.Default.GetType(implAttr.TargetTypeName, false);
                }

                if (implAttr.TargetBinding == AdapterBinding.Static)
                {
                    targetStaticType = type;
                }
                else
                {
                    targetType = type;
                }

                targetMemberType = implAttr.TargetMemberType;
                return implAttr.TargetMemberName;
            }

            return null;
        }

        /// <summary>
        /// Gets the adapted type from a <see cref="AdapterAttribute"/> instance.
        /// </summary>
        /// <param name="attr">The <see cref="AdpaterAttribute"/> instance.</param>
        /// <returns>The adapted type if found; otherwise null.</returns>
        private Type GetAdaptedType(AdapterAttribute attr)
        {
            if (attr != null)
            {
                Type adaptedType = attr.AdaptedType;
                if (adaptedType == null &&
                    attr.AdaptedTypeName.IsNullOrEmpty() == false)
                {
                    adaptedType = TypeFactory.Default.GetType(attr.AdaptedTypeName, false);
                }

                return adaptedType;
            }

            return null;
        }
    }
}
