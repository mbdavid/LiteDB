using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Service to implement External MergeSort, in disk, to run ORDER BY command.
    /// [ThreadSafe]
    /// </summary>
    public class MergeSortService : IDisposable
    {
        private readonly IStreamFactory _factory;
        private readonly ConcurrentQueue<long> _freePositions = new ConcurrentQueue<long>();
        private readonly ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();

        private readonly int _containerSize;
        private readonly bool _utcDate;

        private long _lastContainerPosition = 0;

        public MergeSortService(/*IStreamFactory factory, */int containerSize, bool utcDate)
        {
            ENSURE(containerSize % PAGE_SIZE == 0, "size must be PAGE_SIZE multiple");

            //_factory = factory;
            _factory = new FileStreamFactory(@"c:\temp\_sort-data.db", false);
            _containerSize = containerSize;
            _utcDate = utcDate;
        }

        /// <summary>
        /// Get next avaiable disk position - can be a new extend file or reuse container slot
        /// </summary>
        private long GetContainerPosition()
        {
            lock(_freePositions)
            {
                if (_freePositions.TryDequeue(out var position))
                {
                    return position;
                }

                position = _lastContainerPosition;

                _lastContainerPosition += _containerSize;

                return position;
            }
        }

        /// <summary>
        /// Get a re-use stream for pool (or create a new)
        /// </summary>
        private Stream GetStream()
        {
            if (!_pool.TryTake(out var stream))
            {
                stream = _factory.GetStream(true, false);
            }

            return stream;
        }

        public void Dispose()
        {
            // dispose all reader stream
            foreach (var stream in _pool)
            {
                stream.Dispose();
            }

            _factory.Delete();
        }

        /// <summary>
        /// Slipt all items in big sorted containers - Do merge sort with all containers
        /// </summary>
        public IEnumerable<KeyValuePair<BsonValue, PageAddress>> Sort(IEnumerable<KeyValuePair<BsonValue, PageAddress>> items, int order)
        {
            var stream = new Lazy<Stream>(this.GetStream);
            var containers = new List<MergeSortContainer>();
            var bytes = BufferPool.Rent(_containerSize);
            var buffer = new BufferSlice(bytes, 0, _containerSize); // re-use same buffer for all containers
            var done = new Done { Running = true };

            // slit all items in sorted containers
            foreach (var containerItems in this.SliptValues(items, done))
            {
                var container = new MergeSortContainer(_containerSize);
                
                container.Insert(containerItems, order, buffer);
                
                containers.Add(container);
                
                // initialize container readers: if single container, do not use Stream file... only buffer memory
                if (done.Running == false && containers.Count == 1)
                {
                    container.InitializeReader(null, buffer, _utcDate);
                }
                else
                {
                    // store in disk
                    container.Position = this.GetContainerPosition();
                
                    stream.Value.Position = container.Position;
                    stream.Value.Write(buffer.Array, 0, _containerSize);
                
                    container.InitializeReader(stream.Value, null, _utcDate);
                }
            }

            // starts with first container as current
            var current = containers[0];

            // if single container, just return ordered data
            if (containers.Count == 1)
            {
                do
                {
                    yield return current.Current;
                }
                while (current.MoveNext());
            }
            else
            {
                // merge sort with all containers
                while (containers.Any(x => !x.IsEOF))
                {
                    foreach (var container in containers.Where(x => !x.IsEOF))
                    {
                        var diff = container.Current.Key.CompareTo(current.Current.Key) * -1;

                        if (diff == order)
                        {
                            current = container;
                        }
                    }

                    yield return current.Current;

                    var lastKey = current.Current.Key;

                    if (current.MoveNext() == false)
                    {
                        current.Dispose();

                        // now, current container must any new container that still have values
                        current = containers.FirstOrDefault(x => !x.IsEOF);
                    }

                    // after run MoveNext(), if container contains same lastKey, can return now
                    while (current?.Current.Key == lastKey)
                    {
                        yield return current.Current;

                        if (current.MoveNext() == false)
                        {
                            current.Dispose();
                            current = containers.FirstOrDefault(x => !x.IsEOF);
                        }
                    }
                }

            }

            // return array into pool
            buffer = null;
            BufferPool.Return(bytes);

            // return stream into pool
            if (stream.IsValueCreated) _pool.Add(stream.Value);
        }

        /// <summary>
        /// Split values in many IEnumerable. Each enumerable contains values to be insert in a single container
        /// </summary>
        private IEnumerable<IEnumerable<KeyValuePair<BsonValue, PageAddress>>> SliptValues(IEnumerable<KeyValuePair<BsonValue, PageAddress>> source, Done done)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    while (done.Running)
                    {
                        yield return this.YieldValues(enumerator, done);
                    }
                }
            }
        }

        /// <summary>
        /// Loop in values enumerator to return N values for a single container
        /// </summary>
        private IEnumerable<KeyValuePair<BsonValue, PageAddress>> YieldValues(IEnumerator<KeyValuePair<BsonValue, PageAddress>> source, Done done)
        {
            var size = MergeSortContainer.GetKeyLength(source.Current.Key) + PageAddress.SIZE;

            yield return source.Current;

            while (source.MoveNext())
            {
                var length = MergeSortContainer.GetKeyLength(source.Current.Key) + PageAddress.SIZE;

                if (size + length > _containerSize) yield break;

                size += length;

                yield return source.Current;
            }

            done.Running = false;
        }

        /// <summary>
        /// Bool inside a class to be used as "ref" parameter on ienumerable
        /// </summary>
        private class Done
        {
            public bool Running = false;
        }
    }
}