using System;
using System.Reflection;
using System.Reflection.Emit;

namespace LiteDB.Platform
{
    public class EmitReflectionHandler : IReflectionHandler
    {
        public CreateObject CreateClass(Type type)
        {
            DynamicMethod dynamicMethod = new DynamicMethod("_", type, (Type[])null);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
            ilGenerator.Emit(OpCodes.Ret);
            return (CreateObject)dynamicMethod.CreateDelegate(typeof(CreateObject));
        }

        public CreateObject CreateStruct(Type type)
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

        public GenericGetter CreateGenericGetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic);
            if (getMethod == null)
                return null;

            return target => getMethod.Invoke(target, null);
        }

        private static GenericSetter CreateSetMethod(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            var setMethod = propertyInfo.GetSetMethod(nonPublic);

            if (setMethod == null) return null;

            return (target, value) => setMethod.Invoke(target, new[] { value });
        }

        public GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            return CreateSetMethod(type, propertyInfo, nonPublic);
        }
    }
}