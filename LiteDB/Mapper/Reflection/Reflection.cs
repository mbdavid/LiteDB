using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace LiteDB
{
    #region Delegates

    internal delegate object CreateObject();

    internal delegate void GenericSetter(object target, object value);

    internal delegate object GenericGetter(object obj);

    #endregion

    /// <summary>
    /// Helper class to get entity properties and map as BsonValue
    /// </summary>
    internal class Reflection
    {
        private static Dictionary<Type, CreateObject> _cacheCtor = new Dictionary<Type, CreateObject>();

        #region Reflection Handler

        public static CreateObject CreateClass(Type type)
        {
            DynamicMethod dynamicMethod = new DynamicMethod("_", type, (Type[])null);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Ret);
            return (CreateObject)dynamicMethod.CreateDelegate(typeof(CreateObject));
        }

        public static CreateObject CreateStruct(Type type)
        {
            DynamicMethod dynamicMethod = new DynamicMethod("_", typeof(object), (Type[])null);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            LocalBuilder local = ilGenerator.DeclareLocal(type);
            ilGenerator.Emit(OpCodes.Ldloca_S, local);
            ilGenerator.Emit(OpCodes.Initobj, type);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Box, type);
            ilGenerator.Emit(OpCodes.Ret);
            return (CreateObject)dynamicMethod.CreateDelegate(typeof(CreateObject));
        }

        public static GenericGetter CreateGenericGetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            var gMethod = propertyInfo.GetGetMethod(nonPublic);
            if (gMethod == null) return null;

            DynamicMethod method = new DynamicMethod("_", typeof(object), new[] { typeof(object) }, true);
            ILGenerator generator = method.GetILGenerator();
            generator.DeclareLocal(typeof(object));
            generator.Emit(OpCodes.Ldarg_0);


            EmitTypeConversion(generator, propertyInfo.DeclaringType, true);
            EmitCall(generator, gMethod);
            if (propertyInfo.PropertyType.IsValueType)
            {
                generator.Emit(OpCodes.Box, propertyInfo.PropertyType);
            }

            generator.Emit(OpCodes.Ret);

            return (GenericGetter)method.CreateDelegate(typeof(GenericGetter));
        }

        public static GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic);
            if (setMethod == null) return null;

            DynamicMethod method = new DynamicMethod("_", typeof(void), new[] { typeof(object), typeof(object) }, true);
            ILGenerator generator = method.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            EmitTypeConversion(generator, propertyInfo.DeclaringType, true);
            generator.Emit(OpCodes.Ldarg_1);
            EmitTypeConversion(generator, propertyInfo.PropertyType, false);
            EmitCall(generator, setMethod);
            generator.Emit(OpCodes.Ret);

            return (GenericSetter)method.CreateDelegate(typeof(GenericSetter));
        }

        private static void EmitCall(ILGenerator generator, MethodInfo method)
        {
            OpCode opcode = (method.IsStatic || method.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
            generator.EmitCall(opcode, method, null);
        }

        private static void EmitTypeConversion(ILGenerator generator, Type castType, bool isContainer)
        {
            if (castType == typeof(object))
            {
            }
            else if (castType.IsValueType)
            {
                generator.Emit(isContainer ? OpCodes.Unbox : OpCodes.Unbox_Any, castType);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, castType);
            }
        }

        #endregion

        #region GetIdProperty

        /// <summary>
        /// Gets PropertyInfo that refers to Id from a document object.
        /// </summary>
        public static PropertyInfo GetIdProperty(Type type)
        {
            // Get all properties and test in order: BsonIdAttribute, "Id" name, "<typeName>Id" name
#if NETFULL
            return SelectProperty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic),
                x => Attribute.IsDefined(x, typeof(BsonIdAttribute), true),
#else
            return SelectProperty(type.GetRuntimeProperties(),
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

        #endregion
    }
}