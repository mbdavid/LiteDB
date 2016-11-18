using System;

namespace LiteDB.Shell.Commands
{
    internal class BeginTrans : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"begin(\stransaction)?");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            engine.BeginTrans();

            return BsonValue.Null;
        }
    }

    internal class CommitTrans : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"commit(\stransaction)?");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            engine.Commit();

            return BsonValue.Null;
        }
    }

    internal class RollbackTrans : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"rollback(\stransaction)?");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            engine.Rollback();

            return BsonValue.Null;
        }
    }
}