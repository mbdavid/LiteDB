using System.Reflection;
using Android.App;
using Android.OS;
using LiteDB.Core;
using Xamarin.Android.NUnitLite;

namespace LiteDB.Tests.Android
{
   [Activity(Label = "LiteDB.Tests.Android", MainLauncher = true, Icon = "@drawable/icon")]
   public class MainActivity : TestSuiteActivity
   {
#pragma warning disable 0219, 0649
      static bool falseflag = false;
      static MainActivity()
      {
         if (falseflag)
         {
            var ignore = new LiteDbPlatform();
         }
      }
#pragma warning restore 0219, 0649


      protected override void OnCreate(Bundle bundle)
      {
         // tests can be inside the main assembly
         AddTest(Assembly.GetExecutingAssembly());
         // or in any reference assemblies
         // AddTest (typeof (Your.Library.TestClass).Assembly);

         // Once you called base.OnCreate(), you cannot add more assemblies.
         base.OnCreate(bundle);
      }
   }
}

