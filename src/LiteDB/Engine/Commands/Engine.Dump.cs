namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    /// <summary>
    /// Dump database variables
    /// </summary>
    internal void Dump()
    {
        Print("Settings", _factory.Settings.ToString());
        Print("State", _factory.State.ToString());
        Print("MemoryCache", _factory.MemoryCache.ToString());
        Print("AllocMap", _factory.AllocationMapService.ToString());
        Print("MemoryFactory", _factory.MemoryFactory.ToString());
        Print("QueryService", _factory.QueryService.ToString());
        Print("WalIndexService", _factory.WalIndexService.ToString());
        Print("AutoIdService", _factory.AutoIdService.ToString());
        Print("LockService", _factory.LockService.ToString());
        Print("DiskService", _factory.DiskService.ToString());
        Print("LogService", _factory.LogService.ToString());
        Print("MasterService", _factory.MasterService.ToString());
        Print("MonitorService", _factory.MonitorService.ToString());
        //Print("SortService", _factory.SortService.ToString());
        Print("QueryService", _factory.QueryService.ToString());

        void Print(string title, string json)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(title.PadRight(18) + ": ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(json);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
