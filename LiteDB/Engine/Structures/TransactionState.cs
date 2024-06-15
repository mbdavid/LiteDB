namespace LiteDB.Engine;

internal enum TransactionState
{
    Active,
    Committed,
    Aborted,
    Disposed
}