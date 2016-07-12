using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    public class BeginTrans : IShellCommand
    {
        internal static Transaction _currentTransaction = null;

        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"begin(\stransaction)?");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            if (_currentTransaction != null)
            {
                throw new LiteException("Transaction already started");
            }
            _currentTransaction = engine.BeginTrans();

            return BsonValue.Null;
        }
    }

    public class CommitTrans : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"commit(\stransaction)?");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            if (BeginTrans._currentTransaction == null)
            {
                throw new LiteException("No trnsaction started");
            }
            BeginTrans._currentTransaction.Commit();
            BeginTrans._currentTransaction = null;

            return BsonValue.Null;
        }
    }

    public class RollbackTrans : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"rollback(\stransaction)?");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            if (BeginTrans._currentTransaction == null)
            {
                throw new LiteException("No trnsaction started");
            }
            BeginTrans._currentTransaction.Rollback();
            BeginTrans._currentTransaction = null;

            return BsonValue.Null;
        }
    }
}