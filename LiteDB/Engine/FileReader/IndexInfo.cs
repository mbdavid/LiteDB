namespace LiteDB.Engine;

internal class IndexInfo
{
    public string Collection { get; set; }
    public string Name { get; set; }
    public string Expression { get; set; }
    public bool Unique { get; set; }
}