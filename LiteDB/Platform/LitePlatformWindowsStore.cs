using System;
using System.Threading.Tasks;
#if WINDOWS_UWP
using System.Collections.Specialized;
#endif
using Windows.Storage;
using LiteDB.Platform;

namespace LiteDB.Platform
{
    public class LitePlatformWindowsStore : ILitePlatform
    {
        private readonly LazyLoad<IFileHandler> _fileHandler;
        private readonly LazyLoad<IReflectionHandler> _reflectionHandler;
        private readonly Func<string, IEncryption> _encryption;

        /// <summary>
        /// Default construtor. Places all database files in the default application folder.
        /// </summary>
        public LitePlatformWindowsStore() : this(Windows.Storage.ApplicationData.Current.LocalFolder) { }

        /// <summary>
        /// Constructor which allows encryption, but sets the default folder to the application data folder.
        /// </summary>
        /// <param name="encryption"></param>
        public LitePlatformWindowsStore(Func<string, IEncryption> encryption) : this(Windows.Storage.ApplicationData.Current.LocalFolder, encryption) { }
        
        // Making this private for now, because putting the folder anywhere but in the application store causes performance issues.
        private LitePlatformWindowsStore(StorageFolder folder, Func<string, IEncryption> encryption = null)
        {
            _fileHandler = new LazyLoad<IFileHandler>(() => new FileHandlerWindowsStore(folder));
            _reflectionHandler = new LazyLoad<IReflectionHandler>(() => new ExpressionReflectionHandler());
            _encryption = encryption;

            AddNameCollectionToMapper();
        }

        public IFileHandler FileHandler { get { return _fileHandler.Value; } }

        public IReflectionHandler ReflectionHandler { get { return _reflectionHandler.Value; } }

        public IEncryption GetEncryption(string password)
        {
            if (_encryption == null) throw new ArgumentException("Encryption requested, but encryption was not set during initialization");

            return _encryption(password);
        }

        public void WaitFor(int milliseconds)
        {
            AsyncHelpers.RunSync(() => Task.Delay(milliseconds));
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
    }
}
