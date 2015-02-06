using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Help : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"help$").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var sb = new StringBuilder();

            this.Write(sb, "Shell commands");
            this.Write(sb, "==============");

            this.Write(sb, "> open <filename>", "Open a new database");
            this.Write(sb, "> close", "Close current database");
            this.Write(sb, "> run <filename>", "Run commands inside filename");
            this.Write(sb, "> pretty on|off", "Turns on/off pretty json format");
            this.Write(sb, "> timer", "Show timer before prompt");
            this.Write(sb, "> ed", "Open nodepad with last command to edit and execute");
            this.Write(sb, "> spool on|off", "Spool all output in a spool file");
            this.Write(sb, "> -- comment", "Do nothing, its just a comment");
            this.Write(sb, "> /<command>/", "Support for multi line command");
            this.Write(sb, "> exit", "Close LiteDB shell");

            this.Write(sb);
            this.Write(sb, "Transaction commands");
            this.Write(sb, "====================");

            this.Write(sb, "> begin", "Begins a new transaction");
            this.Write(sb, "> commit", "Commit current transaction");
            this.Write(sb, "> rollback", "Rollback current transaction");

            this.Write(sb);
            this.Write(sb, "Collections commands");
            this.Write(sb, "====================");

            this.Write(sb, "> db.<collection>.insert <jsonDoc>", "Insert a new document into collection");
            this.Write(sb, "> db.<collection>.update <jsonDoc>", "Update a document inside collection");
            this.Write(sb, "> db.<collection>.delete <filter>", "Delete documents using a filter clausule (see find)");
            this.Write(sb, "> db.<collection>.bulk <filename>", "Bulk insert a json file as documents");
            this.Write(sb, "> db.<collection>.find [top N]", "Show all documents. Can limit results in N documents");
            this.Write(sb, "> db.<collection>.find [top N] <filter>", "Show filtered documents based on index search");
            this.Write(sb, "> db.<collection>.count <filter>", "Show count rows according query filter");
            this.Write(sb, "> db.<collection>.exec <filter> { Action<Object (id), BsonDocument (doc), Collection (col), LiteDatabase (db)> }", "Execute C# code for each document based on filter.");
            this.Write(sb, "> db.<collection>.ensureIndex <field> [unique]", "Create a new index document field");
            this.Write(sb, "> db.<collection>.indexes", "List all indexes in this collection");
            this.Write(sb, "> db.<collection>.drop", "Drop collection and destroy all documents inside");
            this.Write(sb, "> db.<collection>.dropIndex <field>", "Drop a index and make index area free to use with another index");
            this.Write(sb, "<filter> = <field> [=|>|>=|<|<=|!=|like|between] <jsonValue>", "Filter query syntax");
            this.Write(sb, "<filter> = (<filter> [and|or] <filter> [and|or] ...)", "Multi queries syntax");
            this.Write(sb, "<jsonDoc> = {_id: ... , key: value, key1: value1 }", "Represent a json (extended version) for a BsonDocument. See special data types");
            this.Write(sb, "JsonEx Date", "{ mydate: { $date :\"2015-01-01T23:59:59Z\"} }");
            this.Write(sb, "JsonEx Guid", "{ myguid: { $guid :\"3a1c34b3-9f66-4d8e-975a-d545d898a4ba\"} }");
            this.Write(sb, "JsonEx Binary", "{ mydata: { $binary :\"base64 byte array\"} }");

            this.Write(sb);
            this.Write(sb, "File storage commands");
            this.Write(sb, "=====================");

            this.Write(sb, "> fs.find", "List all files on datafile");
            this.Write(sb, "> fs.find <fileId>", "List file info from a key. Supports * for starts with key");
            this.Write(sb, "> fs.upload <fileId> <filename>", "Insert a new file inside database");
            this.Write(sb, "> fs.download <fileId> <filename>", "Save a file to disk passing a file key and filename");
            this.Write(sb, "> fs.update <fileId> {key:value}", "Update metadata file");
            this.Write(sb, "> fs.delete <fileId>", "Remove a file inside database");

            this.Write(sb);
            this.Write(sb, "Other commands");
            this.Write(sb, "==============");

            this.Write(sb, "> db.info", "Get database informations");
            this.Write(sb, "> dump", "Display dump database information");

            this.Write(sb);
            this.Write(sb, "Try:");
            this.Write(sb, " > db.customers.insert { _id:1, name:\"John Doe\", age: 37 }");
            this.Write(sb, " > db.customers.ensureIndex name");
            this.Write(sb, " > db.customers.find name like \"J\"");
            this.Write(sb, " > db.customers.find _id > 0");

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
                    sb.AppendLine("  " + line2);
                    sb.AppendLine();
                }
            }
        }
    }
}
