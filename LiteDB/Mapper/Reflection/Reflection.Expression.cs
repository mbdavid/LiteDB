#if !NET35
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteDB
{
    /// <summary>
    /// Using Expressions is the easy and fast way to create classes, structs, get/set fields/properties. But it not works in NET35
    /// </summary>
    internal partial class Reflection
    {
        public static CreateObject CreateClass(Type type)
        {
            return Expression.Lambda<CreateObject>(Expression.New(type)).Compile();
        }

        public static CreateObject CreateStruct(Type type)
        {
            var newType = Expression.New(type);
            var convert = Expression.Convert(newType, typeof(object));

            return Expression.Lambda<CreateObject>(convert).Compile();
        }

        public static GenericGetter CreateGenericGetter(Type type, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException("memberInfo");

            // if has no read
            if (memberInfo is PropertyInfo && (memberInfo as PropertyInfo).CanRead == false) return null;

            var obj = Expression.Parameter(typeof(object), "o");
            var accessor = Expression.MakeMemberAccess(Expression.Convert(obj, memberInfo.DeclaringType), memberInfo);

            return Expression.Lambda<GenericGetter>(Expression.Convert(accessor, typeof(object)), obj).Compile();
        }

        public static GenericSetter CreateGenericSetter(Type type, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException("propertyInfo");
            
            // if has no write
            if (memberInfo is PropertyInfo && (memberInfo as PropertyInfo).CanWrite == false) return null;

            var dataType = memberInfo is PropertyInfo ?
                (memberInfo as PropertyInfo).PropertyType :
                (memberInfo as FieldInfo).FieldType;

            var target = Expression.Parameter(typeof(object), "obj");
            var value = Expression.Parameter(typeof(object), "val");

            var castTarget = type.GetTypeInfo().IsValueType ? 
                Expression.Unbox(target, type) :
                Expression.Convert(target, type);

            var castValue = Expression.ConvertChecked(value, dataType);

            var accessor =
                memberInfo is PropertyInfo ? Expression.Property(castTarget, memberInfo as PropertyInfo) :
                memberInfo is FieldInfo ? Expression.Field(castTarget, memberInfo as FieldInfo) : null;

            var assign = Expression.Assign(accessor, castValue);
            var conv = Expression.Convert(assign, typeof(object));
            
            return Expression.Lambda<GenericSetter>(conv, target, value).Compile();
        }
    }
}
#endif