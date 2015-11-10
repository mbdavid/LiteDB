using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Query documents and execute, for each document, action method. All data is locked during execution
        /// </summary>
        public void FindAndModify(Query query, Action<T> action)
        {
            if (query == null) throw new ArgumentNullException("query");
            if (action == null) throw new ArgumentNullException("action");

            this.Database.Transaction.Begin();

            try
            {
                var docs = this.Find(query);

                foreach(var doc in docs)
                {
                    action(doc);

                    this.Update(doc);
                }

                this.Database.Transaction.Commit();
            }
            catch
            {
                this.Database.Transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Query documents and execute, for each document, action method. All data is locked during execution
        /// </summary>
        public void FindAndModify(Expression<Func<T, bool>> predicate, Action<T> action)
        {
            this.FindAndModify(_visitor.Visit(predicate), action);
        }
    }
}
