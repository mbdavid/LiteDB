using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Interface to be implemented when execute shell commands
    /// </summary>
    public interface IShellResult
    {
        int Limit { get; }
        void Write(int resultset, bool single, BsonValue value);
        void Write(Exception ex);
    }
}