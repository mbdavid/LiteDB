using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal class FileDiskService : IDiskService
    {
        private const int LOCK_POSITION = 0;

        private FileStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private string _filename;

        public FileDiskService(string filename)
        {
            _filename = filename;
        }
        
        public void Initialize()
        {
            // open file as readOnly - if we need use Write, re-open in Write Mode
            _stream = new FileStream(_filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite, BasePage.PAGE_SIZE);

            _reader = new BinaryReader(_stream);

            if (_stream.Length == 0)
            {
                this.WritePages(new HeaderPage[] { new HeaderPage() });
            }
        }

        public void Lock()
        {
            TryExec(() => _stream.Lock(LOCK_POSITION, 1));
        }

        public void Unlock()
        {
            _stream.Unlock(LOCK_POSITION, 1);
        }

        public T ReadPage<T>(uint pageID)
            where T : BasePage, new()
        {
            var page = new T();
            var posStart = (long)pageID * (long)BasePage.PAGE_SIZE;
            var posEnd = posStart + BasePage.PAGE_SIZE;

            TryExec(() => 
            {
                // position cursor
                if (_stream.Position != posStart)
                {
                    _stream.Seek(posStart, SeekOrigin.Begin);
                }

                // read page header
                page.ReadHeader(_reader);

                // if T is base and PageType has a defined type, convert page
                var isBase = page.GetType() == typeof(BasePage);

                if (isBase)
                {
                    if (page.PageType == PageType.Index) page = (T)(object)page.CopyTo<IndexPage>();
                    else if (page.PageType == PageType.Data) page = (T)(object)page.CopyTo<DataPage>();
                    else if (page.PageType == PageType.Extend) page = (T)(object)page.CopyTo<ExtendPage>();
                    else if (page.PageType == PageType.Collection) page = (T)(object)page.CopyTo<CollectionPage>();
                }

                // read page content if page is not empty
                if (page.PageType != PageType.Empty)
                {
                    page.ReadContent(_reader);
                }

                // position cursor at starts next page
                _reader.ReadBytes((int)(posEnd - _stream.Position));
            });

            return page;
        }

        public void WritePages(IEnumerable<BasePage> pages)
        {
            if(_writer == null)
            {
                _reader.Dispose();
                _stream.Dispose();
                _stream = new FileStream(_filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, BasePage.PAGE_SIZE);
                _reader = new BinaryReader(_stream);
                _writer = new BinaryWriter(_stream);
            }

            foreach(var page in pages)
            {
                var posStart = (long)page.PageID * (long)BasePage.PAGE_SIZE;
                var posEnd = posStart + BasePage.PAGE_SIZE;

                // position cursor
                if (_stream.Position != posStart)
                {
                    _stream.Seek(posStart, SeekOrigin.Begin);
                }

                // write page header
                page.WriteHeader(_writer);

                // write content except for empty pages
                if (page.PageType != PageType.Empty)
                {
                    page.WriteContent(_writer);
                }

                // write with zero non-used page
                _writer.Write(new byte[posEnd - _stream.Position]);

                page.IsDirty = false;
            }
        }

        public void Dispose()
        {
            if(_stream != null)
            {
                _stream.Dispose();
                _reader.Close();
                if(_writer != null) _writer.Dispose();
            }
        }

        public static void TryExec(Action action)
        {
            var timeout = new TimeSpan(0, 1, 0);
            var timer = DateTime.Now.Add(timeout);

            while (DateTime.Now < timer)
            {
                try
                {
                    action();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(250);
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(250);
                }
            }

            throw LiteException.LockTimeout(timeout);
        }
    }
}
