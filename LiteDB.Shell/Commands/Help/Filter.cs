using System;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Filter Syntax",
        Name = "filter",
        Syntax = "[<field>|<index>|<path/expression>] [=|!=|>|>=|<|<=|in|contains|like|between] <value> ([and|or] <filter>)",
        Description = "Filter documents based on index keys, field documents or document expressions. Support all common operator. Support bracket to inner filter. Can be used in many collections commands, like `find`, `count`, `exists`, `update`, `delete` and `select`.",
        Examples = new string[] {
            "db.customers.find _id = 1",
            "db.customers.find name startsWith \"John\" and _id > 100",
            "db.customers.find customer = 2 and (age = 20 or age = 30)",
            "db.customers.find age between [20, 30]",
            "db.customers.find age in [21, 22, 30, 31]",
            "db.customers.find YEAR($.birthday) = 1977"
        }
    )]
    internal class FilterHelp : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return false;
        }

        public void Execute(StringScanner s, Env env)
        {
        }
    }
}