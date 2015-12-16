namespace LiteDB.Shell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var o = new OptionSet(args);

            if (o.Has("upgrade"))
            {
            }
            else if(o.Has("run"))
            {
            }
            else
            {
                ShellProgram.Start(o.Extra);
            }
        }
    }
}