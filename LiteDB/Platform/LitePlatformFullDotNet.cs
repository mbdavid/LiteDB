using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using LiteDB;

namespace LiteDB.Platform
{
    public class LitePlatformFullDotNet : ILitePlatform
    {
        private readonly LazyLoad<IFileHandler> _fileHandler;
        private readonly LazyLoad<IReflectionHandler> _reflectionHandler = new LazyLoad<IReflectionHandler>(() => new EmitReflectionHandler());

        /// <summary>
        /// Default constructor. Will put all files in the application directory unless otherwise specified
        /// </summary>
        public LitePlatformFullDotNet() : this(".")
        {
        }

        /// <summary>
        /// Constructor which accepts a default directory for all files. 
        /// Default directory can be overridden by specifying a full path when opening the database.
        /// </summary>
        /// <param name="defaultPath">Default path where files will be placed.</param>
        public LitePlatformFullDotNet(string defaultPath)
        {
            _fileHandler = new LazyLoad<IFileHandler>(() => new FileHandler(defaultPath));
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
