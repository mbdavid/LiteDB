using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using LiteDB.Platform;

namespace LiteDB.Platform
{
    public class LitePlatformiOS : ILitePlatform
    {
        private readonly LazyLoad<IFileHandler> _fileHandler = new LazyLoad<IFileHandler>(() => new FileHandler(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal)));
        private readonly LazyLoad<IReflectionHandler> _reflectionHandler = new LazyLoad<IReflectionHandler>(() => new ExpressionReflectionHandler());

        public LitePlatformiOS()
        {
            AddNameCollectionToMapper();
        }

        public IFileHandler FileHandler { get { return _fileHandler.Value; } }

        public IReflectionHandler ReflectionHandler { get { return _reflectionHandler.Value; } }

        public IEncryption GetEncryption(string password)
        {
            return new RijndaelEncryption(password);
        }

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
