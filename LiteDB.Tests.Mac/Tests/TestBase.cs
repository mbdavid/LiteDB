using LiteDB.Core;
using LiteDB.Platform.iOS;

namespace LiteDB.Tests
{
   public class TestBase
   {
      public TestBase()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformiOS(new EncryptionFactory(),
     new ExpressionReflectionHandler(), new FileHandler()));
      }

   }
}
