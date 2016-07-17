using System;

namespace LiteDB
{
    public class PlatformNotInitializedException : LiteException
    {
        public PlatformNotInitializedException()
           : base("LiteDbPlatform not initialized.")
        {
        }
    }
}
