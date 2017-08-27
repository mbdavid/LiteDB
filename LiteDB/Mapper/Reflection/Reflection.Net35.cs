#if NET35
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteDB
{
    /// <summary>
    /// Unity3D version without any Emit
    /// </summary>
    internal partial class Reflection
    {
        public static CreateObject CreateClass(Type type)
        {
            return () => Activator.CreateInstance(type);
        }

        public static CreateObject CreateStruct(Type type)
        {
            return () => Activator.CreateInstance(type);
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

            return target => getMethod.Invoke(target, null);
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

            return (target, value) => setMethod.Invoke(target, new[] { value });
        }
    }
}
#endif