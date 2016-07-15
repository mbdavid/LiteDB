using LiteDB.Interfaces;

namespace LiteDB.Core
{
   [Android.Runtime.Preserve(AllMembers = true)]
   public class LiteDbPlatform
   {
      [Android.Runtime.Preserve]
      public static ILiteDbPlatform Platform;

      public static void ThrowIfNotInitialized()
      {
         if(Platform == null)
            throw new PlatformNotInitializedException();
      }

      [Android.Runtime.Preserve]
      public static void Initialize(ILiteDbPlatform platform)
      {
         Platform = platform;
      }
   }
}


namespace Android.Runtime
{
   // for Xamarin
   public sealed class PreserveAttribute : System.Attribute
   {
      public bool AllMembers;
      public bool Conditional;
   }
}