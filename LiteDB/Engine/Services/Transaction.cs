using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// </summary>
    internal class Transaction
    {
        private IDiskFactory _disk;
        private AesEncryption _crypto;
        private LockService _locker;
        private PageService _pager;
        private CacheService _cache;
        private Logger _log;

        internal Transaction(IDiskFactory disk, AesEncryption crypto, PageService pager, LockService locker, CacheService cache, Logger log)
        {
            _disk = disk;
            _crypto = crypto;
            _cache = cache;
            _locker = locker;
            _pager = pager;
            _log = log;
        }


    }
}