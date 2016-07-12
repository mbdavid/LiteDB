using LiteDB.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Mapping
{
   [TestClass]
   public class ReflectionTests
   {
      public class MyClass
      {
         public string str { get; set; }
         public int num { get; set; }
         public byte asdasd { get; set; }
      }

      public struct MyStruct
      {
         public string str { get; set; }
         public int num { get; set; }
         public byte asdasd { get; set; }
      }


#if PCL
      [TestMethod]
      public void OneHundredMillionObjectsWithExpressionCompile()
      {
         var reflectionCache = new ExpressionReflectionHandler();

         var cachedCall = reflectionCache.CreateClass(typeof(MyClass));

         for (int i = 0; i < 100000000; i++)
         {
            var obj = cachedCall();

         }
      }


      [TestMethod]
      public void OneHundredMillionStructsWithExpressionCompile()
      {
         var reflectionCache = new ExpressionReflectionHandler();

         var cachedCall = reflectionCache.CreateStruct(typeof(MyStruct));

         for (int i = 0; i < 100000000; i++)
         {
            var obj = cachedCall();

         }
      }
#else
      [TestMethod]
      public void OneHundredMillionObjectsWithILGeneration()
      {
         var reflectionCache = new EmitReflectionHandler();

         var cachedCall = reflectionCache.CreateClass(typeof(MyClass));

         for (int i = 0; i < 100000000; i++)
         {
            var obj = cachedCall();

         }
      }

      [TestMethod]
      public void OneHundredMillionStructsWithILGeneration()
      {
         var reflectionCache = new EmitReflectionHandler();

         var cachedCall = reflectionCache.CreateStruct(typeof(MyStruct));

         for (int i = 0; i < 100000000; i++)
         {
            var obj = cachedCall();

         }
      }

#endif
   }
}
