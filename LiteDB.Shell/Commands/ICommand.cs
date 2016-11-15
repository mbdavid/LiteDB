using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    public enum DataAccess { None, Read, Write }

    internal interface ICommand
    {
        DataAccess Access { get; }

        bool IsCommand(StringScanner s);

        void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env);
    }
}