using System;
using LiteDB.Plataform;

namespace LiteDB.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            LitePlatform.Initialize(new LitePlatformFullDotNet());
        }
    }
}
