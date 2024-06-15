namespace LiteDB.Engine;

public partial class LiteEngine
{
    /// <summary>
    ///     Register all internal system collections avaiable by default
    /// </summary>
    private void InitializeSystemCollections()
    {
        RegisterSystemCollection("$database", () => SysDatabase());

        RegisterSystemCollection("$cols", () => SysCols());
        RegisterSystemCollection("$indexes", () => SysIndexes());

        RegisterSystemCollection("$sequences", () => SysSequences());

        RegisterSystemCollection("$transactions", () => SysTransactions());
        RegisterSystemCollection("$snapshots", () => SysSnapshots());
        RegisterSystemCollection("$open_cursors", () => SysOpenCursors());

        RegisterSystemCollection(new SysFile()); // use single $file(?) for all file formats
        RegisterSystemCollection(new SysDump(_header, _monitor));
        RegisterSystemCollection(new SysPageList(_header, _monitor));

        RegisterSystemCollection(new SysQuery(this));
    }
}