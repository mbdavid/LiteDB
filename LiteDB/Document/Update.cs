/*
db.col1.update
    $.Name = "ok", 
    $.Address = { Street: "Ipiranga", Number: 445 }
    $.Address.Number = COUNT(..)
    $.Items[*].Name = COUNT(...),
    $.Name = null // remove?
    -$.Name // better way?
WHERE _id=1

{
    Path (string)
    Value (BsonValue)
    Expr (string)
    Action (set/add/remove)
}

db.Update(string collection, Query query, 
    Update.Set("$.Name", "teste"), 
    Update.SetExpr("$.Name", "COUNT(..)"), 
    Update.Add("$.Name", "COUNT(..)"), 
    Update.AddExpr("$.Name", "COUNT(..)"), 
    Update.Remove("$.Name"));
*/

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Represent a single document field update
    /// </summary>
    public class Update
    {
        internal string Path { get; set; }

        internal LiteExpression Expression { get; set; }

        internal BsonValue FieldValue { get; set; }

        internal bool Delete { get; set; }

        private Update()
        {
        }

        /// <summary>
        /// Add/Update all fields according JSON path.
        /// </summary>
        public static Update Value(string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return new Update { Path = path, FieldValue = value };
        }

        /// <summary>
        /// Add/Update all fields according JSON path applying expression calculation over document. Gets first result only to field
        /// </summary>
        public static Update Expr(string path, string expr)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(expr)) throw new ArgumentNullException(nameof(expr));

            return new Update { Path = path, Expression = new LiteExpression(expr) };
        }

        /// <summary>
        /// Remove all fields according JSON path
        /// </summary>
        public static Update Remove(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            return new Update { Path = path, Delete = true };
        }
    }
}