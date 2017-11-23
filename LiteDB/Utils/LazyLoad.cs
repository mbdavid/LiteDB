using System;

namespace LiteDB
{
    /// <summary>
    /// LazyLoad class for .NET 3.5
    /// http://stackoverflow.com/questions/3207580/implementation-of-lazyt-for-net-3-5
    /// </summary>
    public class LazyLoad<T>
        where T : class
    {
        private readonly object _locker = new object();
        private readonly Func<T> _createValue;
        private bool _isValueCreated;
        private T _value;

        /// <summary>
        /// Gets the lazily initialized value of the current Lazy{T} instance.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_isValueCreated)
                {
                    lock (_locker)
                    {
                        if (!_isValueCreated)
                        {
                            _value = _createValue();
                            _isValueCreated = true;
                        }
                    }
                }
                return _value;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether a value has been created for this Lazy{T} instance.
        /// </summary>
        public bool IsValueCreated
        {
            get
            {
                lock (_locker)
                {
                    return _isValueCreated;
                }
            }
        }


        /// <summary>
        /// Initializes a new instance of the Lazy{T} class.
        /// </summary>
        /// <param name="createValue">The delegate that produces the value when it is needed.</param>
        public LazyLoad(Func<T> createValue)
        {
            if (createValue == null) throw new ArgumentNullException(nameof(createValue));

            _createValue = createValue;
        }

        /// <summary>
        /// Creates and returns a string representation of the Lazy{T}.Value.
        /// </summary>
        /// <returns>The string representation of the Lazy{T}.Value property.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}