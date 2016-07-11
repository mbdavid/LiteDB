using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
   [TestClass]
   public class InitializeTests
   {
      [AssemblyCleanup]
      public void AssemblyCleanup()
      {
         // wait all threads close FileDB
         System.Threading.Thread.Sleep(2000);

         DB.DeleteFiles();
      }
   }
}