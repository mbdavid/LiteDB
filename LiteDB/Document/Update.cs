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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Represent an update multi field to by apply in a document
    /// </summary>
    public class Update
    {
        private List<Func<BsonDocument, bool>> _updates = new List<Func<BsonDocument, bool>>();

        #region Public methods to Add/Set

        /// <summary>
        /// Set fields according JSON path to specific value.
        /// </summary>
        public Update Set(string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (value == null) throw new ArgumentNullException(nameof(value));

            this.InsertSet(path, value);

            return this;
        }

        /// <summary>
        /// Set fields according JSON path to an expression evaluate (first result).
        /// </summary>
        public Update SetExpr(string path, string expr)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (expr == null) throw new ArgumentNullException(nameof(expr));

            this.InsertSet(path, new BsonExpression(expr));

            return this;
        }

        /// <summary>
        /// Add into an array according JSON path a specific value.
        /// </summary>
        public Update Add(string path, BsonValue value)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (value == null) throw new ArgumentNullException(nameof(value));

            this.InsertAdd(path, value);

            return this;
        }

        /// <summary>
        /// Add into an array according JSON path a result of an expression.
        /// </summary>
        public Update AddExpr(string path, string expr)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(expr)) throw new ArgumentNullException(nameof(expr));

            this.InsertAdd(path, new BsonExpression(expr));

            return this;
        }

        #endregion


        private void InsertSet(string path, object value)
        {
            _updates.Add((doc) =>
            {
                var field = path.StartsWith("$") ? path : "$." + path;
                var parent = field.Substring(0, field.LastIndexOf('.'));
                var key = field.Substring(field.LastIndexOf('.') + 1);
                var expr = new BsonExpression(parent);
                var val = value is BsonValue ? value as BsonValue : (value as BsonExpression).Execute(doc, true).First();
                var changed = false;

                foreach (var item in expr.Execute(doc, false).Where(x => x.IsDocument))
                {
                    var idoc = item.AsDocument;
                    var cur = idoc[key];

                    // update field only if value are different from current value
                    if(cur != val)
                    {
                        idoc[key] = val;
                        changed = true;
                    }
                }

                return changed;
            });
        }

        private void InsertAdd(string path, object value)
        {
            _updates.Add((doc) =>
            {
                var expr = new BsonExpression(path.StartsWith("$") ? path : "$." + path);
                var val = value is BsonValue ? value as BsonValue : (value as BsonExpression).Execute(doc, true).First();
                var changed = false;

                foreach (var arr in expr.Execute(doc, false).Where(x => x.IsArray))
                {
                    arr.AsArray.Add(val);
                    changed = true;
                }

                return changed;
            });
        }

        /// <summary>
        /// Execute update in all defined fields adding/setting new values. Returns true if any field are updated/added
        /// </summary>
        public bool Execute(BsonDocument doc)
        {
            var changed = false;

            foreach (var fn in _updates)
            {
                if (fn(doc)) changed = true;
            }

            return changed;
        }
    }
}