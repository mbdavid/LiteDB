//using System.Runtime.InteropServices;

//using static LiteDB.Constants;


//var start = GC.GetAllocatedBytesForCurrentThread();

//Console.WriteLine(start);

//var m = Marshal.AllocHGlobal(819200);

////await Task.Delay(1000);

//var end = GC.GetAllocatedBytesForCurrentThread();

//var diff = end - start;

//Console.WriteLine(end);
//Console.WriteLine($"Total Allocated: {diff}");

//Marshal.FreeHGlobal(m);