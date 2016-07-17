using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;

namespace LiteDB.Plataform
{
    public class LitePlatformFullDotNet : ILitePlatform
    {
        private readonly LazyLoad<IFileHandler> _fileHandler;
        private readonly LazyLoad<IReflectionHandler> _reflectionHandler;
        private readonly LazyLoad<IEncryptionFactory> _encryptionFactory;

        public LitePlatformFullDotNet()
        {
            _fileHandler = new LazyLoad<IFileHandler>(() => new FileHandler());
            _reflectionHandler = new LazyLoad<IReflectionHandler>(() => new EmitReflectionHandler());
            _encryptionFactory = new LazyLoad<IEncryptionFactory>(() => new RijndaelEncryptionFactory());

            AddNameCollectionToMapper();
        }

        public IEncryptionFactory EncryptionFactory { get { return _encryptionFactory.Value; } }
        public IFileHandler FileHandler { get { return _fileHandler.Value; } }
        public IReflectionHandler ReflectionHandler { get { return _reflectionHandler.Value; } }

        public void WaitFor(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        private void AddNameCollectionToMapper()
        {
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
        }
    }
}
