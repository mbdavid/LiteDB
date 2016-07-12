using System;
using System.Reflection;
using System.Reflection.Emit;
using LiteDB.Interfaces;

namespace LiteDB
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

         var getter = new DynamicMethod("_", typeof(object), new Type[] { typeof(object) }, type, true);
         var il = getter.GetILGenerator();

         if (!type.IsClass) // structs
         {
            var lv = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Unbox_Any, type);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca_S, lv);
            il.EmitCall(OpCodes.Call, getMethod, null);
            if (propertyInfo.PropertyType.IsValueType)
               il.Emit(OpCodes.Box, propertyInfo.PropertyType);
         }
         else
         {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            il.EmitCall(OpCodes.Callvirt, getMethod, null);
            if (propertyInfo.PropertyType.IsValueType)
               il.Emit(OpCodes.Box, propertyInfo.PropertyType);
         }

         il.Emit(OpCodes.Ret);

         return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
      }

      private static GenericSetter CreateSetMethod(Type type, PropertyInfo propertyInfo, bool nonPublic)
      {
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
      }

      public GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic)
      {
         return CreateSetMethod(type, propertyInfo, nonPublic);
      }
   }
}