using LiteDB.Core;
using LiteDB.Platform.iOS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
   public class TestBase
   {
      [ClassInitialize]
      public void Initialize()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformiOS(new EncryptionFactory(),
     new ExpressionReflectionHandler(), new FileHandler()));
      }

   }
}
