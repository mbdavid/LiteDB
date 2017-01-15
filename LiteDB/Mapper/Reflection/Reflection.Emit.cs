#if NET35
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace LiteDB
{
    /// <summary>
    /// Using Emit an fast and old way to create classes/structs and getter/setter fields and property. Works fine only for propertis (fields/structs use simple reflection). Not works for NetStandard (use Expressions)
    /// </summary>
    internal partial class Reflection
    {
        public static CreateObject CreateClass(Type type)
        {
            var dynamicMethod = new DynamicMethod("_", type, (Type[])null);
            var il = dynamicMethod.GetILGenerator();

            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ret);

            return (CreateObject)dynamicMethod.CreateDelegate(typeof(CreateObject));
        }

        public static CreateObject CreateStruct(Type type)
        {
            var dynamicMethod = new DynamicMethod("_", typeof(object), (Type[])null);
            var il = dynamicMethod.GetILGenerator();
            var local = il.DeclareLocal(type);

            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Ret);

            return (CreateObject)dynamicMethod.CreateDelegate(typeof(CreateObject));
        }

        public static GenericGetter CreateGenericGetter(Type type, MemberInfo memberInfo)
        {
            // when member is a field, use simple Reflection
            if (memberInfo is FieldInfo)
            {
                var fieldInfo = memberInfo as FieldInfo;

                return fieldInfo.GetValue;
            }

            // if is property, use Emit IL code
            var propertyInfo = memberInfo as PropertyInfo;
            var getMethod = propertyInfo.GetGetMethod(true);

            if (getMethod == null) return null;

            // with struct, still using reflection-only
            if (!type.IsClass)
            {
                return target => getMethod.Invoke(target, null);
            }

            var getter = new DynamicMethod("_", typeof(object), new Type[] { typeof(object) }, type, true);
            var il = getter.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            il.EmitCall(OpCodes.Callvirt, getMethod, null);

            if (propertyInfo.PropertyType.GetTypeInfo().IsValueType)
            {
                il.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }

            il.Emit(OpCodes.Ret);

            return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
        }

        public static GenericSetter CreateGenericSetter(Type type, MemberInfo memberInfo)
        {
            // when member is a field, use simple Reflection
            if (memberInfo is FieldInfo)
            {
                var fieldInfo = memberInfo as FieldInfo;

                return fieldInfo.SetValue;
            }

            // if is property, use Emit IL code
            var propertyInfo = memberInfo as PropertyInfo;
            var setMethod = propertyInfo.GetSetMethod(true);

            if (setMethod == null) return null;

            // with struct, still using reflection-only
            if (!type.IsClass)
            {
                return (target, value) => setMethod.Invoke(target, new[] { value });
            }

            // create IL code to setter property
            var setter = new DynamicMethod("_", typeof(void), new Type[] { typeof(object), typeof(object) }, true);
            var il = setter.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(propertyInfo.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, propertyInfo.PropertyType);
            il.EmitCall(OpCodes.Callvirt, setMethod, null);
            il.Emit(OpCodes.Ret);

            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }
    }
}
#endif
