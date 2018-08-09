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
        private Func<BsonValue, IEnumerable<BsonDocument>> _input = null;

        public SystemCollection(string name)
        {
            if (!name.StartsWith("$")) throw new ArgumentException("System collection name must starts with $");

            _name = name;
        }

        public SystemCollection(string name, Func<BsonValue, IEnumerable<BsonDocument>> input)
            : this(name)
        {
            _input = input;
        }

        /// <summary>
        /// Get system collection name (must starts with $)
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Get input data source factory
        /// </summary>
        public virtual IEnumerable<BsonDocument> Input(BsonValue options) => _input(options);

        /// <summary>
        /// Get output data source factory (must implement in inherit class)
        /// </summary>
        public virtual int Output(IEnumerable<BsonValue> source, BsonValue options) => throw new LiteException(0, $"{_name} do not support as output collection");
    }
}