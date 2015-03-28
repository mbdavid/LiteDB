using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionExec : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "exec");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var query = s.Match("{") ? Query.All() : this.ReadQuery(s);
            var code = DynamicCode.GetCode(s);

            var docs = col.Find(query).ToArray();

            try
            {
                db.BeginTrans();

                foreach (var doc in docs)
                {
                    code(doc["_id"], doc, col, db);
                }

                db.Commit();

                return docs.Length;
            }
            catch (Exception ex)
            {
                db.Rollback();
                throw ex;
            }
        }
    }

    internal class DynamicCode
    {
        private const string CODE_TEMPLATE = @"
using System; 
using LiteDB;

public class Program {
    public static void DoWork(
        BsonValue id, 
        BsonDocument doc, 
        LiteCollection<BsonDocument> col, 
        LiteDatabase db) { [code] }
}";

        public static Action<object, BsonDocument, LiteCollection<BsonDocument>, LiteDatabase> GetCode(StringScanner s)
        {
            var str = s.Scan(@"[\s\S]*");
            var code = CODE_TEMPLATE.Replace("[code]", str);
            var provider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();

            parameters.ReferencedAssemblies.Add("LiteDB.dll");
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;

            var results = provider.CompileAssemblyFromSource(parameters, code);

            if (results.Errors.HasErrors)
            {
                var err = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    err.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }
                throw new InvalidOperationException(err.ToString().Trim());
            }

            var assembly = results.CompiledAssembly;
            var program = assembly.GetType("Program");
            var method = program.GetMethod("DoWork");

            return new Action<object, BsonDocument, LiteCollection<BsonDocument>, LiteDatabase>((id, doc, col, db) =>
            {
                method.Invoke(null, new object[] { id, doc, col, db });
            });
        }
    }
}
