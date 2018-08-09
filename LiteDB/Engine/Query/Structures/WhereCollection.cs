using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public class WhereCollection : ICollection<BsonExpression>
    {
        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(BsonExpression item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(BsonExpression item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(BsonExpression[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<BsonExpression> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(BsonExpression item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
