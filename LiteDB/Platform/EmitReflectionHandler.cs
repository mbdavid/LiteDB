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

            return(GenericGetter)method.CreateDelegate(typeof(GenericGetter));
        }
        static void EmitCall(ILGenerator generator, MethodInfo method)
        {
            OpCode opcode = (method.IsStatic || method.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
            generator.EmitCall(opcode, method, null);
        }
        static void EmitTypeConversion(ILGenerator generator, Type castType, bool isContainer)
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

        public GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
        {
            MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic);
            if (setMethod == null)
            {

                return null;

            }

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
    }
}