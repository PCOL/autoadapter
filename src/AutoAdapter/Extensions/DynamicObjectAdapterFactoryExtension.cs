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

namespace AutoAdapter.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using AutoAdapter.Reflection;

    /// <summary>
    /// An <see cref="AdapterTypeGenerator"/> extension for adapting dynamic objects.
    /// </summary>
    public class DynamicObjectAdapterFactoryExtension
        : IAdapterFactoryExtension
    {
        /// <summary>
        /// Implements a method.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/> for the method being implemented.</param>
        /// <param name="ilGen">The <see cref="ILGenerator"/> for the method.</param>
        /// <param name="context">The current <see cref="IAdapterContext"/> object.</param>
        /// <returns>True if the method was implemented; otherwise false.</returns>
        public bool ImplementMethod(MethodInfo methodInfo, ILGenerator ilGen, IAdapterContext context)
        {
            if (typeof(System.Dynamic.DynamicObject).IsAssignableFrom(context.BaseType) == true)
            {
                if (methodInfo.IsPropertyGet() == true)
                {
                    ConstructorInfo getMemberBinderCtor = typeof(ProxyGetMemberBinder).GetConstructor(new Type[] { typeof(string) });
                    ConstructorInfo getIndexBinderCtor = typeof(ProxyGetIndexBinder).GetConstructor(new Type[] { typeof(int), typeof(IEnumerable<string>) });
                    MethodInfo callMethod = context.BaseType.GetMethod("TryGetMember");
                    MethodInfo tryGetIndex = context.BaseType.GetMethod("TryGetIndex");

                    Label hasResult  = ilGen.DefineLabel();
                    Label end = ilGen.DefineLabel();
                    LocalBuilder getResult = ilGen.DeclareLocal<object>();
                    LocalBuilder callResult = ilGen.DeclareLocal<bool>();
                    LocalBuilder indexes = ilGen.DeclareLocal<object[]>();
                    LocalBuilder argNames = ilGen.DeclareLocal<string[]>();

                    var parms = methodInfo.GetParameters();
                    if (parms.Length > 0)
                    {
                        ilGen.EmitArray(
                            argNames,
                            parms.Length,
                            (i) =>
                            {
                                ilGen.Emit(OpCodes.Ldstr, parms[i].Name);
                            });

                        ilGen.EmitArray(
                            indexes,
                            parms.Length,
                            (i) =>
                            {
                                ilGen.Emit(OpCodes.Ldarg, i + 1);
                                ilGen.EmitBox(parms[i].ParameterType);
                            });

                        // Load the target onto the stack.
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);

                        // Load the argument count.
                        ilGen.Emit(OpCodes.Ldc_I4, parms.Length);
                        ilGen.Emit(OpCodes.Ldloc, argNames);

                        // New GetIndexBinder. Pushes the binder onto the stack.
                        ilGen.Emit(OpCodes.Newobj, getIndexBinderCtor);

                        // Load value onto stack.
                        ilGen.Emit(OpCodes.Ldloc, indexes);
                        ilGen.Emit(OpCodes.Ldloca_S, getResult);

                        // Call the 'TryGetIndex' method.
                        ilGen.Emit(OpCodes.Callvirt, tryGetIndex);
                        ilGen.Emit(OpCodes.Stloc, callResult);

                        ilGen.Emit(OpCodes.Ldloc, callResult);
                        ilGen.Emit(OpCodes.Brtrue_S, hasResult);

                        ilGen.Emit(OpCodes.Nop);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Ret);

                        ilGen.MarkLabel(hasResult);

                        ilGen.Emit(OpCodes.Ldloc, getResult);
                        ilGen.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        // load the target onto the stack.
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);

                        // Load the name of the property name.
                        ilGen.Emit(OpCodes.Ldstr, methodInfo.Name.Substring(4));

                        // New GetMemberBinder. Pushes the binder onto the stack.
                        ilGen.Emit(OpCodes.Newobj, getMemberBinderCtor);

                        // load getResult onto stack.
                        ilGen.Emit(OpCodes.Ldloca_S, getResult);

                        ilGen.Emit(OpCodes.Callvirt, callMethod);
                        ilGen.Emit(OpCodes.Stloc, callResult);

                        ilGen.Emit(OpCodes.Ldloc, callResult);
                        ilGen.Emit(OpCodes.Brtrue_S, hasResult);

                        ilGen.Emit(OpCodes.Nop);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Ret);

                        ilGen.MarkLabel(hasResult);

                        ilGen.Emit(OpCodes.Ldloc, getResult);
                        ilGen.Emit(OpCodes.Ret);
                    }

                    return true;
                }
                else if (methodInfo.IsPropertySet() == true)
                {
                    ConstructorInfo setMemberBinderCtor = typeof(ProxySetMemberBinder).GetConstructor(new Type[] { typeof(string) });
                    ConstructorInfo setIndexBinderCtor = typeof(ProxySetIndexBinder).GetConstructor(new Type[] { typeof(int), typeof(IEnumerable<string>) });
                    MethodInfo trySetMember = context.BaseType.GetMethod("TrySetMember");
                    MethodInfo trySetIndex = context.BaseType.GetMethod("TrySetIndex");

                    LocalBuilder callResult = ilGen.DeclareLocal<bool>();
                    LocalBuilder value = ilGen.DeclareLocal<object>();
                    LocalBuilder indexes = ilGen.DeclareLocal<object[]>();
                    LocalBuilder argNames = ilGen.DeclareLocal<string[]>();

                    var parms = methodInfo.GetParameters();
                    if (parms.Length > 1)
                    {
                        ilGen.Emit(OpCodes.Ldarg, parms.Length);

                        // Box any value types
                        ilGen.EmitBox(parms[parms.Length - 1].ParameterType);
                        ilGen.Emit(OpCodes.Stloc, value);

                        ilGen.EmitArray(
                            argNames,
                            parms.Length - 1,
                            (i) =>
                            {
                                ilGen.Emit(OpCodes.Ldstr, parms[i].Name);
                            });

                        ilGen.EmitArray(
                            indexes,
                            parms.Length - 1,
                            (i) =>
                            {
                                ilGen.Emit(OpCodes.Ldarg, i + 1);
                                ilGen.EmitBox(parms[i].ParameterType);
                            });

                        // Load the target onto the stack.
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);

                        // Load the argument count.
                        ilGen.Emit(OpCodes.Ldc_I4, parms.Length - 1);
                        ilGen.Emit(OpCodes.Ldloc, argNames);

                        // New SetIndexBinder. Pushes the binder onto the stack.
                        ilGen.Emit(OpCodes.Newobj, setIndexBinderCtor);

                        // Load value onto stack.
                        ilGen.Emit(OpCodes.Ldloc, indexes);
                        ilGen.Emit(OpCodes.Ldloc, value);

                        // Call the 'TrySetMember' method.
                        ilGen.Emit(OpCodes.Callvirt, trySetIndex);
                        ilGen.Emit(OpCodes.Stloc, callResult);
                        ilGen.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        // Load value
                        ilGen.Emit(OpCodes.Ldarg_1);

                        // Box any value types
                        ilGen.EmitBox(parms[0].ParameterType);

                        ilGen.Emit(OpCodes.Stloc, value);

                        // Load the target onto the stack.
                        ilGen.Emit(OpCodes.Ldarg_0);
                        ilGen.Emit(OpCodes.Ldfld, context.BaseObjectField);

                        // Load the name of the property name.
                        ilGen.Emit(OpCodes.Ldstr, methodInfo.Name.Substring(4));

                        // New SetMemberBinder. Pushes the binder onto the stack.
                        ilGen.Emit(OpCodes.Newobj, setMemberBinderCtor);

                        // Load value onto stack.
                        ilGen.Emit(OpCodes.Ldloc, value);

                        // Call the 'TrySetMember' method.
                        ilGen.Emit(OpCodes.Callvirt, trySetMember);
                        ilGen.Emit(OpCodes.Stloc, callResult);
                        ilGen.Emit(OpCodes.Ret);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Represents a binder for getting a member property.
        /// </summary>
        public class ProxyGetMemberBinder
            : GetMemberBinder
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ProxyGetMemberBinder"/> class.
            /// </summary>
            /// <param name="name">The member name.</param>
            public ProxyGetMemberBinder(string name)
                : base(name, false)
            {
            }

            /// <summary>
            /// Fallback get member.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <param name="errorSuggestion">The error suggestion.</param>
            /// <returns>A dynamic meta object.</returns>
            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Represents a binder for setting a member property.
        /// </summary>
        public class ProxySetMemberBinder
            : SetMemberBinder
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ProxySetMemberBinder"/> class.
            /// </summary>
            /// <param name="name">The member name.</param>
            public ProxySetMemberBinder(string name)
                : base(name, false)
            {
            }

            /// <summary>
            /// Fallback set member.
            /// </summary>
            /// <param name="target">The target.</param>
            /// <param name="value">The value.</param>
            /// <param name="errorSuggestion">The error suggestion.</param>
            /// <returns>A dynamic meta object.</returns>
            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Represents a binder for getting an index property.
        /// </summary>
        public class ProxyGetIndexBinder
            : GetIndexBinder
        {
            /// <summary>
            /// Initialises a new instance of the <see cref="ProxyGetIndexBinder"/> class.
            /// </summary>
            /// <param name="argCount">The argument count.</param>
            /// <param name="argNames">The argument names.</param>
            public ProxyGetIndexBinder(int argCount, IEnumerable<string> argNames)
                : base(new CallInfo(argCount, argNames))
            {
            }

            public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Represents a binder for setting an index property.
        /// </summary>
        public class ProxySetIndexBinder
            : SetIndexBinder
        {
            /// <summary>
            /// Initialises a new instance of the <see cref="ProxySetIndexBinder"/> class.
            /// </summary>
            /// <param name="argCount">The argument count.</param>
            /// <param name="argNames">The argument names.</param>
            public ProxySetIndexBinder(int argCount, IEnumerable<string> argNames)
                : base(new CallInfo(argCount, argNames))
            {
            }

            public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion) 
            {
                throw new NotImplementedException();
            }
        }
    }
}
