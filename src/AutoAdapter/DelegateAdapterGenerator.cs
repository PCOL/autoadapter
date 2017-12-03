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
    /// Action adapter generator.
    /// </summary>
    internal class DelegateAdapterGenerator
    {
        private string typeName;

        public DelegateAdapterGenerator()
            : this("DelegateAdapter")
        {
        }

        protected DelegateAdapterGenerator(string typeName)
        {
            this.typeName = typeName;
        }

        /// <summary>
        /// Makes the delegate type name
        /// </summary>
        /// <param name="sourceTypes"></param>
        /// <param name="adaptedTypes"></param>
        /// <returns></returns>
        protected string MakeTypeName(Type[] sourceTypes, Type[] adaptedTypes)
        {
            return string.Format("{0}_{1}_{2}",
                this.typeName,
                string.Join(",", sourceTypes.Select(t => t.Name)),
                string.Join(",", adaptedTypes.Select(t => t.Name)));
        }

        /// <summary>
        /// Generates the action adapter type.
        /// </summary>
        /// <param name="sourceTypes">The source actions types</param>
        /// <param name="adaptedTypes"></param>
        /// <returns></returns>
        public virtual Type GenerateType(
            Type[] sourceTypes,
            Type[] adaptedTypes)
        {
            return this.GenerateType(
                typeof(Delegate),
                sourceTypes,
                typeof(Delegate),
                adaptedTypes);
        }

        /// <summary>
        /// Generates the action adapter type.
        /// </summary>
        /// <param name="sourceTypes">The source actions types</param>
        /// <param name="adaptedTypes"></param>
        /// <returns></returns>
        protected Type GenerateType(
            Type sourceType,
            Type[] sourceTypes,
            Type adaptedType,
            Type[] adaptedTypes)
        {
            string delegateName = MakeTypeName(sourceTypes, adaptedTypes);

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

            var actionFieldBuilder = typeBuilder
                .DefineField(
                    "source",
                    sourceType,
                    FieldAttributes.Private);

            this.EmitConstructor(typeBuilder, actionFieldBuilder);

            var internalMethodBuilder =this.EmitInternalMethod(
                typeBuilder,
                sourceType,
                sourceTypes,
                actionFieldBuilder);

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
        /// Emits the internal action method.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> instance.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="sourceTypeArgs">The source types arguments.</param>
        /// <param name="actionField">The <see cref="FieldBuilder"/> that holds the action.</param>
        /// <returns>A <see cref="MethodBuilder"/> that represents the internal action.</returns>
        private MethodBuilder EmitInternalMethod(
            TypeBuilder typeBuilder,
            Type sourceType,
            Type[] sourceTypeArgs,
            FieldBuilder actionField)
        {
            var methodBuilder = typeBuilder
                .DefineMethod(
                    "SourceInternal",
                    MethodAttributes.Private,
                    CallingConventions.HasThis,
                    typeof(void),
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

            ilGen.Emit(OpCodes.Nop);

            LocalBuilder[] parmLocals = new LocalBuilder[sourceTypeArgs.Length];

            for (int i = 0; i < sourceTypeArgs.Length; i++)
            {
                parmLocals[i] = ilGen.DeclareLocal(sourceTypeArgs[i]);

                ilGen.Emit(OpCodes.Ldarg, i + 1);
                ilGen.Emit(OpCodes.Box, sourceTypeArgs[i]);
                ilGen.EmitTypeOf(sourceTypeArgs[i]);
                ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Call, createAdapter);
                ilGen.Emit(OpCodes.Stloc, parmLocals[i]);
            }

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, actionField);
            for (int i = 0; i < parmLocals.Length; i++)
            {
                ilGen.Emit(OpCodes.Ldloc, parmLocals[i]);
            }

            ilGen.Emit(OpCodes.Callvirt, sourceType.GetMethod("Invoke"));
            ilGen.Emit(OpCodes.Nop);
            ilGen.Emit(OpCodes.Ret);

            return methodBuilder;
        }

        /// <summary>
        /// Emit the action method.
        /// </summary>
        /// <param name="typeBuilder">The <see cref="TypeBuilder"/> instance.</param>
        /// <param name="sourceType">The action type.</param>
        /// <param name="adaptedType">The adapted action type.</param>
        /// <param name="sourceMethod">The internal action method.</param>
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
