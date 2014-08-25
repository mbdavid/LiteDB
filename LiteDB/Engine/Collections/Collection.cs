using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class Collection<T>
        where T : new()
    {
        private uint _pageID;
        private LiteEngine _engine;
        private string _name;

        internal Collection(LiteEngine engine, string name)
        {
            _engine = engine;
            _name = name;
            _pageID = uint.MaxValue;
        }

        /// <summary>
        /// Get the collection page only when nedded. Gets from cache always to garantee that wil be the last (in case of _clearCache will get a new one)
        /// </summary>
        internal CollectionPage GetCollectionPage()
        {
            if (_pageID == uint.MaxValue)
            {
                var col = _engine.Collections.Get(_name);

                if (col == null)
                {
                    col = _engine.Collections.Add(_name);
                }

                _pageID = col.PageID;

                return col;
            }

            return _engine.Pager.GetPage<CollectionPage>(_pageID);
        }

        #region NextVal/CurrVal

        public int NextVal()
        {
            return NextVal(1, null);
        }

        public int NextVal(int step, int? newSequence = null)
        {
            _engine.Transaction.Begin();

            var col = this.GetCollectionPage();

            try
            {
                if (newSequence.HasValue)
                {
                    col.Sequence = newSequence.Value;
                }
                else
                {
                    col.Sequence = col.Sequence + step;
                }

                var seq = col.Sequence;

                col.IsDirty = true;

                _engine.Commit();

                return seq;
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }
        }

        public int CurrVal()
        {
            return this.GetCollectionPage().Sequence;
        }

        #endregion
    }
}
