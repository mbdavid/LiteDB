namespace LiteDB.Engine;

using System;

/// <summary>
///     Represent a single internal engine variable that user can read/change
/// </summary>
internal class Pragma
{
    public string Name { get; set; }
    public Func<BsonValue> Get { get; set; }
    public Action<BsonValue> Set { get; set; }
    public Action<BufferSlice> Read { get; set; }
    public Action<BsonValue, HeaderPage> Validate { get; set; }
    public Action<BufferSlice> Write { get; set; }
}