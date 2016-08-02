using System;

namespace LiteDB
{
    public class PlatformNotInitializedException : LiteException
    {
        public PlatformNotInitializedException()
           : base(PLATFORM_NOT_INITIALIZED, "LitePlatform not initialized.")
        {
        }
    }
}
