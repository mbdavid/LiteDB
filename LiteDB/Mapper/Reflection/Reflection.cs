using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NET35
using System.Reflection.Emit;
#endif

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

        public static GenericGetter CreateGenericGetter(Type type, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException("memberInfo");

            var obj = Expression.Parameter(typeof(object), "o");
            var accessor = Expression.MakeMemberAccess(Expression.Convert(obj, memberInfo.DeclaringType), memberInfo);

            return Expression.Lambda<GenericGetter>(Expression.Convert(accessor, typeof(object)), obj).Compile();
        }

        public static GenericSetter CreateGenericSetter(Type type, MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException("propertyInfo");
            
            // if has no write
            if (memberInfo is PropertyInfo && (memberInfo as PropertyInfo).CanWrite == false) return null;

#if NET35
            // do not use Expression in Structs (there is no Expression.Unbox in NET3.5)
            if (type.IsValueType)
            {
                // if member is property, use GetSetMethod
                if(memberInfo is PropertyInfo)
                {
                    var setMethod = (memberInfo as PropertyInfo).GetSetMethod();

                    return (t, v) => setMethod.Invoke(t, new[] { v });
                }
                else return null; // no field on Structs
            }
#endif
            var dataType = memberInfo is PropertyInfo ?
                (memberInfo as PropertyInfo).PropertyType :
                (memberInfo as FieldInfo).FieldType;

            var target = Expression.Parameter(typeof(object), "obj");
            var value = Expression.Parameter(typeof(object), "val");

#if NET35
            var castTarget = Expression.Convert(target, type);
#else
            var castTarget = type.GetTypeInfo().IsValueType ? 
                Expression.Unbox(target, type) :
                Expression.Convert(target, type);
#endif

            var castValue = Expression.ConvertChecked(value, dataType);

            var accessor =
                memberInfo is PropertyInfo ? Expression.Property(castTarget, memberInfo as PropertyInfo) :
                memberInfo is FieldInfo ? Expression.Field(castTarget, memberInfo as FieldInfo) : null;
#if NET35
            var assign = ExpressionExtensions.Assign(accessor, castValue);
#else
            var assign = Expression.Assign(accessor, castValue);
#endif
            var conv = Expression.Convert(assign, typeof(object));
            
            return Expression.Lambda<GenericSetter>(conv, target, value).Compile();
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
            catch (Exception ex)
            {
                throw LiteException.InvalidCtor(type, ex);
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
#if NET35
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
                            throw LiteException.InvalidCtor(type, null);
                        }
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