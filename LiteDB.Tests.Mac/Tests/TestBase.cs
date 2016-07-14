using LiteDB.Core;

namespace LiteDB.Tests
{
   public class TestBase
   {
      public TestBase()
      {
			LiteDbPlatform.Initialize(new LiteDbPlatformFullDotNet());
      }

   }
}
