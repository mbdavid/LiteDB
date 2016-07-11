using LiteDB.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
   public class TestBase
   {
      [ClassInitialize]
      public void Initialize()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformFullDotNet(new EncryptionFactory(),
     new ExpressionReflectionHandler(), new FileHandler()));
      }

   }
}
