namespace LiteDB.Shell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var o = new OptionSet(args);

            if (o.Upgrade != null)
            {
                // do upgrade
            }
            else if (o.Run != null)
            {
            }
            else
            {
                ShellProgram.Start(o.Filename);
            }
        }
    }
}