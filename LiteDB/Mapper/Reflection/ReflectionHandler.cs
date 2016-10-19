using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace LiteDB
{
    internal delegate object CreateObject();

    internal delegate object GenericSetter(object target, object value);

    internal delegate object GenericGetter(object obj);

    internal class ReflectionHandler
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

        public static GenericGetter CreateGenericGetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            var getMethod = propertyInfo.GetGetMethod(nonPublic);

            if (getMethod == null)
                return null;

            return target => getMethod.Invoke(target, null);
        }

        public static GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            var setMethod = propertyInfo.GetSetMethod(nonPublic);

            if (setMethod == null) return null;

            return (target, value) => setMethod.Invoke(target, new[] { value });
        }
    }
}