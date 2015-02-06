using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class Help : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"help$").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var sb = new StringBuilder();

            this.Write(sb, "Web Shell Commands - try offline version for more commands");
            this.Write(sb, "==========================================================");

            this.Write(sb, "> db.<collection>.insert <jsonDoc>", "Insert a new document into collection");
            this.Write(sb, "> db.<collection>.update <jsonDoc>", "Update a document inside collection");
            this.Write(sb, "> db.<collection>.delete <filter>", "Delete documents using a filter clausule (see find)");
            this.Write(sb, "> db.<collection>.find [top N] <filter>", "Show filtered documents based on index search");
            this.Write(sb, "> db.<collection>.count <filter>", "Show count rows according query filter");
            this.Write(sb, "> db.<collection>.ensureIndex <field> [unique]", "Create a new index document field");
            this.Write(sb, "> db.<collection>.indexes", "List all indexes in this collection");
            this.Write(sb, "<filter> = <field> [=|>|>=|<|<=|!=|like|between] <jsonValue>", "Filter query syntax");
            this.Write(sb, "<filter> = (<filter> [and|or] <filter> [and|or] ...)", "Multi queries syntax");

            this.Write(sb, "Try:");
            this.Write(sb, " > db.customers.insert { _id:1, name:\"John Doe\", age: 37 }");
            this.Write(sb, " > db.customers.ensureIndex name");
            this.Write(sb, " > db.customers.find name like \"John\"");
            this.Write(sb, " > db.customers.find top 10 (name like \"John\" and _id between [0, 100])");

            return sb.ToString();
        }

        private void Write(StringBuilder sb, string line1 = null, string line2 = null)
        {
            if (string.IsNullOrEmpty(line1))
            {
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine(line1);

                if (!string.IsNullOrEmpty(line2))
                {
                    sb.AppendLine("    " + line2);
                }
            }
        }
    }
}
