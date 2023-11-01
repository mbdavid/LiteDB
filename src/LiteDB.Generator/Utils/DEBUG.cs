using System.Diagnostics;

public static class SourceGenDebugger
{
    [Conditional("DEBUG")]
    public static void DEBUG()
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            // sadly this is Windows only so as of now :(
            //Debugger.Launch();
        }
#endif
    }
}
