using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteDB
{
    #region Delegates

    internal delegate object CreateObject();

    public delegate void GenericSetter(object target, object value);

    public delegate object GenericGetter(object obj);

    #endregion

    /// <summary>
    /// Helper class to get entity properties and map as BsonValue
    /// </summary>
    internal partial class Reflection
    {
        private static Dictionary<Type, CreateObject> _cacheCtor = new Dictionary<Type, CreateObject>();

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
            catch (Exception ex)
            {
                throw LiteException.InvalidCtor(type, ex);
            }

            lock (_cacheCtor)
            {
                try
                {
                    CreateObject c = null;
                    if (_cacheCtor.TryGetValue(type, out c))
                    {
                        return c();
                    }

                    if (type.GetTypeInfo().IsClass)
                    {
                        _cacheCtor.Add(type, c = CreateClass(type));
                    }
                    else if (type.GetTypeInfo().IsInterface) // some know interfaces
                    {
                        if(type.GetTypeInfo().IsGenericType)
                        {
                            var typeDef = type.GetGenericTypeDefinition();

                            if (typeDef == typeof(IList<>) || 
                                typeDef == typeof(ICollection<>) ||
                                typeDef == typeof(IEnumerable<>))
                            {
                                return CreateInstance(GetGenericListOfType(UnderlyingTypeOf(type)));
                            }
                            else if (typeDef == typeof(IDictionary<,>))
                            {
#if NET35
                                var k = type.GetGenericArguments()[0];
                                var v = type.GetGenericArguments()[1];
#else
                                var k = type.GetTypeInfo().GenericTypeArguments[0];
                                var v = type.GetTypeInfo().GenericTypeArguments[1];
#endif
                                return CreateInstance(GetGenericDictionaryOfType(k, v));
                            }
                        }

                        throw LiteException.InvalidCtor(type, null);
                    }
                    else // structs
                    {
                        _cacheCtor.Add(type, c = CreateStruct(type));
                    }

                    return c();
                }
                catch (Exception ex)
                {
                    throw LiteException.InvalidCtor(type, ex);
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

        /// <summary>
        /// Get underlying get - using to get inner Type from Nullable type
        /// </summary>
        public static Type UnderlyingTypeOf(Type type)
        {
            // works only for generics (if type is not generic, returns same type)
#if NET35
            if (!type.IsGenericType) return type;

            return type.GetGenericArguments()[0];
#else
            if (!type.GetTypeInfo().IsGenericType) return type;

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

        /// <summary>
        /// Get item type from a generic List or Array
        /// </summary>
        public static Type GetListItemType(Type listType)
        {
            if (listType.IsArray) return listType.GetElementType();

#if NET35
            foreach (var i in listType.GetInterfaces())
#else
            foreach (var i in listType.GetTypeInfo().ImplementedInterfaces)
#endif
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
#if NET35
                    return i.GetGenericArguments()[0];
#else
                    return i.GetTypeInfo().GenericTypeArguments[0];
#endif
                }
                // if interface is IEnumerable (non-generic), let's get from listType and not from interface
                // from #395
                else if(listType.GetTypeInfo().IsGenericType && i == typeof(IEnumerable))
                {
#if NET35
                    return listType.GetGenericArguments()[0];
#else
                    return listType.GetTypeInfo().GenericTypeArguments[0];
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

#if NET35
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