using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class Help : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"help$").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display d)
        {
            d.WriteResult("Web Shell Commands - try offline version for more commands");
            d.WriteResult("==========================================================");

            d.WriteHelp("> db.<collection>.insert <jsonDoc>", "Insert a new document into collection");
            d.WriteHelp("> db.<collection>.update <jsonDoc>", "Update a document inside collection");
            d.WriteHelp("> db.<collection>.delete <filter>", "Delete documents using a filter clausule (see find)");
            d.WriteHelp("> db.<collection>.find [top N] <filter>", "Show filtered documents based on index search");
            d.WriteHelp("> db.<collection>.count <filter>", "Show count rows according query filter");
            d.WriteHelp("> db.<collection>.ensureIndex <field> [unique]", "Create a new index document field");
            d.WriteHelp("> db.<collection>.indexes", "List all indexes in this collection");
            d.WriteHelp("<filter> = <field> [=|>|>=|<|<=|!=|like|between] <jsonValue>", "Filter query syntax");
            d.WriteHelp("<filter> = (<filter> [and|or] <filter> [and|or] ...)", "Multi queries syntax");

            d.WriteResult("Try:");
            d.WriteResult(" > db.customers.insert { _id:1, name:\"John Doe\", age: 37 }");
            d.WriteResult(" > db.customers.ensureIndex name");
            d.WriteResult(" > db.customers.find name like \"John\"");
            d.WriteResult(" > db.customers.find top 10 (name like \"John\" and _id between [0, 100])");
        }
    }
}
