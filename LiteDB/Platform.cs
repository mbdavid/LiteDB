using LiteDB.Core;

namespace LiteDB.Platform
{
   public class Platform
   {
      public static void Initialize()
      {
         LiteDbPlatform.Initialize(new LiteDbPlatformFullDotNet());
      }
   }
}
