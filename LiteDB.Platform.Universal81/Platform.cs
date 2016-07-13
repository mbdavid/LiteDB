using Windows.Storage;
using LiteDB.Core;
using LiteDB.Universal81;

namespace LiteDB.Platform
{
   public class Platform
   {
      public static void Initialize()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformWindowsStore(ApplicationData.Current.LocalFolder));
      }
   }
}
