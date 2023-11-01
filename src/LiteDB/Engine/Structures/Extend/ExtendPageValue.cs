namespace LiteDB.Engine;

internal enum ExtendPageValue : byte
{
    Empty = 0, // 000
    Data = 1,  // 001
    Index = 2, // 010
               // 3-6 reserved
    Full = 7,  // 111

    NoChange = 255
}