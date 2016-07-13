using LiteDB.Core;
using LiteDB.Platform.iOS;

namespace LiteDB.Platform
{
   public class Platform
   {
      public static void Initialize()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformiOS());
      }
   }
}
