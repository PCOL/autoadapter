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
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using AutoAdapter.Extensions;
    using AutoAdapter.Reflection;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Adapter extension methods.
    /// </summary>
    public static class AdapterExtensionMethods
    {
        /// <summary>
        /// Checks if an object is adapted.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if it is adapted; otherwise false.</returns>
        public static bool IsAdaptedObject(this object obj)
        {
            return obj != null &&
                typeof(IAdaptedObject).IsAssignableFrom(obj.GetType());
        }

        /// <summary>
        /// Creates an adapter.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="instance">The instance being adapted</param>
        /// <returns>An instance of the adapter type or null if the instance to be adapted is null.</returns>
        public static T CreateAdapter<T>(this object instance)
        {
            if (instance == null)
            {
                return default(T);
            }

            return instance.CreateAdapter<T>(Services.ServiceProvider);
        }

        /// <summary>
        /// Creates an adapter.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="instance">The instance being adapted</param>
        /// <param name="serviceProvider">Optional service provider instance.</param>
        /// <returns>An instance of the adapter type or null if the instance to be adapted is null.</returns>
        public static T CreateAdapter<T>(this object instance, IServiceProvider serviceProvider)
        {
            if (instance == null)
            {
                return default(T);
            }

            var adapterTypeGenerator = serviceProvider?.GetService<IAdapterTypeGenerator>();
            if (adapterTypeGenerator == null)
            {
                adapterTypeGenerator = new AdapterTypeGenerator();
            }

            return adapterTypeGenerator.CreateAdapter<T>(instance, serviceProvider);
        }

        /// <summary>
        /// Creates a adapter type.
        /// </summary>
        /// <typeparam name="T">The adapter type.</typeparam>
        /// <param name="type">The type being adapted</param>
        /// <returns>An instance of the adapter type.</returns>
        public static Type CreateAdapterType<T>(this Type type)
        {
            return type.CreateAdapterType(typeof(T));
        }

        /// <summary>
        /// Creates a adapter type.
        /// </summary>
        /// <param name="type">The type being adapted</param>
        /// <param name="adpaterType">The adapter type.</param>
        /// <returns>An instance of the adapter type.</returns>
        public static Type CreateAdapterType(this Type type, Type adapterType)
        {
            return type.CreateAdapterType(adapterType, Services.ServiceProvider);
        }

        public static Type CreateAdapterType(this Type type, Type adapterType, IServiceProvider serviceProvider)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (adapterType == null)
            {
                throw new ArgumentNullException(nameof(adapterType));
            }

            var adapterTypeGenerator = serviceProvider?.GetService<IAdapterTypeGenerator>();
            if (adapterTypeGenerator == null)
            {
                adapterTypeGenerator = new AdapterTypeGenerator();
            }

            return adapterTypeGenerator.CreateAdapterType(type, adapterType, serviceProvider);
        }

        /// <summary>
        /// Gets an adapter extension.
        /// </summary>
        /// <param name="extensionName">The name of the extension.</param>
        /// <returns>An <see cref="IAdapterExtension{T, TResult}"/> if found; otherwise null.</returns>
        internal static IAdapterExtension<T, TResult> GetAdapterExtension<T, TResult>(this IServiceProvider serviceProvider, string extensionName)
        {
            var adapters = serviceProvider.GetServices<IAdapterExtension<T, TResult>>();
            if (adapters != null)
            {
                return adapters.FirstOrDefault(a => a.Name == extensionName);
            }

            return default(IAdapterExtension<T, TResult>);
        }

        /// <summary>
        /// Adds the adapter interface to a <see cref="TypeBuilder"/>.
        /// </summary>
        /// <param name="typeBuilder">A <see cref="TypeBuilder"/> instance.</param>
        /// <param name="adapterType">The adapter type.</param>
        /// <returns>A <see cref="TypeBuilder"/> instance.</returns>
        internal static TypeBuilder AddAdapterInterface(this TypeBuilder typeBuilder, Type adapterType)
        {
            typeBuilder.AddInterfaceImplementation(adapterType);

            Type[] implementedInterfaces = adapterType.GetInterfaces();
            if (implementedInterfaces != null)
            {
                foreach (Type iface in implementedInterfaces)
                {
                    typeBuilder.AddInterfaceImplementation(iface);
                }
            }

            return typeBuilder;
        }

        /// <summary>
        /// Implements the <see cref="IAdaptedObject"/> interface on the adapter type.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> use to construct the type.</param>
        /// <param name="adaptedType">The <see cref="Type"/> being adapted.</param>
        /// <param name="adaptedTypeField">The <see cref="FieldBuilder"/> which will hold the instance of the adapted type.</param>
        internal static TypeBuilder ImplementAdaptedObjectInterface(
            this TypeBuilder typeBuilder,
            Type adaptedType,
            FieldBuilder adaptedTypeField)
        {
            typeBuilder.AddInterfaceImplementation(typeof(IAdaptedObject));

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

            return typeBuilder;
        }

        /// <summary>
        /// Adds the target property details to the <see cref="AdapterContext"/>.
        /// </summary>
        /// <param name="propertyInfo">A <see cref="PropertyInfo"/> instance.</param>
        /// <param name="context">The <see cref="AdapterContext"/> to update.</param>
        internal static void AddTargetPropertyDetailsToContext(this PropertyInfo propertyInfo, AdapterContext context)
        {
            context.TargetMemberName = propertyInfo.Name;

            // Check for a property extension attribute.
            AdapterExtensionAttribute adapterAttr = propertyInfo.GetCustomAttribute<AdapterExtensionAttribute>();
            if (adapterAttr != null)
            {
                context.ExtensionMethodName = adapterAttr.ExtensionName;
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
        }

        /// <summary>
        /// Adds the target methods details to the <see cref="AdapterContext"/>.
        /// </summary>
        /// <param name="methodInfo">A <see cref="MethodInfo"/> instance.</param>
        /// <param name="context">The <see cref="AdapterContext"/> to update.</param>
        internal static void AddTargetMethodDetailsToContext(this MethodInfo methodInfo, AdapterContext context)
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

            var adapterImpl = memberInfo.GetCustomAttribute<AdapterImplAttribute>();
            if (adapterImpl != null)
            {
                string attrTargetName =adapterImpl.GetMemberTargetName(
                    out Type targetStaticType,
                    out TargetMemberType targetMemberType,
                    out Type targetType);

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
        }
    }
}
