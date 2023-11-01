namespace LiteDB.Engine;

internal enum PageType : byte 
{ 
    Empty = 0, 
    AllocationMap = 1, 
    Index = 2, 
    Data = 3,
    Unknown = 255
}