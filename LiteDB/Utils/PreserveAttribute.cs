using System;

namespace Android.Runtime
{
    // for Xamarin
    public sealed class PreserveAttribute : System.Attribute
    {
        public bool AllMembers;
        public bool Conditional;
    }
}