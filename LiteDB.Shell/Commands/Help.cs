using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Help : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"help$").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display d)
        {
            d.WriteResult("Shell commands");
            d.WriteResult("==============");

            d.WriteHelp("> open <filename>", "Open a new database");
            d.WriteHelp("> close", "Close current database");
            d.WriteHelp("> run <filename>", "Run commands inside filename");
            d.WriteHelp("> pretty on|off", "Turns on/off pretty json format");
            d.WriteHelp("> timer", "Show timer before prompt");
            d.WriteHelp("> ed", "Open nodepad with last command to edit and execute");
            d.WriteHelp("> spool on|off", "Spool all output in a spool file");
            d.WriteHelp("> -- comment", "Do nothing, its just a comment");
            d.WriteHelp("> /<command>/", "Support for multi line command");
            d.WriteHelp("> exit", "Close LiteDB shell");

            d.WriteResult("");
            d.WriteResult("Transaction commands");
            d.WriteResult("====================");

            d.WriteHelp("> begin", "Begins a new transaction");
            d.WriteHelp("> commit", "Commit current transaction");
            d.WriteHelp("> rollback", "Rollback current transaction");

            d.WriteResult("");
            d.WriteResult("Collections commands");
            d.WriteResult("====================");

            d.WriteHelp("> db.<collection>.insert <jsonDoc>", "Insert a new document into collection");
            d.WriteHelp("> db.<collection>.update <jsonDoc>", "Update a document inside collection");
            d.WriteHelp("> db.<collection>.delete <filter>", "Delete documents using a filter clausule (see find)");
            d.WriteHelp("> db.<collection>.bulk <filename>", "Bulk insert a json file as documents");
            d.WriteHelp("> db.<collection>.find [top N]", "Show all documents. Can limit results in N documents");
            d.WriteHelp("> db.<collection>.find [top N] <filter>", "Show filtered documents based on index search");
            d.WriteHelp("> db.<collection>.count <filter>", "Show count rows according query filter");
            d.WriteHelp("> db.<collection>.exec <filter> { Action<Object (id), BsonDocument (doc), Collection (col), LiteEngine (db)> }", "Execute C# code for each document based on filter.");
            d.WriteHelp("> db.<collection>.ensureIndex <field> [unique]", "Create a new index document field");
            d.WriteHelp("> db.<collection>.indexes", "List all indexes in this collection");
            d.WriteHelp("> db.<collection>.drop", "Drop collection and destroy all documents inside");
            d.WriteHelp("> db.<collection>.dropIndex <field>", "Drop a index and make index area free to use with another index");
            d.WriteHelp("<filter> = <field> [=|>|>=|<|<=|!=|like|between] <jsonValue>", "Filter query syntax");
            d.WriteHelp("<filter> = (<filter> [and|or] <filter> [and|or] ...)", "Multi queries syntax");
            d.WriteHelp("<jsonDoc> = {_id: ... , key: value, key1: value1 }", "Represent a json (extended version) for a BsonDocument. See special data types");
            d.WriteHelp("JsonEx Date", "{ mydate: { $date :\"2015-01-01T23:59:59Z\"} }");
            d.WriteHelp("JsonEx Guid", "{ myguid: { $guid :\"3a1c34b3-9f66-4d8e-975a-d545d898a4ba\"} }");
            d.WriteHelp("JsonEx Binary", "{ mydata: { $binary :\"base64 byte array\"} }");

            d.WriteResult("");
            d.WriteResult("File storage commands");
            d.WriteResult("=====================");

            d.WriteHelp("> fs.find", "List all files on datafile");
            d.WriteHelp("> fs.find <fileId>", "List file info from a key. Supports * for starts with key");
            d.WriteHelp("> fs.upload <fileId> <filename>", "Insert a new file inside database");
            d.WriteHelp("> fs.download <fileId> <filename>", "Save a file to disk passing a file key and filename");
            d.WriteHelp("> fs.update <fileId> {key:value}", "Update metadata file");
            d.WriteHelp("> fs.delete <fileId>", "Remove a file inside database");

            d.WriteResult("");
            d.WriteResult("Other commands");
            d.WriteResult("==============");

            d.WriteHelp("> db.info", "Get database informations");
            d.WriteHelp("> dump", "Display dump database information");

            d.WriteResult("");
            d.WriteResult("Try:");
            d.WriteResult(" > db.customers.insert { _id:1, name:\"John Doe\", age: 37 }");
            d.WriteResult(" > db.customers.ensureIndex name");
            d.WriteResult(" > db.customers.find name like \"J\"");
            d.WriteResult(" > db.customers.find _id > 0");
        }
    }
}
