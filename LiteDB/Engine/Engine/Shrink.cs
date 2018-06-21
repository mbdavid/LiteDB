using System;
using System.IO;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Reduce disk size re-arranging unused spaces. Can change password.
        /// </summary>
        public long Shrink(string password = null)
        {
            _log.Info("shrink datafile" + (password != null ? " with password" : ""));

            return 0;
            // var originalSize = _datafile.FileLength;
            // 
            // // if temp disk are not passed, use memory stream disk
            // using (_locker.Write())
            // using (var engine = new LiteEngine(new ConnectionString { Filename = ":temp:", Password = password }))
            // {
            //     var temp = engine._disk;
            // 
            //     // read all collection
            //     foreach (var collectionName in this.GetCollectionNames())
            //     {
            //         // first create all user indexes (exclude _id index)
            //         foreach (var index in this.GetIndexes(collectionName).Where(x => x.Name != "_id"))
            //         {
            //             engine.EnsureIndex(collectionName, index.Name, index.Unique);
            //         }
            // 
            //         // now copy documents 
            //         var docs = this.Find(collectionName, Query.All());
            // 
            //         engine.InsertBulk(collectionName, docs);
            //     }
            // 
            //     // copy user version
            //     engine.UserVersion = this.UserVersion;
            // 
            //     // set current disk size to exact new disk usage
            //     _disk.SetLength(temp.FileLength);
            // 
            //     // read new header page to start copy
            //     var header = BasePage.ReadPage(temp.ReadPage(0)) as HeaderPage;
            // 
            //     // copy (as is) all pages from temp disk to original disk
            //     for (uint i = 0; i <= header.LastPageID; i++)
            //     {
            //         var page = temp.ReadPage(i);
            // 
            //         _disk.WritePage(i, page);
            //     }
            // 
            //     // create/destroy crypto class
            //     _crypto = password == null ? null : new AesEncryption(password, header.Salt);
            // 
            //     // initialize all services again (crypto can be changed)
            //     this.InitializeServices();
            //     
            //     // return how many bytes are reduced
            //     return originalSize - temp.FileLength;
            // }
        }
    }
}
