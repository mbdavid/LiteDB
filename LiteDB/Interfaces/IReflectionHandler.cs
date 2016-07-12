using System;
using System.Reflection;

namespace LiteDB.Interfaces
{
   public delegate object CreateObject();

   public delegate object GenericSetter(object target, object value);

   public delegate object GenericGetter(object obj);


   public interface IReflectionHandler
   {
      CreateObject CreateClass(Type type);
      CreateObject CreateStruct(Type type);

      GenericGetter CreateGenericGetter(Type type, PropertyInfo propertyInfo, bool nonPublic);
      GenericSetter CreateGenericSetter(Type type, PropertyInfo propertyInfo, bool nonPublic);
   }
}