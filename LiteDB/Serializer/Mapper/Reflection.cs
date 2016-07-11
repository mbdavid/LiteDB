using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LiteDB.Interfaces;

namespace LiteDB
{
    /// <summary>
    /// Helper class to get entity properties and map as BsonValue
    /// </summary>
    public class Reflection
    {

        private static Dictionary<Type, CreateObject> _cacheCtor = new Dictionary<Type, CreateObject>();

        #region GetIdProperty

        /// <summary>
        /// Gets PropertyInfo that refers to Id from a document object.
        /// </summary>
        public static PropertyInfo GetIdProperty(Type type)
        {
#if PCL
            return SelectProperty(type.GetRuntimeProperties(),
#else
            // Get all properties and test in order: BsonIdAttribute, "Id" name, "<typeName>Id" name
            return SelectProperty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic),
#endif
#if NETFULL
                x => Attribute.IsDefined(x, typeof(BsonIdAttribute), true),
#else
                x => x.GetCustomAttribute(typeof(BsonIdAttribute)) != null,
#endif
                x => x.Name.Equals("Id", StringComparison.OrdinalIgnoreCase),
                x => x.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase));
        }

        private static PropertyInfo SelectProperty(IEnumerable<PropertyInfo> props, params Func<PropertyInfo, bool>[] predicates)
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

        #endregion GetIdProperty

        #region GetProperties

        /// <summary>
        /// Read all properties from a type - store in a static cache - exclude: Id and [BsonIgnore]
        /// </summary>
        public static Dictionary<string, PropertyMapper> GetProperties(Type type, Func<string, string> resolvePropertyName)
        {
            var dict = new Dictionary<string, PropertyMapper>(StringComparer.OrdinalIgnoreCase);
            var id = GetIdProperty(type);
            var ignore = typeof(BsonIgnoreAttribute);
            var idAttr = typeof(BsonIdAttribute);
            var fieldAttr = typeof(BsonFieldAttribute);
            var indexAttr = typeof(BsonIndexAttribute);
#if PCL
            var props = type.GetRuntimeProperties();
#else
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
#endif
            foreach (var prop in props)
            {
                // ignore indexer property
                if (prop.GetIndexParameters().Length > 0) continue;

                // ignore not read/write
                ////if (!prop.CanRead || !prop.CanWrite) continue;
                if (!prop.CanRead) continue;

                // [BsonIgnore]
                if (prop.IsDefined(ignore, false)) continue;

                // check if property has [BsonField]
                var bsonField = prop.IsDefined(fieldAttr, false);

                // create getter/setter IL function
                var getter = LiteDB.Core.LiteDbPlatform.Platform.ReflectionHandler.CreateGenericGetter(type, prop, bsonField);
                var setter = LiteDB.Core.LiteDbPlatform.Platform.ReflectionHandler.CreateGenericSetter(type, prop, bsonField);

                // if not getter or setter - no mapping
                if (getter == null) continue;

                var name = id != null && id.Equals(prop) ? "_id" : resolvePropertyName(prop.Name);

                // check if property has [BsonField] with a custom field name
                if (bsonField)
                {
                    var field = (BsonFieldAttribute)prop.GetCustomAttributes(fieldAttr, false).FirstOrDefault();
                    if (field != null && field.Name != null) name = field.Name;
                }

                // check if property has [BsonId] to get with was setted AutoId = true
                var autoId = (BsonIdAttribute)prop.GetCustomAttributes(idAttr, false).FirstOrDefault();

                // checks if this proerty has [BsonIndex]
                var index = (BsonIndexAttribute)prop.GetCustomAttributes(indexAttr, false).FirstOrDefault();

                // if is _id field, do not accept index definition
                if (name == "_id") index = null;

                // test if field name is OK (avoid to check in all instances) - do not test internal classes, like DbRef
                if (BsonDocument.IsValidFieldName(name) == false) throw LiteException.InvalidFormat(prop.Name, name);

                // create a property mapper
                var p = new PropertyMapper
                {
                    AutoId = autoId == null ? true : autoId.AutoId,
                    FieldName = name,
                    PropertyName = prop.Name,
                    PropertyType = prop.PropertyType,
                    IndexOptions = index == null ? null : index.Options,
                    Getter = getter,
                    Setter = setter
                };

                dict.Add(prop.Name, p);
            }

            return dict;
        }

        #endregion GetProperties

  
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

                    if (_cacheCtor.TryGetValue(type, out c))
                    {
                        return c();
                    }
                    else
                    {
                        if (type.GetTypeInfo().IsClass)
                        {
                     _cacheCtor.Add(type, c = LiteDB.Core.LiteDbPlatform.Platform.ReflectionHandler.CreateClass(type));
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
#if PCL
                                var k = type.GetTypeInfo().GenericTypeArguments[0];
                                var v = type.GetTypeInfo().GenericTypeArguments[1];
#else
                                var k = type.GetGenericArguments()[0];
                                var v = type.GetGenericArguments()[1];
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
                     _cacheCtor.Add(type, c = LiteDB.Core.LiteDbPlatform.Platform.ReflectionHandler.CreateStruct(type));
                        }

                        return c();
                    }
                }
                catch (Exception ex)
                {
                    throw LiteException.InvalidCtor(type);
                }
            }
        }

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
#if PCL
            foreach (var i in type.GetTypeInfo().ImplementedInterfaces)
#else
            foreach (var i in type.GetInterfaces())
#endif
            {
                if (i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
#if PCL
                    return i.GetTypeInfo().GenericTypeArguments[0];
#else
                    return i.GetGenericArguments()[0];
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

#if PCL
            foreach (Type @interface in type.GetTypeInfo().ImplementedInterfaces)
#else
            foreach (Type @interface in type.GetInterfaces())
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

        #endregion Utils
    }
}