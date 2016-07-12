using LiteDB.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
   [TestClass]
   public class InitializeTests
   {
      //[AssemblyInitialize]
      //public static void AssemblyLoaded()
      //{
      //   LiteDbPlatform.Initialize(new LiteDbPlatformFullDotNet());
      //}

      [AssemblyCleanup]
      public static void AssemblyCleanup()
      {
         // wait all threads close FileDB
         System.Threading.Thread.Sleep(2000);

         DB.DeleteFiles();
      }
   }
}