using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DevExpressMods
{
    public static class ILExtensions
    {
        public static void EmitTryFinally(this ILGenerator il, Action tryBody, Action finallyBody)
        {
            var block = il.BeginExceptionBlock();
            tryBody();
            il.Emit(OpCodes.Leave_S, block);
            il.BeginFinallyBlock();
            finallyBody();
            il.EndExceptionBlock();
        }

        public static void EmitUsing(this ILGenerator il, ushort local, Action usingBody)
        {
            EmitTryFinally(il, usingBody, () =>
            {
                il.EmitLdloc(local);
                il.EmitIf(() =>
                {
                    il.EmitLdloc(local);
                    il.Emit(OpCodes.Callvirt, typeof(IDisposable).GetMethod("Dispose"));
                });
            });
        }

        public static void EmitLock(this ILGenerator il, Action loadLockObj, Action lockBody)
        {
            var lockWasTaken = il.DeclareLocal(typeof(bool));
            var temp = il.DeclareLocal(typeof(object));

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_S, lockWasTaken);

            il.EmitTryFinally(() =>
            {
                loadLockObj();
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Stloc_S, temp);
                il.Emit(OpCodes.Ldloca_S, lockWasTaken);
                il.Emit(OpCodes.Call, typeof(Monitor).GetMethod("Enter", new[] { typeof(object), typeof(bool).MakeByRefType() } ));
                lockBody();   
            }, () =>
            {
                il.Emit(OpCodes.Ldloc_S, lockWasTaken);
                il.EmitIf(() =>
                {
                    il.Emit(OpCodes.Ldloc_S, temp);
                    il.Emit(OpCodes.Call, typeof(Monitor).GetMethod("Exit", new[] { typeof(object) }));
                });    
            });
        }

        public static void EmitIf(this ILGenerator il, Action trueBlock)
        {
            var endIf = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, endIf);
            trueBlock();
            il.MarkLabel(endIf);
        }

        public static void EmitForeach<T>(this ILGenerator il, ushort enumeratorLocal, Action foreachBody)
        {
            EmitForeach(il, enumeratorLocal, typeof(T), foreachBody);
        }
        public static void EmitForeach(this ILGenerator il, ushort enumeratorLocal, Type enumType, Action foreachBody)
        {
            EmitUsing(il, enumeratorLocal, () =>
            {                
                var bottom = il.DefineLabel();
                il.Emit(OpCodes.Br_S, bottom);
                var top = il.DefineLabel();
                il.MarkLabel(top);
                il.EmitLdloc(enumeratorLocal);
                il.Emit(OpCodes.Callvirt, typeof(IEnumerator<>).MakeGenericType(enumType).GetMethod("get_Current"));
                foreachBody();
                il.MarkLabel(bottom);
                il.EmitLdloc(enumeratorLocal);
                il.Emit(OpCodes.Callvirt, typeof(IEnumerator).GetMethod("MoveNext"));
                il.Emit(OpCodes.Brtrue_S, top);
            });
        }

        public static void EmitLiteral(this ILGenerator il, object literal)
        {
            if (literal == null)
                il.Emit(OpCodes.Ldnull);
            if (literal is bool)
                il.Emit((bool)literal ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            else if (literal is int)
                il.EmitLdc_I4((int)literal);
            else if (literal is uint)
                il.EmitLdc_I4(unchecked((int)(uint)literal));
            else if (literal is short)
                il.EmitLdc_I4(unchecked((int)(short)literal));
            else if (literal is ushort)
                il.EmitLdc_I4(unchecked((int)(ushort)literal));
            else if (literal is byte)
                il.EmitLdc_I4(unchecked((int)(byte)literal));
            else if (literal is sbyte)
                il.EmitLdc_I4(unchecked((int)(sbyte)literal));
            else if (literal is char)
                il.EmitLdc_I4(unchecked((int)(char)literal));
            else if (literal is long || literal is ulong)
                il.Emit(OpCodes.Ldc_I8, unchecked((long)literal));
            else if (literal is float)
                il.Emit(OpCodes.Ldc_R4, (float)literal);
            else if (literal is double)
                il.Emit(OpCodes.Ldc_R8, (double)literal);
            else if (literal is string)
                il.Emit(OpCodes.Ldstr, (string)literal);
        }

        public static void EmitLdarg(this ILGenerator il, ushort arg)
        {
            if (arg == 0)
                il.Emit(OpCodes.Ldarg_0);
            else if (arg == 1)
                il.Emit(OpCodes.Ldarg_1);
            else if (arg == 2)
                il.Emit(OpCodes.Ldarg_2);
            else if (arg == 3)
                il.Emit(OpCodes.Ldarg_3);
            else if (arg <= byte.MaxValue)
                il.Emit(OpCodes.Ldarg_S, arg);
            else
                il.Emit(OpCodes.Ldarg, arg);
        }
        public static void EmitLdloc(this ILGenerator il, ushort local)
        {
            if (local == 0)
                il.Emit(OpCodes.Ldloc_0);
            else if (local == 1)
                il.Emit(OpCodes.Ldloc_1);
            else if (local == 2)
                il.Emit(OpCodes.Ldloc_2);
            else if (local == 3)
                il.Emit(OpCodes.Ldloc_3);
            else if (local <= byte.MaxValue)
                il.Emit(OpCodes.Ldloc_S, local);
            else
                il.Emit(OpCodes.Ldloc, local);
        }
        public static void EmitLdc_I4(this ILGenerator il, int arg)
        {
            if (arg == 0)
                il.Emit(OpCodes.Ldc_I4_0);
            else if (arg == 1)
                il.Emit(OpCodes.Ldc_I4_1);
            else if (arg == -1)
                il.Emit(OpCodes.Ldc_I4_M1);
            else if (arg == 2)
                il.Emit(OpCodes.Ldc_I4_2);
            else if (arg == 3)
                il.Emit(OpCodes.Ldc_I4_3);
            else if (arg == 4)
                il.Emit(OpCodes.Ldc_I4_4);
            else if (arg == 5)
                il.Emit(OpCodes.Ldc_I4_5);
            else if (arg == 6)
                il.Emit(OpCodes.Ldc_I4_6);
            else if (arg == 7)
                il.Emit(OpCodes.Ldc_I4_7);
            else if (arg == 8)
                il.Emit(OpCodes.Ldc_I4_8);
            else if (arg <= byte.MaxValue)
                il.Emit(OpCodes.Ldc_I4_S, arg);
            else
                il.Emit(OpCodes.Ldc_I4, arg);
        }

        public static void EmitBoxOrCast(this ILGenerator il, Type type)
        {
            if (type.IsValueType)
                il.Emit(OpCodes.Box, type);
            else
                il.Emit(OpCodes.Castclass, type);
        }
    }

    public static class Reflect
    {
        private static MethodInfo GetMethod(Type type, string name, Type matchingDelegate)
        {
            var parameters = matchingDelegate.GetMethod("Invoke").GetParameters();
            var instSignatureTypes = new Type[parameters.Length - 1];
            for (var i = 0; i + 1 < parameters.Length; i++)
                instSignatureTypes[i] = parameters[i + 1].ParameterType;

            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, instSignatureTypes, null);
            if (method == null)
            {
                var staticSignatureTypes = new Type[parameters.Length];
                Array.Copy(instSignatureTypes, 0, staticSignatureTypes, 1, instSignatureTypes.Length);
                staticSignatureTypes[0] = parameters[0].ParameterType;
                method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, staticSignatureTypes, null);
            }

            return method;
        }
        /// <summary>
        /// Creates a delegate to the public or private method on the type that has the specified name and signature.
        /// For instance methods, add the instance type as the first argument in the signature.
        /// </summary>
        /// <typeparam name="TSignature">The signature of the method and the type of the returned delegate.</typeparam>
        /// <param name="type">The type that declares the method.</param>
        /// <param name="methodName">The name of the method (case-sensitive).</param>
        public static TSignature GetMethodDelegate<TSignature>(this Type type, string methodName) where TSignature : class
        {
            if (!typeof(TSignature).IsSubclassOf(typeof(Delegate))) throw new InvalidOperationException("TSignature must be a delegate type.");
            var method = GetMethod(type, methodName, typeof(TSignature));
            if (method == null) throw new MissingMethodException(type.FullName, methodName);
            return (TSignature)(object)Delegate.CreateDelegate(typeof(TSignature), method);
        }
        /// <summary>
        /// Creates a delegate to the public or private method on the type that has the specified name and signature.
        /// For instance methods, add the instance type as the first argument in the signature.
        /// </summary>
        /// <typeparam name="TSignature">The signature of the method and the type of the returned delegate.</typeparam>
        /// <param name="type">The type that declares the method.</param>
        /// <param name="methodName">The name of the method (case-sensitive).</param>
        public static TSignature GetMethodCallvirt<TSignature>(this Type type, string methodName) where TSignature : class
        {
            return CompileMethod<TSignature>(methodName, type, il =>
            {
                var method = GetMethod(type, methodName, typeof(TSignature));
                var numParameters = method.GetParameters().Length;
                if (!method.IsStatic) numParameters++;
                for (ushort i = 0; i < numParameters; i++)
                    il.EmitLdarg(i);
                il.Emit(OpCodes.Callvirt, method);
                il.Emit(OpCodes.Ret);
            });
        }

        public static TSignature CompileMethod<TSignature>(string name, Type ownerType, Action<ILGenerator> ilGenerator)
            where TSignature : class
        {
            if (!typeof(TSignature).IsSubclassOf(typeof(Delegate)))
                throw new InvalidOperationException("TSignature must be a delegate type.");

            var invoke = typeof(TSignature).GetMethod("Invoke");
            var parameters = invoke.GetParameters();

            var method = new DynamicMethod(name, invoke.ReturnType, parameters.Select(p => p.ParameterType).ToArray(), ownerType);
            for (var i = 0; i < parameters.Length; i++)
                method.DefineParameter(i, parameters[i].Attributes & (ParameterAttributes.In | ParameterAttributes.Out), parameters[i].Name);

            ilGenerator(method.GetILGenerator());

            return (TSignature)(object)method.CreateDelegate(typeof(TSignature));
        }

        public static TSignature CompileMethod<TSignature>(string name, Action<ILGenerator> ilGenerator)
            where TSignature : class
        {
            return (TSignature)(object)CompileMethod(typeof(TSignature), name, ilGenerator);
        }

        public static Delegate CompileMethod(Type signature, string name, Action<ILGenerator> ilGenerator)
        {
            if (!signature.IsSubclassOf(typeof(Delegate)))
                throw new InvalidOperationException("TSignature must be a delegate type.");

            var invoke = signature.GetMethod("Invoke");
            var parameters = invoke.GetParameters();

            var method = new DynamicMethod(name, invoke.ReturnType, parameters.Select(p => p.ParameterType).ToArray(), true);

            for (var i = 0; i < parameters.Length; i++)
                method.DefineParameter(i, parameters[i].Attributes & (ParameterAttributes.In | ParameterAttributes.Out), parameters[i].Name);

            ilGenerator(method.GetILGenerator());

            return method.CreateDelegate(signature);
        }

        public static MethodInfo CompileMethod(Type signature, Type targetType, string name, Action<ILGenerator> ilGenerator)
        {
            if (!signature.IsSubclassOf(typeof(Delegate)))
                throw new InvalidOperationException("TSignature must be a delegate type.");

            var invoke = signature.GetMethod("Invoke");
            var parameters = invoke.GetParameters();

            var method = new DynamicMethod(name, invoke.ReturnType, new[] { targetType }.Concat(parameters.Select(p => p.ParameterType)).ToArray(), true);

            for (var i = 0; i < parameters.Length; i++)
                method.DefineParameter(i + 1, parameters[i].Attributes & (ParameterAttributes.In | ParameterAttributes.Out), parameters[i].Name);

            ilGenerator(method.GetILGenerator());

            return method;
        }


        /// <summary>
        /// Creates a delegate to the public or private method on the type that has the specified name and signature.
        /// For instance methods, add the instance type as the first argument in the signature.
        /// </summary>
        /// <typeparam name="TSignature">The signature of the method and the type of the returned delegate.</typeparam>
        /// <param name="type">The type that declares the method.</param>
        /// <param name="methodName">The name of the method (case-sensitive).</param>
        public static TSignature GetFieldGetter<TSignature>(this Type type, string fieldName) where TSignature : class
        {
            var invokeMethod = typeof(TSignature).GetMethod("Invoke");
            var parameters = invokeMethod.GetParameters();
            switch (parameters.Length)
            {
                case 0:
                    return CompileMethod<TSignature>("getfield_" + fieldName, type, il =>
                    {
                        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (field == null) throw new MissingMemberException("Cannot find a static field with the name \"" + fieldName + "\" on type " + type.AssemblyQualifiedName + ".");
                        if (field.FieldType != invokeMethod.ReturnType) throw new InvalidOperationException("The return type of TSignature does not match the field's type.");

                        il.Emit(OpCodes.Ldsfld, field);
                        il.Emit(OpCodes.Ret);
                    });
                case 1:
                    return CompileMethod<TSignature>("getfield_" + fieldName, type, il =>
                    {
                        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field == null) throw new MissingMemberException("Cannot find an instance field with the name \"" + fieldName + "\" on type " + type.AssemblyQualifiedName + ".");
                        if (field.FieldType != invokeMethod.ReturnType) throw new InvalidOperationException("The return type of TSignature does not match the field's type.");

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, field);
                        il.Emit(OpCodes.Ret);
                    });
                default:
                    throw new InvalidOperationException("TSignature must have either one parameter (for instance fields) or zero parameters (for static fields).");
            }
        }


        #region Constructor and method wrappers

        public static Func<object> CreateConstructor(Type type)
        {
            return CompileMethod<Func<object>>(type.FullName + ".ctor [Reflect]", type, il =>
                CreateConstructorInternal(il, type.GetConstructor(new Type[0]))
            );
        }
        public static Func<T1, object> CreateConstructor<T1>(Type type)
        {
            return CompileMethod<Func<T1, object>>(type.FullName + ".ctor [Reflect]", type, il =>
                CreateConstructorInternal(il, type.GetConstructor(new[] { typeof(T1) }))
            );
        }
        public static Func<T1, T2, object> CreateConstructor<T1, T2>(Type type)
        {
            return CompileMethod<Func<T1, T2, object>>(type.FullName + ".ctor [Reflect]", type, il =>
                CreateConstructorInternal(il, type.GetConstructor(new[] { typeof(T1), typeof(T2) }))
            );
        }
        static void CreateConstructorInternal(ILGenerator il, ConstructorInfo constructor)
        {
            var parameters = constructor.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                il.EmitLdarg((ushort)i);
                il.EmitBoxOrCast(parameters[i].ParameterType);
            }
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);
        }
        #endregion
    }
}
