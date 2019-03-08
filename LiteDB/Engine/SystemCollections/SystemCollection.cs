using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement a simple system collection with input data only (to use Output must inherit this class)
    /// </summary>
    public class SystemCollection
    {
        private readonly string _name;
        private Func<IEnumerable<BsonDocument>> _input = null;

        public SystemCollection(string name)
        {
            if (!name.StartsWith("$")) throw new ArgumentException("System collection name must starts with $");

            _name = name;
        }

        public SystemCollection(string name, Func<IEnumerable<BsonDocument>> input)
            : this(name)
        {
            _input = input;
        }

        /// <summary>
        /// Get system collection name (must starts with $)
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Indicate this system collection as a function collection (has parameters to be called)
        /// </summary>
        public virtual bool IsFunction => false;

        /// <summary>
        /// Get input data source factory
        /// </summary>
        public virtual IEnumerable<BsonDocument> Input(LiteEngine engine, BsonValue options) => _input();

        /// <summary>
        /// Get output data source factory (must implement in inherit class)
        /// </summary>
        public virtual int Output(IEnumerable<BsonDocument> source, BsonValue options) => throw new LiteException(0, $"{_name} do not support as output collection");

        /// <summary>
        /// Static helper to read options arg as plain value or as document fields
        /// </summary>
        protected static T GetOption<T>(BsonValue options, bool root, string field, T defaultValue)
        {
            if (options.IsDocument == false)
            {
                return root ? (T)options.RawValue : defaultValue;
            }
            else
            {
                return (T)(options.DocValue[field].RawValue ?? defaultValue);
            }
        }
    }
}