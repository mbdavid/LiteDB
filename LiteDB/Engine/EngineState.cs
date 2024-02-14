using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LiteDB.Constants;


namespace LiteDB.Engine
{
    internal class EngineState
    {
        public bool Disposed = false;
        private readonly ILiteEngine _engine; // can be null for unit tests

        public EngineState(ILiteEngine engine)
        { 
            _engine = engine;
        }

        public void Validate()
        {
            if (this.Disposed) throw LiteException.EngineDisposed();
        }

        public void Handle(Exception ex)
        {
            LOG(ex.Message, "ERROR");

            if (ex is IOException || 
                (ex is LiteException lex && lex.ErrorCode == LiteException.INVALID_DATAFILE_STATE))
            {
                _engine?.Close(ex);
            }
        }
    }
}
