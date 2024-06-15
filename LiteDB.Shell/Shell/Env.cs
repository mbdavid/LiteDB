namespace LiteDB.Shell;

internal class Env
{
    public Display Display { get; set; }
    public InputCommand Input { get; set; }
    public ILiteDatabase Database { get; set; }
    public bool Running { get; set; } = false;
}