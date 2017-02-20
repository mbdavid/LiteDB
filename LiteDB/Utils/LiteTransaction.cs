using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LiteDB
{
    public class LiteTransaction : IDisposable
    {
        private Action _commit;
        private Action _rollback;

        internal LiteTransaction(Action commit, Action rollback)
        {
            _commit = commit;
            _rollback = rollback;
        }

        public void Rollback()
        {
            _rollback();
        }

        public void Dispose()
        {
            bool exceptionThrown = Marshal.GetExceptionCode() != 0;

            if (exceptionThrown)
                _rollback();
            else
                _commit();
        }
    }
}
