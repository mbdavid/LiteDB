using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Interface to be implemented when execute shell commands
    /// </summary>
    public interface IShellOutput
    {
        int Limit { get; }
        void Write(BsonValue value, int index, int resultset);
        void Write(Exception ex);
    }
}