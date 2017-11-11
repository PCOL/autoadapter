namespace AutoAdapter.Extensions
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using AutoAdapter.Reflection;

    /// <summary>
    /// An <see cref="AdapterTypeGenerator"/> extension for generating asynchronous methods for Asynchronous Programming Model (APM) method pairs.
    /// </summary>
    /// <remarks>
    /// Generated methods must follow the Async method pattern (must return <see cref="Task"/> or <see cref="Task{T}"/> and the name must end in the word "Async").
    /// The proxied type must contain a matching method pair implemented to the Asynchronous Programming Model standard (A Begin... method which returns an <see cref="IAsyncResult"/>
    /// and optionally takes an <see cref="AsyncCallback"/> parameter and an <see cref="object"/> state parameter, along with an End... method which takes an <see cref="IAsyncResult"/>
    /// and returns an appropriate return value).
    /// </remarks>
    public class APMToTaskAdapterExtension
        : IAdapterFactoryExtension
    {
        /// <summary>
        /// Implements a method.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/> for the method being implemented.</param>
        /// <param name="ilGen">The <see cref="ILGenerator"/> for the method.</param>
        /// <param name="context">The current <see cref="TypeFactoryContext"/> object.</param>
        /// <returns>True if the method was implemented; otherwise false.</returns>
        public bool ImplementMethod(MethodInfo methodInfo, ILGenerator ilGen, IAdapterContext context)
        {
            if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType) == true &&
                methodInfo.Name.EndsWith("Async") == true)
            {
                ParameterInfo[] parameters = methodInfo.GetParameters();
                Type[] parameterTypes = new Type[parameters.Length + 2];
                int i = 0;
                for (; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].ParameterType;
                }

                parameterTypes[i++] = typeof(AsyncCallback);
                parameterTypes[i++] = typeof(object);

                MethodInfo proxiedBeginMethod = this.GetAsyncMethod(
                    context.BaseType,
                    $"Begin{methodInfo.Name}",
                    BindingFlags.Public | BindingFlags.Instance,
                    parameterTypes);

                if (proxiedBeginMethod == null)
                {
                    return false;
                }

                MethodInfo proxiedEndMethod = this.GetAsyncMethod(
                    context.BaseType,
                    $"Begin{methodInfo.Name}",
                    BindingFlags.Public | BindingFlags.Instance,
                    new Type[] { typeof(IAsyncResult) });

                if (proxiedEndMethod == null)
                {
                    return false;
                }

                if (parameters.Length > 3)
                {
                    throw new AdapterGenerationException("Too many parameters for async pattern matching method.");
                }

                Type[] argTypes;
                Type beginMethodFuncType;
                Type endMethodFuncType;
                MethodInfo taskFactoryMethod;
                Type taskFactoryType = this.GetReturnTypeArgumentTypesAndMethodTypes(
                    methodInfo,
                    parameters,
                    out argTypes,
                    out beginMethodFuncType,
                    out endMethodFuncType,
                    out taskFactoryMethod);

                ConstructorInfo taskFactoryCtor = taskFactoryType.GetConstructor(Type.EmptyTypes);
                ConstructorInfo beginMethodFuncCtor = beginMethodFuncType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });
                ConstructorInfo endMethodFuncCtor = endMethodFuncType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });

                LocalBuilder factory = ilGen.DeclareLocal(taskFactoryType);
                LocalBuilder returnValue = ilGen.DeclareLocal(methodInfo.ReturnType);

                ilGen.Emit(OpCodes.Nop);
                ilGen.Emit(OpCodes.Newobj, taskFactoryCtor);
                ilGen.Emit(OpCodes.Stloc, factory);

                ilGen.Emit(OpCodes.Ldloc, factory);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);
                ilGen.Emit(OpCodes.Dup);
                ilGen.Emit(OpCodes.Ldvirtftn, proxiedBeginMethod);
                ilGen.Emit(OpCodes.Newobj, beginMethodFuncCtor);

                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);
                ilGen.Emit(OpCodes.Dup);
                ilGen.Emit(OpCodes.Ldvirtftn, proxiedEndMethod);
                ilGen.Emit(OpCodes.Newobj, endMethodFuncCtor);

                for (int a = 0; a < argTypes.Length; a++)
                {
                    ilGen.Emit(OpCodes.Ldarg, a + 1);
                }

                ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Callvirt, taskFactoryMethod);
                ilGen.Emit(OpCodes.Stloc, returnValue);

                ilGen.Emit(OpCodes.Ldloc, returnValue);
                ilGen.Emit(OpCodes.Ret);

                return true;
            }

            return false;
        }

        private Type GetReturnTypeArgumentTypesAndMethodTypes(MethodInfo methodInfo, ParameterInfo[] parameters, out Type[] argTypes, out Type beginMethodFuncType, out Type endMethodFuncType, out MethodInfo taskFactoryMethod)
        {
            Type returnType = methodInfo.ReturnType.GetGenericArguments()[0];
            argTypes = new Type[parameters.Length];
            for (int i = 0; i < argTypes.Length; i++)
            {
                argTypes[i] = parameters[i].ParameterType;
            }

/*
            Type taskFactoryType = typeof(TaskFactory<>).MakeGenericType(returnType);
            endMethodFuncType = typeof(Func<,>).MakeGenericType(typeof(IAsyncResult), returnType);

            if (argTypes.Length == 1)
            {
                beginMethodFuncType = typeof(Func<,,,>).MakeGenericType(argTypes[0], typeof(AsyncCallback), typeof(object), typeof(IAsyncResult));
                taskFactoryMethod = taskFactoryType.GetMethodWithParameters(
                    "FromAsync",
                    new Type[]
                    {
                        beginMethodFuncType,
                        endMethodFuncType,
                        argTypes[0],
                        typeof(object)
                    }).MakeGenericMethod(argTypes);
            }
            else if (argTypes.Length == 2)
            {
                beginMethodFuncType = typeof(Func<,,,,>).MakeGenericType(argTypes[0], argTypes[1], typeof(AsyncCallback), typeof(object), typeof(IAsyncResult));
                taskFactoryMethod = taskFactoryType.GetMethodWithParameters(
                    "FromAsync",
                    new Type[]
                    {
                        beginMethodFuncType,
                        endMethodFuncType,
                        argTypes[0],
                        argTypes[1],
                        typeof(object)
                    }).MakeGenericMethod(argTypes);
            }
            else if (argTypes.Length == 3)
            {
                beginMethodFuncType = typeof(Func<,,,,,>).MakeGenericType(argTypes[0], argTypes[1], argTypes[2], typeof(AsyncCallback), typeof(object), typeof(IAsyncResult));
                taskFactoryMethod = taskFactoryType.GetMethodWithParameters("FromAsync", new Type[] { beginMethodFuncType, endMethodFuncType, argTypes[0], argTypes[1], argTypes[2], typeof(object) }).MakeGenericMethod(argTypes);
            }
            else
            {
                throw new AdapterGenerationException("Too many generic args");
            }
*/
            if (argTypes.Length > 3)
            {
                throw new AdapterGenerationException("Too many generic args");
            }

            Type taskFactoryType;
            taskFactoryMethod = this.MakeTaskFactoryMethodAndBeginEndFuncTypes(
                methodInfo.ReturnType,
                out beginMethodFuncType,
                out endMethodFuncType,
                out taskFactoryType,
                argTypes);

            return taskFactoryType;
        }

        public MethodInfo MakeTaskFactoryMethodAndBeginEndFuncTypes(Type returnType, out Type beginFuncType, out Type endFuncType, out Type taskFactoryType, params Type[] argTypes)
        {
            taskFactoryType = typeof(TaskFactory<>).MakeGenericType(returnType);
            endFuncType = typeof(Func<,>).MakeGenericType(typeof(IAsyncResult), returnType);

            Type[] beginArgTypes = argTypes.CopyToAndAppendExtras(typeof(AsyncCallback), typeof(object), typeof(IAsyncResult));
            beginFuncType = Type.GetType($"System.Func`{argTypes.Length + 3}").MakeGenericType(beginArgTypes);

            Type[] factoryParameterTypes = new Type[argTypes.Length + 3];
            factoryParameterTypes[0] = beginFuncType;
            factoryParameterTypes[1] = endFuncType;
            factoryParameterTypes[factoryParameterTypes.Length - 1] = typeof(object);
            for (int i = 0; i < argTypes.Length; i++)
            {
                factoryParameterTypes[2 + i] = argTypes[i];
            }

            var taskFactoryMethod = taskFactoryType.GetMethodWithParameters(
                "FromAsync",
                factoryParameterTypes);

            if (taskFactoryMethod.IsGenericMethodDefinition == true)
            {
                return taskFactoryMethod.MakeGenericMethod(argTypes);
            }

            return taskFactoryMethod;
        }

        private MethodInfo GetAsyncMethod(Type proxiedType, string methodName, BindingFlags bindingFlags, Type[] parameterTypes)
        {
            MethodInfo proxiedMethod = proxiedType.GetMethodWithParameters(methodName, bindingFlags, parameterTypes);
            if (proxiedMethod == null &&
                methodName.EndsWith("Async") == true)
            {
                methodName = methodName.Remove(methodName.Length - 5, 5);
                proxiedMethod = proxiedType.GetMethodWithParameters(methodName, bindingFlags, parameterTypes);
            }

            return proxiedMethod;
        }
    }
}
