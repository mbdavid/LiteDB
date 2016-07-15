
using System.Threading.Tasks;
#if WINDOWS_UWP
using System.Collections.Specialized;
#endif
using LiteDB.Core;
using LiteDB.Interfaces;
using Windows.Storage;

namespace LiteDB.Universal81
{
   public class LiteDbPlatformWindowsStore : LiteDbPLatformBase
   {
      private readonly StorageFolder m_folder;

      public LiteDbPlatformWindowsStore(StorageFolder folder, IEncryptionFactory encryptionFactory = null) 
         : base(() => encryptionFactory ?? new EncryptionFactory(), () => new ExpressionReflectionHandler(), 
              () => new FileHandlerWindowsStore(folder))
      {
         m_folder = folder;

         AddNameCollectionToMapper();
      }

      public void AddNameCollectionToMapper()
      {
#if WINDOWS_UWP
         BsonMapper.Global.RegisterType(
            nv =>
            {
               var doc = new BsonDocument();

               foreach (var key in nv.AllKeys)
               {
                  doc[key] = nv[key];
               }

               return doc;
            },

            bson =>
            {
               var nv = new NameValueCollection();
               var doc = bson.AsDocument;

               foreach (var key in doc.Keys)
               {
                  nv[key] = doc[key].AsString;
               }

               return nv;
            }
         );
#endif
      }
      public override FileDiskServiceBase CreateFileDiskService(ConnectionString conn, Logger log)
      {
         return new FileDiskService(m_folder, conn, log);
      }

      public override void WaitFor(int milliseconds)
      {
         AsyncHelpers.RunSync(() => Task.Delay(milliseconds));
      }
   }
}
