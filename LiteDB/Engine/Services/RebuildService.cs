using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// [ThreadSafe]
    /// </summary>
    internal class RebuildService : IDisposable
    {
        private readonly EngineSettings _settings;

        public RebuildService(EngineSettings settings)
        {
            _settings = settings;
        }

        public long Rebuild(RebuildOptions options)
        {
            return 0;
        }

       public void Dispose()
        {
        }
    }
}