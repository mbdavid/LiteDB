using Windows.Storage;
using LiteDB.Core;
using LiteDB.Universal81;

namespace LiteDB.Tests
{
   public class TestBase
   {
      public TestBase()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformWindowsStore(ApplicationData.Current.TemporaryFolder, new EncryptionFactory()));
      }

   }
}
