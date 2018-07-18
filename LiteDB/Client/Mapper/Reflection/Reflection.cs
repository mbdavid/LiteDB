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
                if (_cacheCtor.TryGetValue(type, out CreateObject c))
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
                    if (_cacheCtor.TryGetValue(type, out CreateObject c))
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
                                var k = type.GetTypeInfo().GetGenericArguments()[0];
                                var v = type.GetTypeInfo().GetGenericArguments()[1];

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
            var t = type.GetTypeInfo();

            if (!type.GetTypeInfo().IsGenericType) return type;

            return type.GetTypeInfo().GetGenericArguments()[0];
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

            foreach (var i in listType.GetInterfaces())
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return i.GetTypeInfo().GetGenericArguments()[0];
                }
                // if interface is IEnumerable (non-generic), let's get from listType and not from interface
                // from #395
                else if(listType.GetTypeInfo().IsGenericType && i == typeof(IEnumerable))
                {
                    return listType.GetTypeInfo().GetGenericArguments()[0];
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
            if (type == typeof(string)) return false; // do not define "String" as IEnumerable<char>

            foreach (var @interface in type.GetInterfaces())
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

        /// <summary>
        /// Select member from a list of member using predicate order function to select
        /// </summary>
        public static MemberInfo SelectMember(IEnumerable<MemberInfo> members, params Func<MemberInfo, bool>[] predicates)
        {
            foreach (var predicate in predicates)
            {
                var member = members.FirstOrDefault(predicate);

                if (member != null)
                {
                    return member;
                }
            }

            return null;
        }

        #endregion
    }
}