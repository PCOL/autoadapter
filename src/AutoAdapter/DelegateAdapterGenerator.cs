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
    /// Delegate adapter generator.
    /// </summary>
    internal class DelegateAdapterGenerator
    {
        private AdapterContext adapterContext;

        /// <summary>
        /// <summary>
        /// Initialises a new instance of the <see cref="DelegateAdapterGenerator"/> class.
        /// </summary>
        /// <param name="adapterContext">The adapter context.</param>
        public DelegateAdapterGenerator(AdapterContext adapterContext)
        {
            this.adapterContext = adapterContext;
        }

        /// <summary>
        /// Makes the type name
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="adaptedType"></param>
        /// <returns></returns>
        protected string MakeTypeName(Type sourceType, Type adaptedType)
        {
            return $"{sourceType}_{adaptedType}";
        }

        /// <summary>
        /// Copy types to a list of arguments and a return type.
        /// </summary>
        /// <param name="fromArray">The source array.</param>
        /// <param name="toArray">A variable to receive the argument types.</param>
        /// <returns>The return type</returns>
        protected Type CopyToArgumentsAndReturnType(Type[] fromArray, out Type[] toArray)
        {
            int len = fromArray.Length - 1;
            toArray = new Type[len];
            Array.Copy(fromArray, 0, toArray, 0, len);
            return fromArray[len];
        }

        /// <summary>
        /// Generates a delegate Type.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="adaptedType"></param>
        /// <returns></returns>
        public Type GenerateDelegateType(
            Type sourceType,
            Type adaptedType)
        {
            var sourceInvokeMethod = sourceType.GetMethod("Invoke");
            var sourceTypeArgs = sourceInvokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var sourceReturnType = sourceInvokeMethod.ReturnType;

            var adaptedInvokeMethod = adaptedType.GetMethod("Invoke");
            var adaptedTypeArgs = adaptedInvokeMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var adaptedReturnType = adaptedInvokeMethod.ReturnType;

            return this.GenerateType(
                sourceType,
                sourceTypeArgs,
                sourceReturnType,
                adaptedType,
                adaptedTypeArgs,
                adaptedReturnType);
        }

        /// <summary>
        /// Generates the adapter type.
        /// </summary>
        /// <param name="sourceType">The source delegate type</param>
        /// <param name="sourceTypeArgs">The source delegate argument types</param>
        /// <param name="sourceReturnType">The source return type</param>
        /// <param name="adaptedType">The source delegate type</param>
        /// <param name="adaptedTypeArgs">The adapted delegates argument types.</param>
        /// <param name="adaptedReturnType">The adapted return type</param>
        /// <returns>The generated type.</returns>
        protected Type GenerateType(
            Type sourceType,
            Type[] sourceTypeArgs,
            Type sourceReturnType,
            Type adaptedType,
            Type[] adaptedTypeArgs,
            Type adaptedReturnType)
        {
            string delegateName = MakeTypeName(sourceType, adaptedType);

            Type delegateType = AssemblyCache.GetType(
                delegateName,
                true);

            if (delegateType != null)
            {
                return delegateType;
            }

            var typeBuilder =
                TypeFactory
                .Default
                .ModuleBuilder
                .DefineType(
                    delegateName,
                    TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

            var sourceFieldBuilder = typeBuilder
                .DefineField(
                    "source",
                    sourceType,
                    FieldAttributes.Private);

            this.EmitConstructor(typeBuilder, sourceFieldBuilder);

            var internalMethodBuilder = this.EmitInternalMethod(
                typeBuilder,
                sourceType,
                sourceTypeArgs,
                sourceReturnType,
                adaptedType,
                adaptedTypeArgs,
                adaptedReturnType,
                sourceFieldBuilder);

            this.EmitMethod(
                typeBuilder,
                sourceType,
                adaptedType,
                internalMethodBuilder);

            return typeBuilder.CreateTypeInfo().AsType();
        }

        /// <summary>
        /// Emits the constructor.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> instance.</param>
        /// <param name="sourceField">The <see cref="FieldBuilder"/> that holds the source.</param>
        private void EmitConstructor(
            TypeBuilder typeBuilder,
            FieldBuilder sourceField)
        {
            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.HasThis,
                new[]
                {
                    sourceField.FieldType
                });

            var ilGen = ctorBuilder.GetILGenerator();

            ilGen.EmitCallBaseCtor();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, sourceField);
            ilGen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits the internal method.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> instance.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="sourceTypeArgs">The source types arguments.</param>
        /// <param name="sourceReturnType">The source return type.</param>
        /// <param name="adapterType">The adapter type.</param>
        /// <param name="adapterTypeArgs">The adapter types arguments.</param>
        /// <param name="adapterReturnType">The adapter return type.</param>
        /// <param name="sourceField">The <see cref="FieldBuilder"/> that holds the source.</param>
        /// <returns>A <see cref="MethodBuilder"/> that represents the internal method.</returns>
        private MethodBuilder EmitInternalMethod(
            TypeBuilder typeBuilder,
            Type sourceType,
            Type[] sourceTypeArgs,
            Type sourceReturnType,
            Type adaptedType,
            Type[] adaptedTypeArgs,
            Type adaptedReturnType,
            FieldBuilder sourceField)
        {
            var methodBuilder = typeBuilder
                .DefineMethod(
                    "SourceInternal",
                    MethodAttributes.Private,
                    CallingConventions.HasThis,
                    adaptedReturnType ?? typeof(void),
                    sourceTypeArgs);

            var createAdapter = typeof(AdapterExtensionMethods)
                .GetMethod(
                    "CreateAdapter",
                    new[] {
                        typeof(object),
                        typeof(Type),
                        typeof(IServiceProvider)
                    });

            var ilGen = methodBuilder.GetILGenerator();

            int length = sourceTypeArgs.Length;

            LocalBuilder returnLocal = null;
            LocalBuilder adaptedReturnLocal = null;
            if (sourceReturnType != typeof(void))
            {
                returnLocal = ilGen.DeclareLocal(sourceReturnType);
                adaptedReturnLocal = ilGen.DeclareLocal(adaptedReturnType);
            }

            LocalBuilder[] parmLocals = new LocalBuilder[length];

            ilGen.Emit(OpCodes.Nop);
            for (int i = 0; i < length; i++)
            {
                parmLocals[i] = ilGen.DeclareLocal(sourceTypeArgs[i]);

                ilGen.Emit(OpCodes.Ldarg, i + 1);

                ilGen.Emit(OpCodes.Box, adaptedTypeArgs[i]);
                ilGen.EmitTypeOf(sourceTypeArgs[i]);
                ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Call, createAdapter);
                ilGen.Emit(OpCodes.Unbox_Any, sourceTypeArgs[i]);

                ilGen.Emit(OpCodes.Stloc, parmLocals[i]);
            }

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, sourceField);
            for (int i = 0; i < parmLocals.Length; i++)
            {
                ilGen.Emit(OpCodes.Ldloc, parmLocals[i]);
            }

            ilGen.Emit(OpCodes.Callvirt, sourceType.GetMethod("Invoke"));

            if (returnLocal != null)
            {
                var adapted = ilGen.DefineLabel();
                var isAssignable = ilGen.DeclareLocal<bool>();

                ilGen.Emit(OpCodes.Stloc, adaptedReturnLocal);

                // Do the return types match?
                if (sourceReturnType != adaptedReturnType)
                {
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldloc, adaptedReturnLocal);
                    ilGen.Emit(OpCodes.Box, adaptedReturnType);

                    ilGen.EmitIsAssignableFrom<IAdaptedObject>(adaptedReturnLocal);
                    ilGen.Emit(OpCodes.Stloc, isAssignable);

                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldloc, isAssignable);
                    ilGen.Emit(OpCodes.Brtrue, adapted);
                    ilGen.ThrowException(typeof(AdapterGenerationException));

                    ilGen.MarkLabel(adapted);
                    ilGen.Emit(OpCodes.Callvirt, typeof(IAdaptedObject).GetProperty("AdaptedObject").GetGetMethod());
                    ilGen.Emit(OpCodes.Stloc, returnLocal);
                    ilGen.Emit(OpCodes.Nop);
                    ilGen.Emit(OpCodes.Ldloc, returnLocal);
                }
                else
                {
                    ilGen.Emit(OpCodes.Ldloc, adaptedReturnLocal);
                }
            }

            ilGen.Emit(OpCodes.Nop);
            ilGen.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        /// <summary>
        /// Emit the method.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> instance.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="adaptedType">The adapted type.</param>
        /// <param name="sourceMethod">The internal method.</param>
        private void EmitMethod(
            TypeBuilder typeBuilder,
            Type sourceType,
            Type adaptedType,
            MethodInfo sourceMethod)
        {
            var methodBuilder = typeBuilder
                .DefineMethod(
                    "Adapted",
                    MethodAttributes.Public,
                    CallingConventions.HasThis,
                    sourceType,
                    Type.EmptyTypes);

            var ctor = adaptedType.GetConstructor(new[] { typeof(object), typeof(IntPtr) });

            var ilGen = methodBuilder.GetILGenerator();

            var sourceLocal = ilGen.DeclareLocal(sourceType);

            ilGen.Emit(OpCodes.Nop);
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldftn, sourceMethod);
            ilGen.Emit(OpCodes.Newobj, ctor);
            ilGen.Emit(OpCodes.Stloc, sourceLocal);
            ilGen.Emit(OpCodes.Ldloc, sourceLocal);
            ilGen.Emit(OpCodes.Ret);
        }
    }
}
