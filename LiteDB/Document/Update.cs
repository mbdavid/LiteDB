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
    internal enum UpdateAction { Set, Add, Remove }

    /// <summary>
    /// Represent a single document field update
    /// </summary>
    public class Update
    {
        internal string Path { get; set; }

        internal LiteExpression Expression { get; set; }

        internal BsonValue Value { get; set; }

        internal UpdateAction Action { get; set; }

        internal Update()
        {
        }

        /// <summary>
        /// Set all fields according JSON path to specific value.
        /// </summary>
        public static Update Set(string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return new Update { Path = path, Value = value, Action = UpdateAction.Set };
        }

        /// <summary>
        /// Set all fields according JSON path to result of an expression.
        /// </summary>
        public static Update SetExpr(string path, string expr)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(expr)) throw new ArgumentNullException(nameof(expr));

            return new Update { Path = path, Expression = new LiteExpression(expr), Action = UpdateAction.Set };
        }

        /// <summary>
        /// Add into an array according JSON path a specific value.
        /// </summary>
        public static Update Add(string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return new Update { Path = path, Value = value, Action = UpdateAction.Add };
        }

        /// <summary>
        /// Add into an array according JSON path a result of an expression.
        /// </summary>
        public static Update AddExpr(string path, string expr)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(expr)) throw new ArgumentNullException(nameof(expr));

            return new Update { Path = path, Expression = new LiteExpression(expr), Action = UpdateAction.Add };
        }

        /// <summary>
        /// Remove all fields according JSON path
        /// </summary>
        public static Update Remove(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            return new Update { Path = path, Action = UpdateAction.Remove };
        }
    }
}