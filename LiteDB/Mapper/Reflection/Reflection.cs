using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NETFULL
using System.Reflection.Emit;
#endif

namespace LiteDB
{
    #region Delegates

    internal delegate object CreateObject();

    public delegate object GenericSetter(object target, object value);

    public delegate object GenericGetter(object obj);

    #endregion

    /// <summary>
    /// Helper class to get entity properties and map as BsonValue
    /// </summary>
    internal partial class Reflection
    {
        private static Dictionary<Type, CreateObject> _cacheCtor = new Dictionary<Type, CreateObject>();

        #region Reflection

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
            if (propertyInfo == null) throw new ArgumentNullException("propertyInfo");

            var obj = Expression.Parameter(typeof(object), "o");
            var accessor = Expression.MakeMemberAccess(Expression.Convert(obj, propertyInfo.DeclaringType), propertyInfo);

            return Expression.Lambda<GenericGetter>(Expression.Convert(accessor, typeof(object)), obj).Compile();
        }

        public static GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            // there is no Expression.Assign in .NET 3.5
#if NETFULL
            var setMethod = propertyInfo.GetSetMethod(nonPublic);

            if (setMethod == null) return null;

            var setter = new DynamicMethod("_", typeof(object), new Type[] { typeof(object), typeof(object) }, true);
            var il = setter.GetILGenerator();

            if (!type.IsClass) // structs
            {
                var lv = il.DeclareLocal(type);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox_Any, type);
                il.Emit(OpCodes.Stloc_0);
                il.Emit(OpCodes.Ldloca_S, lv);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(propertyInfo.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, propertyInfo.PropertyType);
                il.EmitCall(OpCodes.Call, setMethod, null);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Box, type);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(propertyInfo.PropertyType.IsClass ? OpCodes.Castclass : OpCodes.Unbox_Any, propertyInfo.PropertyType);
                il.EmitCall(OpCodes.Callvirt, setMethod, null);
                il.Emit(OpCodes.Ldarg_0);
            }

            il.Emit(OpCodes.Ret);

            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
#else
            if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));
            
            if (!propertyInfo.CanWrite)
                return null;
            
            var obj = Expression.Parameter(typeof(object), "obj");
            var value = Expression.Parameter(typeof(object), "val");
            var accessor = Expression.Property(Expression.Convert(obj, propertyInfo.DeclaringType), propertyInfo);
            var assign = Expression.Assign(accessor, Expression.Convert(value, propertyInfo.PropertyType));
            var conv = Expression.Convert(assign, typeof(object));
            
            return Expression.Lambda<GenericSetter>(conv, obj, value).Compile();
#endif
        }

        #endregion

        #region CreateInstance

        /// <summary>
        /// Create a new instance from a Type
        /// </summary>
        public static object CreateInstance(Type type)
        {
            try
            {
                CreateObject c;
                if (_cacheCtor.TryGetValue(type, out c))
                {
                    return c();
                }
            }
            catch
            {
                throw LiteException.InvalidCtor(type);
            }

            lock (_cacheCtor)
            {
                try
                {
                    CreateObject c = null;

                    if (type.GetTypeInfo().IsClass)
                    {
                        _cacheCtor.Add(type, c = CreateClass(type));
                    }
                    else if (type.GetTypeInfo().IsInterface) // some know interfaces
                    {
                        if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))
                        {
                            return CreateInstance(GetGenericListOfType(UnderlyingTypeOf(type)));
                        }
                        else if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
                        {
                            return CreateInstance(GetGenericListOfType(UnderlyingTypeOf(type)));
                        }
                        else if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            return CreateInstance(GetGenericListOfType(UnderlyingTypeOf(type)));
                        }
                        else if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        {
#if NETFULL
                            var k = type.GetGenericArguments()[0];
                            var v = type.GetGenericArguments()[1];
#else
                            var k = type.GetTypeInfo().GenericTypeArguments[0];
                            var v = type.GetTypeInfo().GenericTypeArguments[1];
#endif
                            return CreateInstance(GetGenericDictionaryOfType(k, v));
                        }
                        else
                        {
                            throw LiteException.InvalidCtor(type);
                        }
                    }
                    else // structs
                    {
                        _cacheCtor.Add(type, c = CreateStruct(type));
                    }

                    return c();
                }
                catch (Exception)
                {
                    throw LiteException.InvalidCtor(type);
                }
            }
        }

        #endregion

        #region Utils

        public static bool IsNullable(Type type)
        {
            if (!type.GetTypeInfo().IsGenericType) return false;
            var g = type.GetGenericTypeDefinition();
            return (g.Equals(typeof(Nullable<>)));
        }

        public static Type UnderlyingTypeOf(Type type)
        {
#if NETFULL
            return type.GetGenericArguments()[0];
#else
            return type.GetTypeInfo().GenericTypeArguments[0];
#endif
        }

        public static Type GetGenericListOfType(Type type)
        {
            var listType = typeof(List<>);
            return listType.MakeGenericType(type);
        }

        public static Type GetGenericDictionaryOfType(Type k, Type v)
        {
            var listType = typeof(Dictionary<,>);
            return listType.MakeGenericType(k, v);
        }

        public static Type GetListItemType(object list)
        {
            var type = list.GetType();

            if (type.IsArray) return type.GetElementType();
#if NETFULL
            foreach (var i in type.GetInterfaces())
#else
            foreach (var i in type.GetTypeInfo().ImplementedInterfaces)
#endif
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
#if NETFULL
                    return i.GetGenericArguments()[0];
#else
                    return i.GetTypeInfo().GenericTypeArguments[0];
#endif
                }
            }

            return typeof(object);
        }

        /// <summary>
        /// Returns true if Type is any kind of Array/IList/ICollection/....
        /// </summary>
        public static bool IsList(Type type)
        {
            if (type.IsArray) return true;

#if NETFULL
            foreach (var @interface in type.GetInterfaces())
#else
            foreach (var @interface in type.GetTypeInfo().ImplementedInterfaces)
#endif
            {
                if (@interface.GetTypeInfo().IsGenericType)
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        // if needed, you can also return the type used as generic argument
                        return true;
                    }
                }
            }

            return false;
        }

        public static PropertyInfo SelectProperty(IEnumerable<PropertyInfo> props, params Func<PropertyInfo, bool>[] predicates)
        {
            foreach (var predicate in predicates)
            {
                var prop = props.FirstOrDefault(predicate);

                if (prop != null)
                {
                    if (!prop.CanRead || !prop.CanWrite)
                    {
                        throw LiteException.PropertyReadWrite(prop);
                    }

                    return prop;
                }
            }

            return null;
        }

        #endregion
    }
}