using System;
using System.Collections.Generic;

namespace LiteDB
{
    public class LiteTransaction : IDisposable
    {
        private Action _commit;
        private Action _rollback;
        private bool _dispose;

        internal LiteTransaction(Action commit, Action rollback)
        {
            _commit = commit;
            _rollback = rollback;
            _dispose = true;
        }

        public void Commit()
        {
            _commit();
            _dispose = false;
        }

        public void Rollback()
        {
            _rollback();
            _dispose = false;
        }

        public void Dispose()
        {
            if(_dispose == true) _rollback();
        }
    }
}