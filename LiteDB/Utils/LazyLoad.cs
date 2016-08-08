using System;

namespace LiteDB
{
    /// <summary>
    /// LazyLoad class for .NET 3.5
    /// </summary>
    public class LazyLoad<T>
        where T : class
    {
        private T _value = null;
        private Func<T> _factory;
        private object _locker = new object();

        public LazyLoad(Func<T> factory)
        {
            _factory = factory;
        }

        public bool IsValueCreated { get { return _value != null; } }

        public T Value
        {
            get
            {
                lock (_locker)
                {
                    if (_value == null)
                    {
                        _value = _factory();
                    }
                }

                return _value;
            }
        }
    }
}